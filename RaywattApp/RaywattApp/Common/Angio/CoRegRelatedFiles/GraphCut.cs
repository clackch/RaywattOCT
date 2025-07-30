using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;

namespace RaywattApp.Common.Angio.CoRegRelatedFiles
{
    public class GraphCut
    {
        private Mat image;
        private int[,] superpixelLabels;
        private List<float[]> superpixelCenters;
        private List<int[]> objectSeeds;
        private List<int[]> backgroundSeeds;
        private GaussianClusterCollection objGmm, bkgGmm;
        private float alpha;

        public GraphCut() {}

        public void Initialize(Mat image, int[,] superpixelLabels, List<float[]> superpixelCenters, float alpha = 50)
        {
            this.image = image;
            this.superpixelLabels = superpixelLabels;
            this.superpixelCenters = superpixelCenters;
            this.alpha = alpha;
        }

        public void InitializeSeed(string objectImagePath, string backgroundImagePath)
        {
            var objectImage = Cv2.ImRead(objectImagePath, ImreadModes.Grayscale);
            var backgroundImage = Cv2.ImRead(backgroundImagePath, ImreadModes.Grayscale);

            objectSeeds = new List<int[]>();
            for (int i = 0; i < objectImage.Height; i++)
            {
                for (int j = 0; j < objectImage.Width; j++)
                {
                    if (objectImage.At<byte>(i, j) > 0)
                    {
                        objectSeeds.Add(new[] { i, j });
                    }
                }
            }

            backgroundSeeds = new List<int[]>();
            for (int i = 0; i < backgroundImage.Height; i++)
            {
                for (int j = 0; j < backgroundImage.Width; j++)
                {
                    if (backgroundImage.At<byte>(i, j) > 0)
                    {
                        backgroundSeeds.Add(new[] { i, j });
                    }
                }
            }
        }

        private void FitGMM()
        {
            double[][] objSamples = objectSeeds.Select(seed => new double[] { image.At<byte>(seed[0], seed[1]) }).ToArray();
            double[][] bkgSamples = backgroundSeeds.Select(seed => new double[] { image.At<byte>(seed[0], seed[1]) }).ToArray();

            var objGmm = new GaussianMixtureModel(1);
            this.objGmm = objGmm.Learn(objSamples);

            var bkgGmm = new GaussianMixtureModel(1);
            this.bkgGmm = bkgGmm.Learn(bkgSamples);
        }

        private (double[,], int, int) BuildGraph()
        {
            int numSuperpixels = superpixelCenters.Count;
            double[,] graph = new double[numSuperpixels + 2, numSuperpixels + 2];
            int source = 0;
            int sink = numSuperpixels + 1;

            for (int i = 0; i < superpixelCenters.Count; i++)
            {
                float intensity = superpixelCenters[i][2];

                double pObj = Math.Exp(objGmm.LogLikelihood(new double[] { intensity }));
                double pBkg = Math.Exp(bkgGmm.LogLikelihood(new double[] { intensity }));

                double result1 = pObj * 100;
                graph[source, i + 1] = result1; // 작은 값일수록 큰 가중치
                double result2 = pBkg * 100;
                graph[i + 1, sink] = result2;
            }

            int step = (int)Math.Sqrt(image.Width* image.Height / superpixelLabels.Cast<int>().Max());
            for (int i = 0; i < superpixelCenters.Count; i++)
            {
                for (int j = i + 1; j < superpixelCenters.Count; j++)
                {
                    float cy1 = superpixelCenters[i][0], cx1 = superpixelCenters[i][1];
                    float cy2 = superpixelCenters[j][0], cx2 = superpixelCenters[j][1];
                    double distance = Math.Sqrt(Math.Pow(cy1 - cy2, 2) + Math.Pow(cx1 - cx2, 2));

                    if (distance < step / 4)
                    {
                        double weight = alpha * Math.Exp(-Math.Abs(superpixelCenters[i][2] - superpixelCenters[j][2]) / 30);
                        graph[i + 1, j + 1] = weight;
                        graph[j + 1, i + 1] = weight;
                    }
                }
            }

            return (graph, source, sink);
        }

        private static double[,] NetworkFlow(double[,] graph, int source, int sink)
        {
            int N = graph.GetLength(0);
            double[,] flow = new double[N, N];
            int[] parent = new int[N];

            bool Bfs()
            {
                Array.Fill(parent, -1);
                parent[source] = source;
                var queue = new Queue<int>();
                queue.Enqueue(source);

                while (queue.Count > 0)
                {
                    int u = queue.Dequeue();
                    for (int v = 0; v < N; v++)
                    {
                        if (parent[v] == -1 && graph[u, v] - flow[u, v] > 0)
                        {
                            parent[v] = u;
                            if (v == sink) return true;
                            queue.Enqueue(v);
                        }
                    }
                }
                return false;
            }

            while (Bfs())
            {
                double pathFlow = double.MaxValue;
                for (int v = sink; v != source; v = parent[v])
                {
                    int u = parent[v];
                    pathFlow = Math.Min(pathFlow, graph[u, v] - flow[u, v]);
                }

                for (int v = sink; v != source; v = parent[v])
                {
                    int u = parent[v];
                    flow[u, v] += pathFlow;
                    flow[v, u] -= pathFlow;
                }
            }

            return flow;
        }

        private static bool[] MinCut(double[,] graph, double[,] flow, int source)
        {
            int N = graph.GetLength(0);
            bool[] visited = new bool[N];
            var queue = new Queue<int>();
            visited[source] = true;
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                int u = queue.Dequeue();
                for (int v = 0; v < N; v++)
                {
                    if (!visited[v] && graph[u, v] - flow[u, v] > 0)
                    {
                        visited[v] = true;
                        queue.Enqueue(v);
                    }
                }
            }

            return visited;
        }

        public Mat Segment()
        {
            FitGMM();
            var (graph, source, sink) = BuildGraph();

            double[,] flow = NetworkFlow(graph, source, sink);
            bool[] visited = MinCut(graph, flow, source);

            Mat segmentedImg = new Mat(image.Rows, image.Cols, MatType.CV_8UC1);

            var uniqueLabels = superpixelLabels.Cast<int>().Distinct().ToList();
            for (int idx = 0; idx < uniqueLabels.Count; idx++)
            {
                int label = uniqueLabels[idx];

                for (int y = 0; y < superpixelLabels.GetLength(0); y++)
                {
                    for (int x = 0; x < superpixelLabels.GetLength(1); x++)
                    {
                        if (superpixelLabels[y, x] == label)
                        {
                            segmentedImg.At<byte>(y, x) = visited[label + 1] ? (byte)255 : (byte)0;
                        }
                    }
                }
            }

            return segmentedImg;
        }
    }
}
