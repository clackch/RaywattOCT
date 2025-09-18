using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using OpenCvSharp;
using RaywattOCTFFR.ViewModels;

namespace RaywattOCTFFR.Common.Angio.CoRegRelatedFiles
{
    public class Superpixel
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));
        private int m_numSuperpixels;
        private float m_compactness; // 값이 클수록 Segmentation Group의 모양이 정형화 됨.
        private int m_maxIterations;
        private int[,] m_labels;
        private List<List<System.Windows.Point>> m_labelSegments;
        private List<float[]> m_centers;

        public Superpixel() {}

        public void Initialize(int numSuperpixels = 20 * 20, float compactness = 3, int maxIterations = 10)
        {
            this.m_numSuperpixels = numSuperpixels;
            this.m_compactness = compactness;
            this.m_maxIterations = maxIterations;
            this.m_centers = null;
            this.m_labels = null;
            this.m_labelSegments = null;
        }

        private static List<float[]> InitializeCenters(Mat image, int step)
        {
            int height = image.Rows;
            int width = image.Cols;
            List<float[]> centers = new List<float[]>();

            // Sobel Gradient 계산
            Mat sobelX = new Mat();
            Mat sobelY = new Mat();

            Mat Sobel = image.Clone();
            Cv2.Sobel(Sobel, sobelX, MatType.CV_64F, 1, 0, ksize: 3);

            Sobel = image.Clone();
            Cv2.Sobel(Sobel, sobelY, MatType.CV_64F, 0, 1, ksize: 3);

            Mat gradMagnitude = new Mat();
            Cv2.Magnitude(sobelX, sobelY, gradMagnitude);

            for (int x = step / 2; x < width; x += step)
            {
                for (int y = step / 2; y < height; y += step)
                {
                    Rect roi = new Rect(x - step / 2, y - step / 2, step, step);
                    roi = roi.Intersect(new Rect(0, 0, width, height));

                    Mat neighborhood = new Mat(gradMagnitude, roi);
                    Point minLoc;
                    Cv2.MinMaxLoc(neighborhood, out minLoc, out _);

                    int bestX = roi.X + minLoc.X;
                    int bestY = roi.Y + minLoc.Y;
                    float bestIntensity = image.At<byte>(bestY, bestX);

                    centers.Add(new float[] { bestY, bestX, bestIntensity });
                }
            }

            return centers;
        }

        private int[,] UpdateLabelsAndDistances(Mat image, List<float[]> centers, int step)
        {
            int height = image.Height;
            int width = image.Width;
            int size = height * width;

            int[] labels = new int[size];
            float[] distances = new float[size];

            for (int i = 0; i < size; i++)
            {
                labels[i] = -1;
                distances[i] = float.MaxValue;
            }

            for (int i = 0; i < centers.Count; i++)
            {
                float cy = centers[i][0];
                float cx = centers[i][1];
                float intensity = centers[i][2];

                int yStart = Math.Max((int)(cy - 2 * step), 0);
                int yEnd = Math.Min((int)(cy + 2 * step), height);
                int xStart = Math.Max((int)(cx - 2 * step), 0);
                int xEnd = Math.Min((int)(cx + 2 * step), width);

                Parallel.For(yStart, yEnd, y =>
                {
                    int rowStartIndex = y * width;
                    float[] localDistances = new float[width];
                    int[] localLabels = new int[width];

                    for (int x = xStart; x < xEnd; x++)
                    {
                        localDistances[x] = float.MaxValue;
                        localLabels[x] = -1;
                    }

                    for (int x = xStart; x < xEnd; x++)
                    {
                        int idx = rowStartIndex + x;

                        float colorDist = image.At<byte>(y, x) - intensity;
                        float spatialDist = (y - cy) * (y - cy) + (x - cx) * (x - cx);
                        float distance = colorDist * colorDist + m_compactness * m_compactness * (spatialDist / 4);

                        if (localDistances[x] > distance)
                        {
                            localDistances[x] = distance;
                            localLabels[x] = i;
                        }
                    }

                    lock (distances)
                    {
                        for (int x = xStart; x < xEnd; x++)
                        {
                            int idx = rowStartIndex + x;
                            if (distances[idx] > localDistances[x])
                            {
                                distances[idx] = localDistances[x];
                                labels[idx] = localLabels[x];
                            }
                        }
                    }
                });
            }

            int[,] outputLabels = new int[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    outputLabels[y, x] = labels[idx];
                }
            }

            return outputLabels;
        }


        private List<float[]> UpdateCenters(Mat image, int[,] labels, List<float[]> centers, int numLabel, List<List<System.Windows.Point>> segments = null)
        {
            List<float[]> newCenters = new List<float[]>();

            for (int i = 0; i < numLabel; i++)
            {
                newCenters.Add(null);
            }

            Parallel.For(0, numLabel, i =>
            {
                List<int> yCoords = new List<int>();
                List<int> xCoords = new List<int>();
                List<float> intensities = new List<float>();

                for (int y = 0; y < labels.GetLength(0); y++)
                {
                    for (int x = 0; x < labels.GetLength(1); x++)
                    {
                        if (labels[y, x] == i)
                        {
                            yCoords.Add(y);
                            xCoords.Add(x);
                            intensities.Add(image.At<byte>(y, x));

                            if (segments != null) {
                                segments[i].Add(new System.Windows.Point(x, y));
                            }
                        }
                    }
                }

                if (yCoords.Count > 0)
                {
                    float meanY = (float)yCoords.Average();
                    float meanX = (float)xCoords.Average();
                    float meanIntensity = intensities.Average();
                    newCenters[i] = new float[] { meanY, meanX, meanIntensity };
                }
                else
                {
                    newCenters[i] = centers[i];
                }
            });

            m_numSuperpixels = newCenters.Count;

            return newCenters;
        }

        public void Fit(Mat image)
        {
            int height = image.Height;
            int width = image.Width;
            int numPixels = height * width;
            int step = (int)Math.Sqrt(numPixels / m_numSuperpixels);
            int[,] labels = null; 
            int numOfLabels = 0;

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            m_centers = InitializeCenters(image, step);

            for (int iter = 0; iter < m_maxIterations; iter++)
            {
                labels = UpdateLabelsAndDistances(image, m_centers, step);
                //_log.Debug($"UpdateLabelsAndDistances Time : {stopwatch.Elapsed.TotalSeconds} seconds");
                //stopwatch.Restart();

                List<float[]> newCenters = UpdateCenters(image, labels, m_centers, m_centers.Count);

                bool converged = true;
                for (int i = 0; i < m_centers.Count; i++)
                {
                    for (int j = 0; j < m_centers[i].Length; j++)
                    {
                        if (Math.Abs(m_centers[i][j] - newCenters[i][j]) > 1e-3)
                        {
                            converged = false;
                            break;
                        }
                    }
                }

                m_centers = newCenters;
                if (converged) break;
            }
            //stopwatch.Stop();

            numOfLabels = m_centers.Count;

            m_labels = EnforceLabelConnectivity(labels, width, height, m_numSuperpixels, out numOfLabels);

            m_labelSegments = new List<List<System.Windows.Point>>();
            for(int i = 0; i<numOfLabels; i++)
            {
                m_labelSegments.Add(new List<System.Windows.Point>());
            }

            m_centers = UpdateCenters(image, m_labels, m_centers, numOfLabels, m_labelSegments);
            //Visualize(nlabels, centers, width, height, numOfLabels);
        }

        private static int[,] EnforceLabelConnectivity(int[,] labels, int width, int height, int numSuperpixels, out int numLabels)
        {
            int[] dx4 = { -1, 0, 1, 0 };
            int[] dy4 = { 0, -1, 0, 1 };

            int sz = width * height;
            int SUPSZ = sz / numSuperpixels;

            int[,] nlabels = new int[height, width];
            for (int j = 0; j < height; j++)
                for (int k = 0; k < width; k++)
                    nlabels[j, k] = -1;

            int label = 0;
            int[] xvec = new int[sz];
            int[] yvec = new int[sz];

            int adjLabel = -1;

            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < width; k++)
                {
                    if (nlabels[j, k] == -1)
                    {
                        nlabels[j, k] = label;

                        xvec[0] = k;
                        yvec[0] = j;

                        for (int n = 0; n < 4; n++)
                        {
                            int nx = k + dx4[n];
                            int ny = j + dy4[n];
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (nlabels[ny, nx] >= 0)
                                {
                                    adjLabel = nlabels[ny, nx];
                                }
                            }
                        }

                        int clusterSize = 1;
                        int idx = 0;
                        while(idx < clusterSize)
                        {
                            int x = xvec[idx];
                            int y = yvec[idx];
                            for (int n = 0; n < 4 /*L,U,R,D direction check*/; n++)
                            {
                                int nx = x + dx4[n];
                                int ny = y + dy4[n];
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    if (nlabels[ny, nx] == -1 && labels[ny, nx] == labels[j, k])
                                    {
                                        xvec[clusterSize] = nx;
                                        yvec[clusterSize] = ny;
                                        nlabels[ny, nx] = label;
                                        clusterSize++;
                                    }
                                }
                            }
                            idx++;
                        }

                        if (clusterSize <= SUPSZ / 4 /*current averaged cluster size * 0.25 */ && adjLabel != -1)
                        {
                            for (int i = 0; i < clusterSize; i++)
                            {
                                nlabels[yvec[i], xvec[i]] = adjLabel;
                            }
                            label--;
                        }

                        label++;
                    }
                }
            }

            numLabels = label;
            return nlabels;
        }

        private static void Visualize(int[,] label, List<float[]> centers, int width, int height, int numLabel)
        {
            Mat outputImage = new Mat(height, width, MatType.CV_8UC1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentLabel = label[y, x];
                    if (currentLabel >= 0 && currentLabel < numLabel)
                    {
                        outputImage.At<byte>(y, x) = (byte)centers[currentLabel][2];
                    }
                }
            }

            //Cv2.ImWrite(".\\angio\\SuperpixelImage" + count++.ToString() + ".png", outputImage);
        }

        public int[,] GetLabels()
        {
            return m_labels;
        }

        public List<float[]> GetCenters()
        {
            return m_centers;
        }

        public int GetSuperpixelNum()
        {
            return m_numSuperpixels;
        }

        public List<List<System.Windows.Point>> GetSegmentsPoints()
        {
            return m_labelSegments;
        }

    }
}
