using OpenCvSharp;
using BitMiracle.LibTiff.Classic;
using log4net;
using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using RaywattApp.Common.Bases;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices;
using static RaywattOCT.RayCoreWrapper;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Size = OpenCvSharp.Size;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.ViewModels.Dialog;
using RaywattApp.Views.Dialog;
using System.Threading;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using Newtonsoft.Json;
using System.Windows.Shapes;

namespace RaywattApp.Common.Util
{
    public class CommonUtil
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CommonUtil));

        public static bool ValidateText(string input)
        {
            var regex = new Regex(@"^[a-zA-Z0-9ㄱ-ㅎ가-힣\s,.]+$");

            if (input.Length == 0) 
                return true;

            return regex.IsMatch(input);
        }

        public static bool ValidateId(string input)
        {
            var regex = new Regex(@"^[a-zA-Z0-9]+$");

            if (input.Length == 0)
                return true;

            return regex.IsMatch(input);
        }

        public static bool ValidateNumber(string input)
        {
            var regex = new Regex(@"^[0-9]+$");

            if (input.Length == 0)
                return true;

            return regex.IsMatch(input);
        }

        public static Mat ByteMemoryToCvMat(IntPtr data, int width, int height, int ch)
        {
            MatType type = MatType.CV_8UC1;
            switch (ch)
            {
                case 2:
                    type = MatType.CV_32SC2;    // coordinates
                    break;
                case 3:
                    type = MatType.CV_8UC3;     // 3ch image
                    break;
                default:
                    break;
            }
            return new Mat(height, width, type, data).Clone();
        }

        public static void RenderVisualToMat(DrawingVisual visual, Mat image)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)image.Cols, (int)image.Height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            var bitmapImage = new BitmapImage();
            using (var stream = new System.IO.MemoryStream())
            {
                encoder.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            Mat imgDraw = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(bitmapImage);
            Mat imgDrawGray = new Mat();
            Mat imgBW = new Mat();
            Cv2.CvtColor(imgDraw, imgDrawGray, ColorConversionCodes.RGBA2GRAY);
            Cv2.Threshold(imgDrawGray, imgBW, 1, 255, ThresholdTypes.Binary);

            Cv2.CvtColor(imgDraw, imgDraw, ColorConversionCodes.RGBA2RGB);
            Cv2.CopyTo(imgDraw, image, imgBW);
        }

        public static string GetRandomText(int length)
        {
            byte[] rndNumbers = new byte[length];
            for (int i = 0; i < rndNumbers.Length; i++)
            {
                int type = RandomNumberGenerator.GetInt32(1, 3);
                switch (type)
                {
                    case 0: // a~z
                        rndNumbers[i] = (byte)RandomNumberGenerator.GetInt32(97, 123);
                        break;
                    case 1: // A~Z
                        rndNumbers[i] = (byte)RandomNumberGenerator.GetInt32(65, 91);
                        break;
                    case 2: // 0~9
                        rndNumbers[i] = (byte)RandomNumberGenerator.GetInt32(48, 58);
                        break;
                    default:
                        break;
                }
            }
            return System.Text.Encoding.ASCII.GetString(rndNumbers);
        }

        public static async Task Encryptor(string filePath, string contents, Action<double> progressCallback, double progressSize, Action<string> progressTextCallback)
        {
            try
            {
                using (FileStream fileStream = new(filePath, FileMode.Create))
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] key = Encoding.UTF8.GetBytes(Constants.PublicKey);
                        aes.Key = key;

                        byte[] iv = aes.IV;
                        fileStream.Write(iv, 0, iv.Length);

                        using (CryptoStream cryptoStream = new(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            // By default, the StreamWriter uses UTF-8 encoding.
                            // To change the text encoding, pass the desired encoding as the second parameter.
                            // For example, new StreamWriter(cryptoStream, Encoding.Unicode).
                            using (StreamWriter encryptWriter = new(cryptoStream))
                            {
                                string[] strings = new string[10];
                                int cnt = contents.Length / 10;
                                for(int i = 0; i < 9; i++)
                                {
                                    strings[i] = contents.Substring(i * cnt, cnt);
                                }
                                strings[9] = contents.Substring(9 * cnt);
                               
                                foreach (string ch in strings)
                                {
                                    await Task.Run(() =>
                                    {
                                        encryptWriter.Write(ch);
                                        progressCallback(progressSize / 10);
                                        progressTextCallback(Constants.ExportStatusSaveFile);
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"The encryption failed. {ex}");
            }
        }

        public static Tuple<bool, string> Decryptor(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return new Tuple<bool, string>(false, "No File");

                string contents = "";

                using (FileStream fileStream = System.IO.File.OpenRead(filePath))
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] iv = new byte[aes.IV.Length];
                        int numBytesToRead = aes.IV.Length;
                        int numBytesRead = 0;
                        while (numBytesToRead > 0)
                        {
                            int n = fileStream.Read(iv, numBytesRead, numBytesToRead);
                            if (n == 0) break;

                            numBytesRead += n;
                            numBytesToRead -= n;
                        }

                        byte[] key = Encoding.UTF8.GetBytes(Constants.PublicKey);

                        using (CryptoStream cryptoStream = new(fileStream, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
                        {
                            // By default, the StreamReader uses UTF-8 encoding.
                            // To change the text encoding, pass the desired encoding as the second parameter.
                            // For example, new StreamReader(cryptoStream, Encoding.Unicode).
                            using (StreamReader decryptReader = new(cryptoStream))
                            {
                                contents = decryptReader.ReadToEnd();
                            }
                        }
                    }
                }
                return new Tuple<bool, string>(true, contents);
            }
            catch (Exception ex)
            {
                _log.Error($"The decryption failed. {ex}");
                return new Tuple<bool, string>(false, ex.ToString());
            }
        }

        public static long GetFileSize(string path)
        {
            long fileSize = 0;
            if (System.IO.File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                fileSize = info.Length;
            }

            return fileSize;
        }

        public static string GetFileName(string path)
        {
            string fileName = "";
            if (System.IO.File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                fileName = info.Name;
            }

            return fileName;
        }

        public static string? GetDirectoryPath(string path)
        {
            string? directoryPath = "";
            if (System.IO.File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                directoryPath = info.DirectoryName;
            }

            return directoryPath;
        }

        public static string CreateFolder(string path)
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(path);

            return directoryInfo.FullName;
        }

        public static void RenameFolder(string oldPath, string newPath)
        {
            if(oldPath != newPath)
                Directory.Move(oldPath, newPath);
        }

        public static void DeleteFolder(string path)
        {
            Directory.Delete(path, true);
        }

        public static async Task CopyFiles(Dictionary<string, string> files, Action<double> progressCallback, double progressSize, Action<string> progressTextCallback)
        {
            long total_size = files.Keys.Select(x => new FileInfo(x).Length).Sum();

            long total_read = 0;

            foreach (var item in files)
            {
                long total_read_for_file = 0;

                var from = item.Key;
                var to = item.Value;

                using (var outStream = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    using (var inStream = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        await CopyStream(inStream, outStream, x =>
                        {
                            total_read_for_file = x;
                            progressCallback(((total_read + total_read_for_file) / (double)total_size) * progressSize);
                            progressTextCallback(Constants.ExportStatusCopyFile);
                        });
                    }
                }

                total_read += total_read_for_file;
            }
        }

        public static async Task<Mat> ConvertImage(string filePath, double degree, List<Mat> convertedImages, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {
            RayOpenImage(filePath);

            int numOfFrames = (int)RayGetProperty(Property.ImageDepth);
            int width = (int)RayGetProperty(Property.ImageWidth);
            int height = (int)RayGetProperty(Property.ImageHeight);
            int channels = (int)RayGetProperty(Property.ImageChannels);

            // convert all frames
            for (int index = 0; index < numOfFrames; index++)
            {
                await Task.Run(() => {
                    IntPtr data = RayGetImageData(index);
                    if (convertedImages != null)
                    {
                        Mat img = CommonUtil.ByteMemoryToCvMat(data, width, height, channels);
                        convertedImages.Add(img);
                    }
                    progressCallback(progress / numOfFrames);
                    progressTextCallback(Constants.ExportStatusConvertImage);
                });
            }

            // get longitude from core
            width = (int)RayGetProperty(Property.LongitudeImageWidth);
            height = (int)RayGetProperty(Property.LongitudeImageHeight);
            channels = (int)RayGetProperty(Property.LongitudeImageChannels);
            IntPtr data = RayGetLongitudeData(degree);
            Mat imgLongitude = CommonUtil.ByteMemoryToCvMat(data, width, height, channels);

            RayCloseImage();

            return imgLongitude;
        }

        public static async Task<List<Mat>> MakeImageForExport(PatientCase patientCase, List<Mat> imgCrossSections, Mat imgLongitude, List<int>? exportIndices, FileExport fileExport, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {
            List<Mat> convertedImages = new List<Mat>();

            IDialogWindow window = new DialogWindow();

            var dialog = new FileExportDialogControl();
            double originWidth = dialog.Width;
            double originHeight = dialog.Height;
            dialog.Width = 0;
            dialog.Height = 0;
            window.Content = dialog;

            var dialogFE = dialog as System.Windows.FrameworkElement;
            var dialogDataContext = dialogFE.DataContext as FileExportDialogViewModel;
            dialogDataContext.SetInitialize(patientCase, imgCrossSections, imgLongitude, fileExport);

            window.Show();
            window.Hide();
            dialog.Width = originWidth;
            dialog.Height = originHeight;

            int totalCnt = imgCrossSections.Count;
            if(exportIndices != null)
                totalCnt = exportIndices.Count;

            for (int i = 0; i < totalCnt; i++)
            {
                int index = i;
                if (exportIndices != null)
                    index = exportIndices[i];

                dialogDataContext.SetFrameNumber(index);
                dialog.UpdateLayout();
                        
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)dialog.ActualWidth, (int)dialog.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                System.Windows.Rect bounds = VisualTreeHelper.GetDescendantBounds(dialog);
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext ctx = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(dialog);
                    ctx.DrawRectangle(vb, null, bounds);
                }
                rtb.Render(dv);

                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));
                var bitmapImage = new BitmapImage();

                using (var stream = new System.IO.MemoryStream())
                {
                    png.Save(stream);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                }

                Mat image = new Mat();
                Mat imgDraw = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(bitmapImage);
                Mat imgDrawGray = new Mat();
                Mat imgBW = new Mat();
                Cv2.CvtColor(imgDraw, imgDrawGray, ColorConversionCodes.RGBA2GRAY);
                Cv2.Threshold(imgDrawGray, imgBW, 1, 255, ThresholdTypes.Binary);
                Cv2.CvtColor(imgDraw, imgDraw, ColorConversionCodes.RGBA2RGB);
                Cv2.CopyTo(imgDraw, image, imgBW);

                convertedImages.Add(image);

                await Task.Run(() => {
                    progressCallback(progress / totalCnt);
                    progressTextCallback(Constants.ExportStatusMakeImage);
                    Thread.Sleep(100);
                });
            }
            window.Close();

            return convertedImages;
        }

        public static Mat MakeImageForExport(Mat crossSection, Mat? longitude, Mat? lumenProfile, Mat? angio, out List<Tuple<Rect, Size2f>> region) {
            Mat imgExport = new Mat();
            imgExport.Create((int)Constants.ApplicationHeight, (int)Constants.ApplicationWidth, MatType.CV_8UC3);
            imgExport.SetTo(0x00);

            region = new List<Tuple<Rect, Size2f>>();
            if (crossSection == null) return imgExport;

            Size szRemain = new Size(imgExport.Width, imgExport.Height);
            Size szLongitudeInfo = new Size(imgExport.Width, 30);
            Size szLongitude = new Size(imgExport.Width, (imgExport.Height / 2 - szLongitudeInfo.Height) / 2);
            if (longitude != null)
            {
                Mat imgLongitude = new Mat();
                Rect rectLongitude = new Rect(0, szRemain.Height - szLongitude.Height, szLongitude.Width, szLongitude.Height);
                Size2f scaleLongitude = new Size2f(1, 1);
                Cv2.Resize(longitude, imgLongitude, szLongitude);
                Cv2.CopyTo(imgLongitude, imgExport[rectLongitude]);
                szRemain.Height -= szLongitude.Height;

                Tuple<Rect, Size2f> regionLongitude = new Tuple<Rect, Size2f>(rectLongitude, scaleLongitude);
                region.Add(regionLongitude);

                // draw longitude info
                Rect rectLongitudeInfo = new Rect(0, szRemain.Height - szLongitude.Height, szLongitudeInfo.Width, szLongitudeInfo.Height);
                Size2f scaleLongitudeInfo = new Size2f(1, 1);
                szRemain.Height -= szLongitudeInfo.Height;

                Tuple<Rect, Size2f> regionLongitudeInfo = new Tuple<Rect, Size2f>(rectLongitudeInfo, scaleLongitudeInfo);
                region.Add(regionLongitudeInfo);
            }
            if (lumenProfile != null)
            {
                Mat imgLumenProfile = new Mat();
                Rect rectLumenProfile = new Rect(0, szRemain.Height - szLongitude.Height, szLongitude.Width, szLongitude.Height);
                Size2f scaleLumenProfile = new Size2f(1, 1);
                Cv2.Resize(lumenProfile, imgLumenProfile, szLongitude);
                Cv2.CopyTo(imgLumenProfile, imgExport[rectLumenProfile]);
                szRemain.Height -= szLongitude.Height;

                Tuple<Rect, Size2f> regionLumenProfile = new Tuple<Rect, Size2f>(rectLumenProfile, scaleLumenProfile);
                region.Add(regionLumenProfile);
            }

            int diameterCrossSection = Math.Min(szRemain.Width, szRemain.Height);

            if (angio != null)
            {
                Rect rectAngio = new Rect(0, 0, szRemain.Width - diameterCrossSection, szRemain.Height);
                Mat imgAngio = new Mat();
                Cv2.Resize(angio, imgAngio, rectAngio.Size);
                Cv2.CopyTo(imgAngio, imgExport[rectAngio]);
                szRemain.Width -= rectAngio.Width;
            }

            int offsetCrossSection = imgExport.Width - szRemain.Width;
            Rect rectCrossSection = new Rect(offsetCrossSection + (szRemain.Width - diameterCrossSection) / 2, 0, diameterCrossSection, diameterCrossSection);
            Size2f scaleCrossSection = new Size2f(1, 1);
            Mat imgCrossSection = new Mat();
            Cv2.Resize(crossSection, imgCrossSection, rectCrossSection.Size);
            Cv2.CopyTo(imgCrossSection, imgExport[rectCrossSection]);

            Tuple<Rect, Size2f> regionCrossSection = new Tuple<Rect, Size2f>(rectCrossSection, scaleCrossSection);
            region.Add(regionCrossSection);

            return imgExport;
        }

        public static Mat MakeLumenProfileImage(List<LumenContour> lumenContours, int currentFrame = -1)
        {
            const double radius = Constants.OCTImageSize / 2;
            const double totalArea = radius * radius * Math.PI;

            if (lumenContours == null || lumenContours.Count <= 0) return null;

            int cols = currentFrame == -1 ? lumenContours.Count : currentFrame + 1;

            Mat imglumenProfile = new Mat(100, lumenContours.Count, MatType.CV_8UC3);
            imglumenProfile.SetTo(new Scalar(0x4f, 0x4f, 0x4f));

            int curFrame = 0;
            foreach (LumenContour lumenContour in lumenContours.GetRange(0, cols))
            {
                double area = lumenContour.Area;

                int lumenArea = (int)(area / totalArea * imglumenProfile.Rows);
                int yStart = (imglumenProfile.Rows - lumenArea) / 2;

                Cv2.Line(imglumenProfile, new Point(curFrame, yStart), new Point(curFrame, yStart + lumenArea), new Scalar(0x16, 0x16, 0x16));
                curFrame++;
            }

            return imglumenProfile;
        }

        public static Mat MakeLumenProfileImage(List<LumenContour> lumenContours, int frameProximal, int frameDistal, bool isPostCase, List<int>? appositionFrames, int currentFrame = -1)
        {
            const double radius = Constants.OCTImageSize / 2;
            const double totalArea = radius * radius * Math.PI;

            if (lumenContours == null || lumenContours.Count <= 0) return null;

            int cols = currentFrame == -1 ? lumenContours.Count : currentFrame + 1;

            Mat imglumenProfile = new Mat(200, lumenContours.Count, MatType.CV_8UC3);
            imglumenProfile.SetTo(new Scalar(0x33, 0x33, 0x33));

            int curFrame = 0;
            foreach (LumenContour lumenContour in lumenContours.GetRange(0, cols))
            {
                double area = lumenContour.Area;

                int lumenArea = (int)(area / totalArea * imglumenProfile.Rows);
                int yStart = (imglumenProfile.Rows - lumenArea) / 2;

                Cv2.Line(imglumenProfile, new Point(curFrame, yStart), new Point(curFrame, yStart + lumenArea), new Scalar(0x16, 0x16, 0x16));

                //Lesion Section
                if (curFrame >= frameProximal && curFrame <= frameDistal)
                {
                    //Stent Area
                    if (isPostCase && appositionFrames != null && appositionFrames.Contains(curFrame))
                        Cv2.Line(imglumenProfile, new Point(curFrame, yStart), new Point(curFrame, yStart + lumenArea), new Scalar(0x3f, 0x41, 0x76));

                    //Stent
                    for (int i = 0; isPostCase && i < imglumenProfile.Rows; i++)
                    {
                        if ((i + curFrame) % 20 == 0)
                        {
                            Cv2.Line(imglumenProfile, new Point(curFrame, i), new Point(curFrame, i), new Scalar(0x8d, 0x8d, 0x8d));
                        }
                        if ((i - curFrame) % 20 == 0)
                        {
                            Cv2.Line(imglumenProfile, new Point(curFrame, i), new Point(curFrame, i), new Scalar(0x8d, 0x8d, 0x8d));
                        }
                    }

                    Cv2.Line(imglumenProfile, new Point(curFrame, 0), new Point(curFrame, yStart - 1), new Scalar(0x4f, 0x4f, 0x4f));
                    Cv2.Line(imglumenProfile, new Point(curFrame, yStart + lumenArea + 1), new Point(curFrame, imglumenProfile.Rows), new Scalar(0x4f, 0x4f, 0x4f));
                }

                //Side Branch
                if (lumenContour.HasSidebranch)
                {
                    if (curFrame >= frameProximal && curFrame <= frameDistal)
                        Cv2.Line(imglumenProfile, new Point(curFrame, imglumenProfile.Rows / 2 - 5), new Point(curFrame, imglumenProfile.Rows / 2 + 5), new Scalar(0xe4, 0xe4, 0xe4));
                    else
                        Cv2.Line(imglumenProfile, new Point(curFrame, imglumenProfile.Rows / 2 - 5), new Point(curFrame, imglumenProfile.Rows / 2 + 5), new Scalar(0x7d, 0x7d, 0x7d));
                }

                curFrame++;
            }

            return imglumenProfile;
        }

        public static Mat MakeLumenProfileImageExtra(int frameCnt, List<int> colorFrames, bool isPreCase, int currentFrame = -1)
        {
            int cols = currentFrame == -1 ? frameCnt : currentFrame + 1;

            Mat imglumenProfile = new Mat(20, frameCnt, MatType.CV_8UC3);
            imglumenProfile.SetTo(new Scalar(0x33, 0x33, 0x33));

            for (int i = 0; i < cols; i++)
            {
                if(colorFrames.Contains(i))
                    Cv2.Line(imglumenProfile, new Point(i, 0), new Point(i, imglumenProfile.Rows), isPreCase ? new Scalar(0xeb, 0xfe, 0x75) : new Scalar(0x00, 0xd8, 0xff));
            }

            return imglumenProfile;
        }

        public static List<int> GetCalciumList(List<LumenContour> lumenContours, int calciumThreshold)
        {
            List<int> calciumList = new List<int>();

            for(int i = 0; i<lumenContours.Count; i++)
            {
                if (lumenContours[i].Calcium == null)
                    continue;

                if (lumenContours[i].Calcium.TotalAngle >= calciumThreshold)
                    calciumList.Add(i);
            }

            return calciumList;
        }

        public static List<int> GetExpansionList(List<LumenContour> lumenContours, int frameProximal, int frameDistal, double refArea, int expansionThreshold)
        {
            List<int> expansionList = new List<int>();

            for (int i = frameProximal; i <= frameDistal && refArea != 0; i++)
            {
                double expansion = lumenContours[i].Area / refArea * 100;
                if (expansion <= expansionThreshold)
                    expansionList.Add(i);
            }

            return expansionList;
        }

        public static async Task SaveStillFrame(Mat image, string rootPath, string fileName, string format, Action<double> progressCallback, double progressIncrease, Action<string> progressTextCallback)
        {
            string filePath = rootPath + "\\" + fileName + "." + format.ToLower();
            ImageEncodingParam encodingParam = new ImageEncodingParam(ImwriteFlags.JpegQuality, 100);

            await Task.Run(() =>
            {
                Cv2.ImWrite(filePath, image, encodingParam);
                progressCallback(progressIncrease);
                progressTextCallback(Constants.ExportStatusSaveMultipleFiles);
            });
        }

        public static async Task SaveVideo(List<Mat> images, string rootPath, string fileName, string format, double fps, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {
            string filePath = rootPath + "\\" + fileName + "." + format.ToLower();

            if (images == null || images.Count == 0) return;

            Size szVideo = images[0].Size();

            VideoWriter videoWriter = new VideoWriter(filePath, FourCC.H264, fps, szVideo);
            if (videoWriter != null)
            {
                int totalNum = images.Count;
                foreach (Mat img in images)
                {
                    await Task.Run(() =>
                    {
                        videoWriter.Write(img);
                        progressCallback(progress / totalNum);
                        progressTextCallback(Constants.ExportStatusSaveVideo);
                    });
                }
                videoWriter.Release();
            }
        }

        public static async Task SaveMultipleFrames(List<Mat> images, string rootPath, string fileName, string format, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {
            string filePath = rootPath + "\\" + fileName + "." + format.ToLower();

            if (images == null || images.Count == 0) return;

            using (var tiff = Tiff.Open(filePath, "w"))
            {
                if (tiff != null)
                {
                    int totalNum = images.Count;
                    for (int i = 0; i < images.Count; i++)
                    {
                        await Task.Run(() =>
                        {
                            Mat img = images[i];
                            int size = img.Rows * img.Cols * img.Channels();
                            Cv2.CvtColor(img, img, ColorConversionCodes.RGB2BGR);

                            byte[] managedArray = new byte[size];
                            Marshal.Copy(img.Data, managedArray, 0, size);

                            tiff.SetField(TiffTag.IMAGEWIDTH, img.Cols);
                            tiff.SetField(TiffTag.IMAGELENGTH, img.Rows);
                            tiff.SetField(TiffTag.SAMPLESPERPIXEL, img.Channels());
                            tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
                            tiff.SetField(TiffTag.ROWSPERSTRIP, img.Rows);
                            tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
                            tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

                            tiff.WriteEncodedStrip(0, managedArray, managedArray.Length);
                            tiff.WriteDirectory();

                            progressCallback(progress / totalNum);
                            progressTextCallback(Constants.ExportStatusSaveMultipleFrames);
                        });
                    }

                    tiff.Close();
                }
            }
        }

        /*
         * https://github.com/Raywatt/Sejong/issues/27
         */
        public static double GetVideoSize(int width, int height, double frameRate, double targetMbps, double frameNum)
        {
            int numOfChannel = 3;
            int bytesPerRow = width * numOfChannel;
            double totalBytes = bytesPerRow * height * frameNum;

            double eachImageSize = totalBytes / frameNum;
            double needBitrate = eachImageSize * frameRate * 8 / (1024 * 1024) /* MB */;
            double compressionRatio = needBitrate / targetMbps;
            double compressedImages = totalBytes / compressionRatio;
            double fileSize = compressedImages;

            return fileSize;
        }

        public static async Task CopyStream(Stream from, Stream to, Action<long> progress)
        {
            try
            {
                int buffer_size = 1024 * 1024; // 1MB buffer

                byte[] buffer = new byte[buffer_size];

                long total_read = 0;

                while (total_read < from.Length)
                {
                    int read = await from.ReadAsync(buffer, 0, buffer_size);

                    await to.WriteAsync(buffer, 0, read);

                    total_read += read;

                    progress(total_read);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public static double ByteToGB(double bytes)
        {
            double div = 1024.0;
            return Math.Round(bytes / div / div / div, 3);
        }
        public static double ByteToMB(double bytes)
        {
            double div = 1024.0;
            return Math.Round(bytes / div / div, 3);
        }

        public static double ByteToKB(double bytes)
        {
            double div = 1024.0;
            return Math.Round(bytes / div, 3);
        }

        public static PathGeometry? GetLine(System.Windows.Point firstPoint, System.Windows.Point secondPoint)
        {
            var myPathFigure = new PathFigure { StartPoint = firstPoint };
            var myPathSegmentCollection = new PathSegmentCollection();
            var myLineSegment = new LineSegment { Point = secondPoint };
            myPathSegmentCollection.Add(myLineSegment);
            myPathFigure.Segments = myPathSegmentCollection;
            var myPathFigureCollection = new PathFigureCollection { myPathFigure };
            var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };

            return myPathGeometry;
        }

        public static PathGeometry? GetBezierCurve(List<System.Windows.Point> pointList, bool isClosed)
        {
            if (pointList == null)
                return null;

            var points = new List<Rulyotano.Math.Geometry.Point>();

            foreach (var point in pointList)
            {
                points.Add(new Rulyotano.Math.Geometry.Point(point.X, point.Y));
            }

            if (points.Count <= 1)
                return null;

            var myPathFigure = new PathFigure { StartPoint = ConvertToVisualPoint(points.FirstOrDefault()) };
            var myPathSegmentCollection = new PathSegmentCollection();
            var bezierSegments = Rulyotano.Math.Interpolation.Bezier.BezierInterpolation.PointsToBezierCurves(points, isClosed);

            if (bezierSegments == null || bezierSegments.Count < 1)
            {
                //Add a line segment <this is generic for more than one line>
                foreach (var point in points.GetRange(1, points.Count - 1))
                {
                    var myLineSegment = new LineSegment { Point = ConvertToVisualPoint(point) };
                    myPathSegmentCollection.Add(myLineSegment);
                }
            }
            else
            {
                foreach (var bezierCurveSegment in bezierSegments)
                {
                    var segment = new BezierSegment
                    {
                        Point1 = ConvertToVisualPoint(bezierCurveSegment.FirstControlPoint),
                        Point2 = ConvertToVisualPoint(bezierCurveSegment.SecondControlPoint),
                        Point3 = ConvertToVisualPoint(bezierCurveSegment.EndPoint)
                    };
                    myPathSegmentCollection.Add(segment);
                }
            }

            myPathFigure.Segments = myPathSegmentCollection;
            var myPathFigureCollection = new PathFigureCollection { myPathFigure };
            var myPathGeometry = new PathGeometry { Figures = myPathFigureCollection };

            return myPathGeometry;
        }

        public static System.Windows.Point GetScaledPoint(System.Windows.Point point, double xScale, double yScale)
        {
            System.Windows.Point ptScaled = new System.Windows.Point();
            ptScaled.X = point.X * xScale;
            ptScaled.Y = point.Y * yScale;

            return ptScaled;
        }
        private static System.Windows.Point ConvertToVisualPoint(Rulyotano.Math.Geometry.Point p)
        {
            return new System.Windows.Point(p.X, p.Y);
        }

        public static async void CheckFileSaveDone(string filePath, long totalFileSize, Action<double> progressCallback, double progressStart, double progress, Action<string> progressTextCallback)
        {
            long curFileSize = 0;

            while (true)
            {
                await Task.Run(() =>
                {
                    curFileSize = GetFileSize(filePath);

                    if (curFileSize != 0)
                    {
                        progressCallback(progressStart + progress * (1.0 * curFileSize / totalFileSize));
                        progressTextCallback(Constants.ExportStatusSaveFile);
                    }
                });

                if (totalFileSize == curFileSize)
                    break;
            }
        }

        public static int GetFrameFromPosition(double position, int totalFrame, double longitudeWidth, double indicatorDiff)
        {
            int frame = 0;
            double curPosition = (position + indicatorDiff) / longitudeWidth;

            curPosition *= (totalFrame - 1);
            frame = (int)Math.Round(curPosition);

            return frame;
        }

        public static double GetPositionFromFrame(int curFrame, int totalFrame, double longitudeWidth, double indicatorDiff)
        {
            double position = 0;

            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= longitudeWidth;
            position = curPosition - indicatorDiff;

            return position;
        }

        public static bool IsPreCase(string procedure)
        {
            if ("$001".Contains(procedure))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsPostCase(string procedure)
        {
            if ("$002|$003".Contains(procedure))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static System.Windows.Size GetTextBlockSize(string style, string text, int digits)
        {
            string[] temp = text.Split(".");
            if (temp != null && temp.Length == 2)
            {
                temp[1] = temp[1].Replace("㎜", "").Replace("㎟", "");

                for(int i = temp[1].Length; i < digits; i++)
                {
                    text = text + "0";
                }
            }

            TextBlock textBlock = new TextBlock();
            textBlock.Style = (System.Windows.Style)App.Current.Resources[style];
            textBlock.Text = text;

            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

            return textBlock.DesiredSize;
        }

        public static void Exit(DeviceStatus? deviceStatus)
        {
            if (deviceStatus != null)
            {
                deviceStatus.IsPaused = true;
                while (!deviceStatus.CanExit)
                {
                    Thread.Sleep(50);
                }
            }

            RayDisconnectDevices();
            RayStopSystem();

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));

            System.Windows.Application.Current.MainWindow.Close();
        }

        public static string LumenContoursToJson(List<LumenContour> lumenContours)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                string strPoint;

                writer.WriteStartArray();

                foreach (LumenContour lumenContour in lumenContours)
                {
                    lumenContour.SetOriginData(true);

                    writer.WriteStartObject();

                    //MlContour
                    {
                        writer.WritePropertyName(nameof(lumenContour.MlContour));
                        {
                            writer.WriteStartObject();

                            //Points
                            writer.WritePropertyName(nameof(lumenContour.MlContour.Points));

                            writer.WriteStartArray();

                            foreach (System.Windows.Point point in lumenContour.MlContour.Points)
                            {
                                strPoint = point.X + "," + point.Y;
                                writer.WriteValue(strPoint);
                            }
                            writer.WriteEndArray();

                            //Area
                            writer.WritePropertyName(nameof(lumenContour.MlContour.Area));
                            writer.WriteValue(lumenContour.MlContour.Area);

                            //CenterOfMass
                            writer.WritePropertyName(nameof(lumenContour.MlContour.CenterOfMass));
                            strPoint = lumenContour.MlContour.CenterOfMass.X + "," + lumenContour.MlContour.CenterOfMass.Y;
                            writer.WriteValue(strPoint);

                            //MinDiameter
                            {
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MinDiameter));
                                writer.WriteStartObject();

                                //point1
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MinDiameter.point1));
                                strPoint = lumenContour.MlContour.MinDiameter.point1.X + "," + lumenContour.MlContour.MinDiameter.point1.Y;
                                writer.WriteValue(strPoint);

                                //point2
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MinDiameter.point2));
                                strPoint = lumenContour.MlContour.MinDiameter.point2.X + "," + lumenContour.MlContour.MinDiameter.point2.Y;
                                writer.WriteValue(strPoint);

                                //value
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MinDiameter.value));
                                writer.WriteValue(lumenContour.MlContour.MinDiameter.value);

                                writer.WriteEndObject();
                            }

                            //MaxDiameter
                            {
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MaxDiameter));
                                writer.WriteStartObject();

                                //point1
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MaxDiameter.point1));
                                strPoint = lumenContour.MlContour.MaxDiameter.point1.X + "," + lumenContour.MlContour.MaxDiameter.point1.Y;
                                writer.WriteValue(strPoint);

                                //point2
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MaxDiameter.point2));
                                strPoint = lumenContour.MlContour.MaxDiameter.point2.X + "," + lumenContour.MlContour.MaxDiameter.point2.Y;
                                writer.WriteValue(strPoint);

                                //value
                                writer.WritePropertyName(nameof(lumenContour.MlContour.MaxDiameter.value));
                                writer.WriteValue(lumenContour.MlContour.MaxDiameter.value);

                                writer.WriteEndObject();
                            }

                            //MeanDiameter
                            writer.WritePropertyName(nameof(lumenContour.MlContour.MeanDiameter));
                            writer.WriteValue(lumenContour.MlContour.MeanDiameter);

                            //Valid
                            writer.WritePropertyName(nameof(lumenContour.MlContour.Valid));
                            writer.WriteValue(lumenContour.MlContour.Valid);

                            writer.WriteEndObject();
                        }
                    }

                    //Contour
                    {
                        //Points
                        writer.WritePropertyName(nameof(lumenContour.Points));

                        writer.WriteStartArray();

                        foreach (System.Windows.Point point in lumenContour.Points)
                        {
                            strPoint = point.X + "," + point.Y;
                            writer.WriteValue(strPoint);
                        }
                        writer.WriteEndArray();

                        //Area
                        writer.WritePropertyName(nameof(lumenContour.Area));
                        writer.WriteValue(lumenContour.Area);

                        //CenterOfMass
                        writer.WritePropertyName(nameof(lumenContour.CenterOfMass));
                        strPoint = lumenContour.CenterOfMass.X + "," + lumenContour.CenterOfMass.Y;
                        writer.WriteValue(strPoint);

                        //MinDiameter
                        {
                            writer.WritePropertyName(nameof(lumenContour.MinDiameter));
                            writer.WriteStartObject();

                            //point1
                            writer.WritePropertyName(nameof(lumenContour.MinDiameter.point1));
                            strPoint = lumenContour.MinDiameter.point1.X + "," + lumenContour.MinDiameter.point1.Y;
                            writer.WriteValue(strPoint);

                            //point2
                            writer.WritePropertyName(nameof(lumenContour.MinDiameter.point2));
                            strPoint = lumenContour.MinDiameter.point2.X + "," + lumenContour.MinDiameter.point2.Y;
                            writer.WriteValue(strPoint);

                            //value
                            writer.WritePropertyName(nameof(lumenContour.MinDiameter.value));
                            writer.WriteValue(lumenContour.MinDiameter.value);

                            writer.WriteEndObject();
                        }

                        //MaxDiameter
                        {
                            writer.WritePropertyName(nameof(lumenContour.MaxDiameter));
                            writer.WriteStartObject();

                            //point1
                            writer.WritePropertyName(nameof(lumenContour.MaxDiameter.point1));
                            strPoint = lumenContour.MaxDiameter.point1.X + "," + lumenContour.MaxDiameter.point1.Y;
                            writer.WriteValue(strPoint);

                            //point2
                            writer.WritePropertyName(nameof(lumenContour.MaxDiameter.point2));
                            strPoint = lumenContour.MaxDiameter.point2.X + "," + lumenContour.MaxDiameter.point2.Y;
                            writer.WriteValue(strPoint);

                            //value
                            writer.WritePropertyName(nameof(lumenContour.MaxDiameter.value));
                            writer.WriteValue(lumenContour.MaxDiameter.value);

                            writer.WriteEndObject();
                        }

                        //MeanDiameter
                        writer.WritePropertyName(nameof(lumenContour.MeanDiameter));
                        writer.WriteValue(lumenContour.MeanDiameter);

                        //Valid
                        writer.WritePropertyName(nameof(lumenContour.Valid));
                        writer.WriteValue(lumenContour.Valid);

                        //HasSidebranch
                        writer.WritePropertyName(nameof(lumenContour.HasSidebranch));
                        writer.WriteValue(lumenContour.HasSidebranch);

                        //Calcium
                        {
                            writer.WritePropertyName(nameof(lumenContour.Calcium));
                            writer.WriteStartObject();

                            //List
                            writer.WritePropertyName(nameof(lumenContour.Calcium.List));

                            writer.WriteStartArray();

                            foreach (Tuple<double, double> calcium in lumenContour.Calcium.List)
                            {
                                strPoint = calcium.Item1 + "," + calcium.Item2;
                                writer.WriteValue(strPoint);
                            }
                            writer.WriteEndArray();

                            //TotalAngle
                            writer.WritePropertyName(nameof(lumenContour.Calcium.TotalAngle));
                            writer.WriteValue(lumenContour.Calcium.TotalAngle);

                            //MaxThickness
                            writer.WritePropertyName(nameof(lumenContour.Calcium.MaxThickness));
                            writer.WriteValue(lumenContour.Calcium.MaxThickness);

                            //MaxThicknessDegree
                            writer.WritePropertyName(nameof(lumenContour.Calcium.MaxThicknessDegree));
                            writer.WriteValue(lumenContour.Calcium.MaxThicknessDegree);

                            writer.WriteEndObject();
                        }
                    }

                    writer.WriteEndObject();

                    lumenContour.SetOriginData(false);
                }

                writer.WriteEndArray();
            }

            return sb.ToString();
        }

        public static List<LumenContour> JsonToLumenContours(string strLumenContours)
        {
            List<LumenContour> lumenContours = new List<LumenContour>();

            JsonTextReader reader = new JsonTextReader(new StringReader(strLumenContours));
            string currentProperty = string.Empty;

            while (reader.Read())
            {
                //LumenContour
                if (reader.Depth == 1 && reader.TokenType == JsonToken.StartObject)
                {
                    LumenContour lumenContour = new LumenContour();

                    while (reader.Read())
                    {
                        if (reader.Depth == 1 && reader.TokenType == JsonToken.EndObject)
                        {
                            lumenContours.Add(lumenContour);
                            break;
                        }

                        if (reader.TokenType == JsonToken.PropertyName)
                            currentProperty = reader.Value.ToString();

                        if(reader.Depth == 2)
                        {
                            if (nameof(lumenContour.MlContour).Equals(currentProperty))
                            {
                                lumenContour.MlContour = new Contour();
                                while (reader.Read())
                                {
                                    if (reader.Depth == 2 && reader.TokenType == JsonToken.EndObject)
                                        break;

                                    if (reader.TokenType == JsonToken.PropertyName)
                                        currentProperty = reader.Value.ToString();

                                    if (reader.Depth == 3)
                                    {
                                        SetContour(reader, currentProperty, lumenContour.MlContour);
                                    }
                                }
                            }
                            else if (nameof(lumenContour.Calcium).Equals(currentProperty))
                            {
                                lumenContour.Calcium = new Calcium();
                                while (reader.Read())
                                {
                                    if (reader.Depth == 2 && reader.TokenType == JsonToken.EndObject)
                                        break;

                                    if (reader.TokenType == JsonToken.PropertyName)
                                        currentProperty = reader.Value.ToString();

                                    if (reader.Depth == 3)
                                    {
                                        SetCalcium(reader, currentProperty, lumenContour);
                                    }
                                }
                            }
                            else if (nameof(lumenContour.HasSidebranch).Equals(currentProperty))
                            {
                                if (reader.Value != null && reader.TokenType == JsonToken.Boolean)
                                    lumenContour.HasSidebranch = (bool)reader.Value;
                            }
                            else
                            {
                                SetContour(reader, currentProperty, lumenContour);
                            }
                        }
                    }
                }
            }

            return lumenContours;
        }

        unsafe public static void ContoursToMemory(List<LumenContour>? contourList, Size sizeContour, IntPtr buffer, Size sizeBuffer)
        {
            if (contourList == null) return;

            int frameSize = sizeBuffer.Width * sizeBuffer.Height;
            for (int i = 0; i < contourList.Count; i++)
            {
                Mat imgLumen = new Mat(sizeContour, MatType.CV_8UC1);
                Mat imgResize = new Mat(sizeBuffer, MatType.CV_8UC1);
                List<List<Point>> contours = new List<List<Point>>();
                List<Point> contour = new List<Point>();
                foreach (System.Windows.Point point in contourList[i].Points)
                {
                    contour.Add(new OpenCvSharp.Point(point.X, point.Y));
                }
                if (contour.Count > 0)
                {
                    contours.Add(contour);
                }

                imgLumen.SetTo(Scalar.Black);
                if (contours.Count > 0)
                {
                    Cv2.DrawContours(imgLumen, contours, -1, Scalar.White, -1);
                }
                Cv2.Resize(imgLumen, imgResize, imgResize.Size());

                Buffer.MemoryCopy((void*)imgResize.Data, (void*)(IntPtr.Add(buffer, i * frameSize)), frameSize, frameSize);
            }
        }

        private static System.Windows.Point StrToPoint(string str)
        {
            string[] temp = str.Split(",");
            return new System.Windows.Point(double.Parse(temp[0]), double.Parse(temp[1]));
        }

        private static Tuple<double, double> StrToTuple(string str)
        {
            string[] temp = str.Split(",");
            return new Tuple<double, double>(double.Parse(temp[0]), double.Parse(temp[1]));
        }

        private static void SetContour(JsonTextReader reader, string currentProperty, Contour lumenContour)
        {
            switch (currentProperty)
            {
                case nameof(lumenContour.Points):
                    lumenContour.Points = new List<System.Windows.Point>();
                    SetPoints(reader, lumenContour.Points);
                    break;
                case nameof(lumenContour.Area):
                    if (reader.Value != null && reader.TokenType == JsonToken.Float)
                        lumenContour.Area = double.Parse(reader.Value.ToString());
                    break;
                case nameof(lumenContour.CenterOfMass):
                    if (reader.Value != null && reader.TokenType == JsonToken.String)
                        lumenContour.CenterOfMass = StrToPoint(reader.Value.ToString());
                    break;
                case nameof(lumenContour.MinDiameter):
                    lumenContour.MinDiameter = new DiameterInfo();
                    SetDiameterInfo(reader, lumenContour.MinDiameter);
                    break;
                case nameof(lumenContour.MaxDiameter):
                    lumenContour.MaxDiameter = new DiameterInfo();
                    SetDiameterInfo(reader, lumenContour.MaxDiameter);
                    break;
                case nameof(lumenContour.MeanDiameter):
                    if (reader.Value != null && reader.TokenType == JsonToken.Float)
                        lumenContour.MeanDiameter = (double)reader.Value;
                    break;
                case nameof(lumenContour.Valid):
                    if (reader.Value != null && reader.TokenType == JsonToken.Boolean)
                        lumenContour.Valid = (bool)reader.Value;
                    break;
                default:
                    break;
            }
        }

        private static void SetCalcium(JsonTextReader reader, string currentProperty, LumenContour lumenContour)
        {
            switch (currentProperty)
            {
                case nameof(lumenContour.Calcium.List):
                    lumenContour.Calcium.List = new List<Tuple<double, double>>();
                    SetTuples(reader, lumenContour.Calcium.List);
                    break;
                case nameof(lumenContour.Calcium.TotalAngle):
                    if (reader.Value != null && reader.TokenType == JsonToken.Integer)
                        lumenContour.Calcium.TotalAngle = (int)(long)reader.Value;
                    break;
                case nameof(lumenContour.Calcium.MaxThickness):
                    if (reader.Value != null && reader.TokenType == JsonToken.Float)
                        lumenContour.Calcium.MaxThickness = (double)reader.Value;
                    break;
                case nameof(lumenContour.Calcium.MaxThicknessDegree):
                    if (reader.Value != null && reader.TokenType == JsonToken.Float)
                        lumenContour.Calcium.MaxThicknessDegree = (double)reader.Value;
                    break;
                default:
                    break;
            }
        }

        private static void SetPoints(JsonTextReader reader, List<System.Windows.Point> points)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                if (reader.Value != null)
                    points.Add(StrToPoint(reader.Value.ToString()));
            }
        }

        private static void SetTuples(JsonTextReader reader, List<Tuple<double, double>> tuples)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                if (reader.Value != null)
                    tuples.Add(StrToTuple(reader.Value.ToString()));
            }
        }

        private static void SetDiameterInfo(JsonTextReader reader, DiameterInfo diameterInfo)
        {
            string currentProperty = string.Empty;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                    currentProperty = reader.Value.ToString();

                if (reader.Value != null)
                {
                    switch (currentProperty)
                    {
                        case nameof(diameterInfo.point1):
                            if (reader.TokenType == JsonToken.String)
                                diameterInfo.point1 = StrToPoint(reader.Value.ToString());
                            break;
                        case nameof(diameterInfo.point2):
                            if (reader.TokenType == JsonToken.String)
                                diameterInfo.point2 = StrToPoint(reader.Value.ToString());
                            break;
                        case nameof(diameterInfo.value):
                            if (reader.TokenType == JsonToken.Float)
                                diameterInfo.value = (double)reader.Value;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public static BitmapSource DrawCalciumIndicator(List<Tuple<double, double>> calciumAngleList, int rgbCode, int calciumIndicatorSize)
        {
            int r = (rgbCode >> 16) & 0xFF;
            int g = (rgbCode >> 8) & 0xFF;
            int b = (rgbCode >> 0) & 0xFF;

            Mat imgCalcium = new Mat(calciumIndicatorSize, calciumIndicatorSize, MatType.CV_8UC4);
            Point center = new Point(imgCalcium.Width / 2, imgCalcium.Height / 2);
            int thickness = 3;
            int radius = (imgCalcium.Width / 2) - thickness;

            imgCalcium.SetTo(new Scalar(0x00, 0x00, 0x00, 0x00));
            imgCalcium.Circle(center, radius, new Scalar(b, g, r, 0xff), thickness, LineTypes.AntiAlias);

            List<Tuple<double, double>> nonCalciumAngleList = new List<Tuple<double, double>>
            {
                new Tuple<double, double>(0, 360)
            };

            foreach (var calciumArea in calciumAngleList)
            {
                double calciumStart = calciumArea.Item1;
                double calciumEnd = calciumArea.Item1 + calciumArea.Item2;
                for (int i = nonCalciumAngleList.Count - 1; i >= 0; i--)
                {
                    Tuple<double, double> nonCalciumArea = nonCalciumAngleList[i];
                    double nonCalciumStart = nonCalciumArea.Item1;
                    double nonCalciumEnd = nonCalciumArea.Item1 + nonCalciumArea.Item2;
                    if (calciumStart >= nonCalciumStart && calciumEnd <= nonCalciumEnd)
                    {
                        nonCalciumAngleList.RemoveAt(i);
                        if (calciumStart > nonCalciumStart)
                        {
                            Tuple<double, double> splitArea = new Tuple<double, double>(nonCalciumStart, calciumStart - nonCalciumStart);
                            nonCalciumAngleList.Add(splitArea);
                        }
                        if (calciumEnd < nonCalciumEnd)
                        {
                            Tuple<double, double> splitArea = new Tuple<double, double>(calciumEnd, nonCalciumEnd - calciumEnd);
                            nonCalciumAngleList.Add(splitArea);
                        }

                        break;
                    }
                }
            }

            foreach (var calciumArea in nonCalciumAngleList)
            {
                imgCalcium.Ellipse(center,
                    new OpenCvSharp.Size(imgCalcium.Width / 2, imgCalcium.Height / 2),
                    0,
                    calciumArea.Item1,
                    calciumArea.Item1 + calciumArea.Item2,
                    new Scalar(0x00, 0x00, 0x00, 0x00),
                    -1);
            }

            BitmapSource bitmap = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCalcium);
            return bitmap;
        }

        public static bool IsTestMode(Dictionary<string, bool> testMode, string key)
        {
            if (!testMode.ContainsKey(key))
                return false;

            return testMode[key];
        }
    }
}
