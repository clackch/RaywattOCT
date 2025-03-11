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
using RaywattApp.Common.Angio;
using System.Xml;
using Python.Runtime;
using FFMpegCore;

namespace RaywattApp.Common.Util
{
    public class CommonUtil
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CommonUtil));

        public static bool isVTIFileSave = false;

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

        public static bool ValidateRealNumber(string input)
        {
            var regex = new Regex(@"^[-+]?\d*\.?\d*$");

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
                                for (int i = 0; i < 9; i++)
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
            if (oldPath != newPath)
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

        public static async Task<Mat> ConvertImage(string filePath, double imageResolution, int zOffset, double degree, List<Mat> convertedImages, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {
            RayOpenImage(filePath, imageResolution, zOffset);

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
            double dialogWidth = dialogDataContext.SetInitialize(patientCase, imgCrossSections, imgLongitude, fileExport);

            window.Show();
            window.Hide();

            dialog.Width = dialogWidth;
            dialog.Height = originHeight;

            int totalCnt = imgCrossSections.Count;
            if (exportIndices != null)
                totalCnt = exportIndices.Count;

            for (int i = 0; i < totalCnt; i++)
            {
                int index = i;
                if (exportIndices != null)
                    index = exportIndices[i];

                dialogDataContext.SetFrameNumber(index);
                dialog.UpdateLayout();

                Size originalSize = new Size(dialog.ActualWidth, dialog.ActualHeight);
                dialog.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                dialog.Arrange(new System.Windows.Rect(0, 0, dialog.DesiredSize.Width, dialog.DesiredSize.Height));

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)dialog.DesiredSize.Width, (int)dialog.DesiredSize.Height, 96, 96, PixelFormats.Pbgra32);
                System.Windows.Rect bounds = VisualTreeHelper.GetDescendantBounds(dialog);
                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext ctx = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(dialog);
                    ctx.DrawRectangle(vb, null, bounds);
                }
                rtb.Render(dv);

                dialog.Measure(new System.Windows.Size(originalSize.Width, originalSize.Height));
                dialog.Arrange(new System.Windows.Rect(0, 0, originalSize.Width, originalSize.Height));

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

        public static Mat MakeImageForExport(Mat crossSection, Mat? longitude, Mat? lumenProfile, Mat? angio, out List<Tuple<Rect, Size2f>> region)
        {
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

            Mat imglumenProfile = new Mat(100, lumenContours.Count * 2 - 2, MatType.CV_8UC3);
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

        public static Mat MakeLumenProfileImage(List<LumenContour> lumenContours, List<LumenSidebranch> lumenSidebranches, List<LumenStent> lumenStents, double appositionThreshold, int frameProximal, int frameDistal, bool isPostCase, int currentFrame = -1)
        {
            if (lumenContours == null || lumenContours.Count <= 0) return null;

            int cols = currentFrame == -1 ? lumenContours.Count : currentFrame + 1;

            Mat imglumenProfile = new Mat(200, lumenContours.Count * 2 - 2, MatType.CV_8UC3);
            imglumenProfile.SetTo(new Scalar(0x33, 0x33, 0x33));

            for (int curFrame = 0; curFrame < cols; curFrame++)
            {
                imglumenProfile = MakeLumenProfile(imglumenProfile, lumenContours[curFrame], lumenSidebranches[curFrame], lumenStents[curFrame], appositionThreshold, curFrame, frameProximal, frameDistal, isPostCase, (curFrame == lumenContours.Count - 1));
            }

            return imglumenProfile;
        }

        public static Mat MakeLumenProfileImageOneByOne(Mat imglumenProfile, List<LumenContour> lumenContours, List<LumenSidebranch> lumenSidebranches, List<LumenStent> lumenStents, double appositionThreshold, int frameProximal, int frameDistal, bool isPostCase, int currentFrame = -1)
        {
            if (lumenContours == null || lumenContours.Count <= 0) return null;

            if (imglumenProfile == null)
            {
                imglumenProfile = MakeLumenProfileImage(lumenContours, lumenSidebranches, lumenStents, appositionThreshold, frameProximal, frameDistal, isPostCase, currentFrame);
            }

            int curFrame = currentFrame == -1 ? lumenContours.Count - 1 : currentFrame;
            imglumenProfile = MakeLumenProfile(imglumenProfile, lumenContours[curFrame], lumenSidebranches[curFrame], lumenStents[curFrame], appositionThreshold, curFrame, frameProximal, frameDistal, isPostCase, (curFrame == lumenContours.Count - 1));

            return imglumenProfile;
        }

        private static Mat MakeLumenProfile(Mat imglumenProfile, LumenContour lumenContour, LumenSidebranch lumenSidebranch, LumenStent lumenStent, double appositionThreshold, int curFrame, int frameProximal, int frameDistal, bool isPostCase, bool isEdge)
        {
            const double radius = Constants.OCTImageSize / 3;
            const double totalArea = radius * radius * Math.PI;
            double area = lumenContour.Area;
            int lumenArea = (int)(area / totalArea * imglumenProfile.Rows);
            int yStart = (imglumenProfile.Rows - lumenArea) / 2;

            int position = 0;

            if (curFrame != 0)
                position = curFrame * 2 - 1;

            if (area > 0)
            {
                //Lumen Area
                Cv2.Line(imglumenProfile, new Point(position, yStart), new Point(position, yStart + lumenArea), new Scalar(0x16, 0x16, 0x16));
                if (!isEdge)
                    Cv2.Line(imglumenProfile, new Point(position + 1, yStart), new Point(position + 1, yStart + lumenArea), new Scalar(0x16, 0x16, 0x16));

                //Lesion Section
                if (curFrame >= frameProximal && curFrame <= frameDistal)
                {
                    Cv2.Line(imglumenProfile, new Point(position, 0), new Point(position, yStart - 1), new Scalar(0x4f, 0x4f, 0x4f));
                    Cv2.Line(imglumenProfile, new Point(position, yStart + lumenArea + 1), new Point(position, imglumenProfile.Rows), new Scalar(0x4f, 0x4f, 0x4f));
                    if (!isEdge)
                        Cv2.Line(imglumenProfile, new Point(position + 1, 0), new Point(position + 1, yStart - 1), new Scalar(0x4f, 0x4f, 0x4f));
                    if (!isEdge)
                        Cv2.Line(imglumenProfile, new Point(position + 1, yStart + lumenArea + 1), new Point(position + 1, imglumenProfile.Rows), new Scalar(0x4f, 0x4f, 0x4f));
                }
            }
            /* TO-DO : 문제있는 frame(area = 0, 이상한 lumen) 처리 필요
            else
            {
                Cv2.Line(imglumenProfile, new Point(position, 0), new Point(position, imglumenProfile.Rows), new Scalar(0x3f, 0x41, 0x76));
                if (!isEdge)
                    Cv2.Line(imglumenProfile, new Point(position + 1, 0), new Point(position + 1, imglumenProfile.Rows), new Scalar(0x3f, 0x41, 0x76));
            }*/

            //Stent Area
            if (isPostCase && lumenStent.Points != null && lumenStent.IsStent)
            {
                //MalApposition
                foreach (double appositionLength in lumenStent.AppositionLength)
                {
                    if (CommonUtil.IsMalApposition(appositionLength, appositionThreshold))
                    {
                        Cv2.Line(imglumenProfile, new Point(position, yStart), new Point(position, yStart + lumenArea), new Scalar(0x3f, 0x41, 0x76));
                        if (!isEdge)
                            Cv2.Line(imglumenProfile, new Point(position + 1, yStart), new Point(position + 1, yStart + lumenArea), new Scalar(0x3f, 0x41, 0x76));
                        break;
                    }
                }

                //Stent
                for (int i = 0; i < imglumenProfile.Rows; i++)
                {
                    if ((i + curFrame) % 20 == 0)
                    {
                        Cv2.Line(imglumenProfile, new Point(position, i), new Point(position, i), new Scalar(0x8d, 0x8d, 0x8d));
                        if (!isEdge)
                            Cv2.Line(imglumenProfile, new Point(position + 1, i), new Point(position + 1, i), new Scalar(0x8d, 0x8d, 0x8d));
                    }
                    if ((i - curFrame) % 20 == 0)
                    {
                        Cv2.Line(imglumenProfile, new Point(position, i), new Point(position, i), new Scalar(0x8d, 0x8d, 0x8d));
                        if (!isEdge)
                            Cv2.Line(imglumenProfile, new Point(position + 1, i), new Point(position + 1, i), new Scalar(0x8d, 0x8d, 0x8d));
                    }
                }
            }

            //Side Branch
            if (lumenSidebranch.Points != null && lumenSidebranch.Points.Count > 0)
            {
                int sbThickness = 5;
                if (lumenArea / 2 < sbThickness)
                    sbThickness = lumenArea / 2 - 1;

                if (curFrame >= frameProximal && curFrame <= frameDistal)
                {
                    Cv2.Line(imglumenProfile, new Point(position, imglumenProfile.Rows / 2 - sbThickness), new Point(position, imglumenProfile.Rows / 2 + sbThickness), new Scalar(0xe4, 0xe4, 0xe4));
                    if (!isEdge)
                        Cv2.Line(imglumenProfile, new Point(position + 1, imglumenProfile.Rows / 2 - sbThickness), new Point(position + 1, imglumenProfile.Rows / 2 + sbThickness), new Scalar(0xe4, 0xe4, 0xe4));
                }
                else
                {
                    Cv2.Line(imglumenProfile, new Point(position, imglumenProfile.Rows / 2 - sbThickness), new Point(position, imglumenProfile.Rows / 2 + sbThickness), new Scalar(0x7d, 0x7d, 0x7d));
                    if (!isEdge)
                        Cv2.Line(imglumenProfile, new Point(position + 1, imglumenProfile.Rows / 2 - sbThickness), new Point(position + 1, imglumenProfile.Rows / 2 + sbThickness), new Scalar(0x7d, 0x7d, 0x7d));
                }
            }

            return imglumenProfile;
        }

        public static Mat MakeLumenProfileImageExtra(int frameCnt, List<int> colorFrames, bool isPreCase, int currentFrame = -1)
        {
            int cols = currentFrame == -1 ? frameCnt : currentFrame + 1;

            Mat imglumenProfile = new Mat(20, frameCnt * 2 - 2, MatType.CV_8UC3);
            imglumenProfile.SetTo(new Scalar(0x33, 0x33, 0x33));

            for (int i = 0; i < cols; i++)
            {
                MakeLumenProfileExtra(imglumenProfile, i, frameCnt, colorFrames, isPreCase, (i == frameCnt - 1));
            }

            return imglumenProfile;
        }

        public static Mat MakeLumenProfileImageExtraOneByOne(Mat imglumenProfile, int frameCnt, List<int> colorFrames, bool isPreCase, int currentFrame = -1)
        {
            if (imglumenProfile == null)
            {
                imglumenProfile = MakeLumenProfileImageExtra(frameCnt, colorFrames, isPreCase, currentFrame);
            }

            int curFrame = currentFrame == -1 ? frameCnt - 1 : currentFrame;

            imglumenProfile = MakeLumenProfileExtra(imglumenProfile, curFrame, frameCnt, colorFrames, isPreCase, (curFrame == frameCnt - 1));

            return imglumenProfile;
        }

        private static Mat MakeLumenProfileExtra(Mat imglumenProfile, int curFrame, int frameCnt, List<int> colorFrames, bool isPreCase, bool isEdge)
        {
            int position = 0;

            if (curFrame != 0)
                position = curFrame * 2 - 1;

            if (colorFrames.Contains(curFrame))
            {
                Cv2.Line(imglumenProfile, new Point(position, 0), new Point(position, imglumenProfile.Rows), isPreCase ? new Scalar(0xeb, 0xfe, 0x75) : new Scalar(0x00, 0xd8, 0xff));
                if (!isEdge)
                    Cv2.Line(imglumenProfile, new Point(position + 1, 0), new Point(position + 1, imglumenProfile.Rows), isPreCase ? new Scalar(0xeb, 0xfe, 0x75) : new Scalar(0x00, 0xd8, 0xff));
            }

            return imglumenProfile;
        }

        public static List<int> GetCalciumList(List<LumenContour> lumenContours, int calciumThreshold)
        {
            List<int> calciumList = new List<int>();

            for (int i = 0; i < lumenContours.Count; i++)
            {
                if (lumenContours[i].Calcium == null)
                    continue;

                if (lumenContours[i].Calcium.TotalAngle >= calciumThreshold)
                    calciumList.Add(i);
            }

            return calciumList;
        }

        public static List<int> GetExpansionList(List<LumenContour> lumenContours, int frameProximal, int frameDistal, int stentProximal, int stentDistal, double refArea, int expansionThreshold)
        {
            List<int> expansionList = new List<int>();

            int tempProximal = frameProximal > stentProximal ? frameProximal : stentProximal;
            int tempDistal = frameDistal < stentDistal ? frameDistal : stentDistal;

            for (int i = tempProximal; i <= tempDistal && refArea != 0; i++)
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
            string tempPath = ".\\temp.mp4";

            if (images == null || images.Count == 0) return;

            Size szVideo = images[0].Size();

            VideoWriter videoWriter = new VideoWriter(tempPath, FourCC.H264, fps, szVideo);
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

            // FFmpeg 명령 구성 및 실행
            // FourCC.H264 압축 사용하면, 비트레이트가 해상도/FPS/복잡성에 따라 달라져서 12Mbps로 변경 처리해서 예상 용량에 맞추기 위함(정확하게 12Mbps로 맞춰지지는 않음)
            await FFMpegArguments
                .FromFileInput(tempPath)
                .OutputToFile(filePath, true, options => options
                .WithCustomArgument("-b:v 12M") // 비디오 비트레이트 명시적 설정
                .ForceFormat("mp4"))
                .ProcessAsynchronously();

            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
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

        public static void TiffProcessByPython(List<Mat> images, string rootPath, string fileName, string format, CancellationTokenSource _cancellationTokenSource)
        {
            string filePath = rootPath + "\\" + fileName + "." + format.ToLower();

            if (images == null || images.Count == 0) return;

            string pythonDLL = Environment.GetEnvironmentVariable("PYTHON_DLL");
            if (!System.IO.File.Exists(pythonDLL))
            {
                _log.Error($"Error: Python DLL not found at {pythonDLL}");
                return;
            }
            Runtime.PythonDLL = pythonDLL;

            try
            {
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(".\\"); // Python 모듈 검색 경로에 디렉터리 추가
                    _log.Debug($"sys.path: {sys.path}");

                    // Python 모듈 가져오기
                    dynamic script = Py.Import("ImageProcess");

                    // Mat 리스트를 Python으로 전달
                    int width, height;
                    var pyMatList = ConvertMatListToPython(ProcessMatList(images, out width, out height));
                    string result = script.process_images(pyMatList, width, height, filePath);
                    _log.Debug($"Python function returned: {result}");

                    _cancellationTokenSource.Cancel();
                }
            }
            catch (Python.Runtime.PythonException ex)
            {
                _log.Error($"Python Error: {ex.Message}");
                _log.Error($"Traceback: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error: {ex.Message}");
            }
            finally
            {
                PythonEngine.Shutdown();
            }
        }

        private static dynamic ConvertMatListToPython(List<byte[]> matList)
        {
            var pythonList = new Python.Runtime.PyList();
            dynamic np = Py.Import("numpy");

            foreach (var mat in matList)
            {
                // NumPy 배열로 변환해서 Python 리스트에 추가
                pythonList.Append(np.array(mat));
            }
            return pythonList;
        }

        private static List<byte[]> ProcessMatList(List<Mat> matList, out int width, out int height)
        {
            var byteList = new List<byte[]>();
            width = 0;
            height = 0;

            try
            {
                foreach (var mat in matList)
                {
                    if (mat.Empty())
                    {
                        _log.Debug("Skipped empty Mat.");
                        continue;
                    }

                    // Mat 크기 확인
                    width = mat.Width;
                    height = mat.Height;

                    // Mat 데이터를 byte[]로 변환 (BGR -> RGB 변환 포함)
                    byte[] byteData = ConvertMatToByteArray(mat);
                    if (byteData != null)
                    {
                        byteList.Add(byteData);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error processing Mat list: {ex.Message}");
            }

            return byteList;
        }

        private static byte[] ConvertMatToByteArray(Mat mat)
        {
            try
            {
                // OpenCV에서 Mat 객체를 BGR -> RGB로 변환
                Mat rgbMat = new Mat();
                Cv2.CvtColor(mat, rgbMat, ColorConversionCodes.BGR2RGB);

                // byte[]로 변환
                byte[] byteData = new byte[rgbMat.Rows * rgbMat.Cols * rgbMat.Channels()];
                Marshal.Copy(rgbMat.Data, byteData, 0, byteData.Length);

                return byteData;
            }
            catch (Exception ex)
            {
                _log.Error($"Error converting Mat to byte array: {ex.Message}");
                return null;
            }
        }

        public static double GetVideoSize(double frameRate, double targetMbps, double frameNum)
        {
            // 동영상 길이 (초)
            double videoDuration = frameNum / frameRate;

            // 비트레이트 (bps로 변환)
            double bitrateBps = targetMbps * 1_000_000;

            // 예상 파일 크기 (바이트)
            double totalBytes = (bitrateBps * videoDuration) / 8;

            return totalBytes;
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

                for (int i = temp[1].Length; i < digits; i++)
                {
                    text = text + "0";
                }
            }
            else if (temp != null && temp.Length == 1 && digits > 0)
            {
                text += ".";

                for (int i = 0; i < digits; i++)
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

        public static void Exit(DeviceStatus? deviceStatus = null, AngioManager? angioManager = null, bool isShutdown = false)
        {
            if (deviceStatus != null)
            {
                deviceStatus.IsPowerOff = true;

                deviceStatus.IsPaused = true;
                while (!deviceStatus.CanExit)
                {
                    Thread.Sleep(50);
                }
            }

            if (angioManager != null)
                angioManager.CloseAngioManager();

            Thread threadReadyPullback = new Thread(() => ThreadExit(deviceStatus, isShutdown));
            threadReadyPullback.Start();
        }

        private static void ThreadExit(DeviceStatus? deviceStatus, bool isShutdown)
        {
            RayDisconnectDevices();
            RayStopSystem();

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.MainWindow.Close();

                if (deviceStatus == null)
                {
                    Win32Helper.Shutdown();
                }
                else if (!CommonUtil.IsTestMode(deviceStatus.TestMode, "Power"))
                {
                    if (isShutdown)
                        Win32Helper.Shutdown();
                    else
                        Win32Helper.LogOff();
                }
            });
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

                        //Calcium
                        {
                            writer.WritePropertyName(nameof(lumenContour.Calcium));
                            writer.WriteStartObject();

                            //List
                            writer.WritePropertyName(nameof(lumenContour.Calcium.List));

                            writer.WriteStartArray();

                            if (lumenContour.Calcium != null)
                            {
                                foreach (Tuple<double, double> calcium in lumenContour.Calcium.List)
                                {
                                    strPoint = calcium.Item1 + "," + calcium.Item2;
                                    writer.WriteValue(strPoint);
                                }
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

                        if (reader.Depth == 2)
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

        public static string CoRegistrationsToJson(List<CoRegistration> coRegistrations)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartArray();

                foreach (CoRegistration coRegistration in coRegistrations)
                {
                    //Tracking Points (Proximal, Distal and additional connetion Points)
                    writer.WriteStartObject();
                    writer.WritePropertyName(nameof(coRegistration.TrackPoints));
                    writer.WriteStartArray();
                  
                    foreach(System.Windows.Point point in  coRegistration.TrackPoints)
                    {
                        string strPoint = (int)point.X + "," + (int)point.Y;
                        writer.WriteValue(strPoint);
                    }

                    writer.WriteEndArray();

                    //Path Points
                    writer.WritePropertyName(nameof(coRegistration.Line));
                    writer.WriteStartArray();

                    foreach (List<System.Windows.Point> points in coRegistration.Line)
                    {
                        if (points.Count > 0)
                        {
                            writer.WriteStartArray();
                            foreach (System.Windows.Point point in points)
                            {
                                string strPoint = (int)point.X + "," + (int)point.Y;
                                writer.WriteValue(strPoint);
                            }
                            writer.WriteEndArray();
                        }
                    }

                    writer.WriteEndArray();

                    //Marker Point
                    writer.WritePropertyName(nameof(coRegistration.MarkerPoint));
                    string strMarkerPoint = (int)coRegistration.MarkerPoint.X + "," + (int)coRegistration.MarkerPoint.Y;
                    writer.WriteValue(strMarkerPoint);

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }

            return sb.ToString();
        }

        public static List<CoRegistration> JsonToCoRegistrations(string strCoRegistration)
        {
            List<CoRegistration> coRegistrations = new List<CoRegistration>();

            JsonTextReader reader = new JsonTextReader(new StringReader(strCoRegistration));

            string currentProperty = string.Empty;

            while (reader.Read())
            {
                if (reader.Depth == 1 && reader.TokenType == JsonToken.StartObject)
                {
                    CoRegistration coRegistration = new CoRegistration();

                    while (reader.Read())
                    {
                        if (reader.Depth == 1 && reader.TokenType == JsonToken.EndObject)
                        {
                            coRegistrations.Add(coRegistration);
                            break;
                        }

                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            currentProperty = reader.Value.ToString();
                        }

                        if (reader.Depth > 1 /*이유는 모르겠으나, Array 첫번째 요소가 depth 2로 출력됨. 같은 Array의 나머지 요소는 depth 3*/)
                        {
                            if (nameof(coRegistration.TrackPoints).Equals(currentProperty))
                            {
                                coRegistration.TrackPoints = new List<System.Windows.Point>();
                                SetContour(reader, currentProperty, null, coRegistration);
                            }
                            else if (nameof(coRegistration.Line).Equals(currentProperty))
                            {
                                coRegistration.Line = new List<List<System.Windows.Point>>();
                                SetContour(reader, currentProperty, null, coRegistration);
                            }
                            else if (nameof(coRegistration.MarkerPoint).Equals(currentProperty))
                            {
                                coRegistration.MarkerPoint = new System.Windows.Point();
                                while (reader.Read())
                                {
                                    if (reader.Value != null)
                                    {
                                        coRegistration.MarkerPoint = StrToPoint(reader.Value.ToString());
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return coRegistrations;
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
                Cv2.Blur(imgResize, imgResize, new Size(13, 13) /* 필터 크기 */, new Point(-1, -1) /* 필터 중심*/);

                Buffer.MemoryCopy((void*)imgResize.Data, (void*)(IntPtr.Add(buffer, i * frameSize)), frameSize, frameSize);
            }
        }
        unsafe public static void StentsToMemory(List<LumenStent>? stentList, Size sizeContour, IntPtr buffer, Size sizeBuffer)
        {
            if (stentList == null) return;

            int frameSize = sizeBuffer.Width * sizeBuffer.Height;
            for (int i = 0; i < stentList.Count; i++)
            {
                Mat imgLumen = new Mat(sizeContour, MatType.CV_8UC1);
                Mat imgResize = new Mat(sizeBuffer, MatType.CV_8UC1);
                Point[][] contours;
                List<Point> contour = new List<Point>();

                if (stentList[i].Points == null || !stentList[i].IsStent)
                    continue;

                imgLumen.SetTo(Scalar.Black);

                foreach (System.Windows.Point point in stentList[i].Points)
                {
                    //TODO - 실제 스텐트 두께에 맞춰서 Size( , )를 설정해 주어야 함.
                    imgLumen.Ellipse(new OpenCvSharp.Point(point.X, point.Y), new Size(5, 5), 0, 0, 360, Scalar.White, 1);
                }

                Mat binary = new Mat();
                Cv2.Threshold(imgLumen, binary, 128, 255, ThresholdTypes.Binary);

                Cv2.FindContours(binary, out contours, out HierarchyIndex[] hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                Cv2.DrawContours(imgLumen, contours, -1, Scalar.White, 1);

                Cv2.Resize(imgLumen, imgResize, imgResize.Size());
                Cv2.Blur(imgResize, imgResize, new Size(7, 7) /* 필터 크기 */, new Point(-1, -1) /* 필터 중심*/);
                Buffer.MemoryCopy((void*)imgResize.Data, (void*)(IntPtr.Add(buffer, i * frameSize)), frameSize, frameSize);
            }
        }

        unsafe public static void GuideWireToMemory(List<LumenGuidewire>? guidewireList, Size sizeContour, IntPtr buffer, Size sizeBuffer)
        {
            if (guidewireList == null) return;

            int frameSize = sizeBuffer.Width * sizeBuffer.Height;
            for (int i = 0; i < guidewireList.Count; i++)
            {
                Mat imgLumen = new Mat(sizeContour, MatType.CV_8UC1);
                Mat imgResize = new Mat(sizeBuffer, MatType.CV_8UC1);
                Point[][] contours;
                List<Point> contour = new List<Point>();

                imgLumen.SetTo(Scalar.Black);

                if (guidewireList[i].Points == null)
                    continue;

                foreach (System.Windows.Point point in guidewireList[i].Points)
                {
                    //TODO - 실제 Guidewire 반지름에 맞춰서 Size( , )를 설정해 주어야 함.
                    imgLumen.Ellipse(new OpenCvSharp.Point(point.X, point.Y), new Size(50, 50), 0, 0, 360, Scalar.White, 1);
                }

                Mat binary = new Mat();
                Cv2.Threshold(imgLumen, binary, 128, 255, ThresholdTypes.Binary);

                Cv2.FindContours(binary, out contours, out HierarchyIndex[] hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                Cv2.DrawContours(imgLumen, contours, -1, Scalar.White, 1);

                Cv2.Resize(imgLumen, imgResize, imgResize.Size());

                Cv2.Blur(imgResize, imgResize, new Size(7, 7) /* 필터 크기 */, new Point(-1, -1) /* 필터 중심*/);
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

        private static void SetContour(JsonTextReader reader, string currentProperty, Contour lumenContour, CoRegistration coRegistration = null)
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
                case nameof(coRegistration.TrackPoints):
                    SetPoints(reader, coRegistration.TrackPoints);
                    break;
                case nameof(coRegistration.Line):
                    SetMultiDimensionalPoints(reader, coRegistration.Line);
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

        private static void SetMultiDimensionalPoints(JsonTextReader reader, List<List<System.Windows.Point>> multiDimensionalPoints)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                if (reader.Value != null)
                {
                    List<System.Windows.Point> innerPoints = new List<System.Windows.Point>();
                    SetPoints(reader, innerPoints);
                    multiDimensionalPoints.Add(innerPoints);
                }
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

        public static OpenCvSharp.Point[][] GetLumenContours(List<System.Windows.Point> pointList)
        {
            Mat img = new Mat(1024, 1024, MatType.CV_8UC1, new Scalar(0, 0, 0));

            OpenCvSharp.Point[] cvPoints = new OpenCvSharp.Point[pointList.Count];
            for (int i = 0; i < pointList.Count; i++)
            {
                cvPoints[i] = new OpenCvSharp.Point(Convert.ToInt32(Math.Round(pointList[i].X)), Convert.ToInt32(Math.Round(pointList[i].Y)));
            }

            if (cvPoints.Length > 0)
            {
                Cv2.DrawContours(img, new OpenCvSharp.Point[][] { cvPoints }, contourIdx: -1, color: new Scalar(255, 255, 255), thickness: 1);

                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(img, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);
                return contours;
            }
            else
            {
                return null;
            }
        }

        public static bool IsMalApposition(double appositionLength, double appositionThreshold)
        {
            if (appositionThreshold == -1)
                return false;

            if (appositionLength >= appositionThreshold)
                return true;
            else
                return false;
        }

        public static double GetAppositionLength(OpenCvSharp.Point[][] contours, OpenCvSharp.Point point)
        {
            OpenCvSharp.Point centerPoint = new OpenCvSharp.Point(512, 512);
            int x = centerPoint.X;
            int y = centerPoint.Y;
            int diffX = Math.Abs(centerPoint.X - point.X);
            int diffY = Math.Abs(centerPoint.Y - point.Y);

            if (point.X > centerPoint.X)
            {
                x = point.X + diffX;
            }
            else if (point.X < centerPoint.X)
            {
                x = point.X - diffX;
            }

            if (point.Y > centerPoint.Y)
            {
                y = point.Y + diffY;
            }
            else if (point.Y < centerPoint.Y)
            {
                y = point.Y - diffY;
            }

            OpenCvSharp.Point lineStart = centerPoint;
            OpenCvSharp.Point lineEnd = new OpenCvSharp.Point(x, y);

            foreach (var contour in contours)
            {
                for (int i = 0; i < contour.Length - 1; i++)
                {
                    OpenCvSharp.Point intersection;
                    if (LineIntersects(lineStart, lineEnd, contour[i], contour[i + 1], out intersection))
                    {
                        //_log.Debug($"교차점: {intersection}");
                        //// 교차점에 대한 추가 처리
                        //
                        //Cv2.Line(matLumenContour, lineStart, point, new Scalar(255, 255, 255), 1);
                        //Cv2.Line(matLumenContour, lineStart, lineEnd, new Scalar(255, 255, 255), 1);
                        //
                        //Cv2.Circle(matLumenContour, point, radius: 1, color: new Scalar(255, 255, 255), thickness: -1);
                        //
                        //Cv2.Circle(matLumenContour, intersection, radius: 1, color: new Scalar(0, 0, 0), thickness: -1);
                        //Cv2.ImShow("Test", matLumenContour);
                        //Cv2.WaitKey(0);

                        double distanceStent = Math.Sqrt(Math.Pow(centerPoint.X - point.X, 2) + Math.Pow(centerPoint.Y - point.Y, 2));
                        double distanceLumen = Math.Sqrt(Math.Pow(centerPoint.X - intersection.X, 2) + Math.Pow(centerPoint.Y - intersection.Y, 2));

                        return (distanceLumen - distanceStent) * Constants.ImageResolution;
                    }
                }
            }

            return 0;
        }

        // 선분 간의 교차점 계산 함수
        static bool LineIntersects(OpenCvSharp.Point p1, OpenCvSharp.Point p2, OpenCvSharp.Point p3, OpenCvSharp.Point p4, out OpenCvSharp.Point intersection)
        {
            intersection = new OpenCvSharp.Point();

            float denom = ((p4.Y - p3.Y) * (p2.X - p1.X)) - ((p4.X - p3.X) * (p2.Y - p1.Y));
            if (denom == 0) return false; // 평행 혹은 일치

            float num1 = ((p4.X - p3.X) * (p1.Y - p3.Y)) - ((p4.Y - p3.Y) * (p1.X - p3.X));
            float num2 = ((p2.X - p1.X) * (p1.Y - p3.Y)) - ((p2.Y - p1.Y) * (p1.X - p3.X));

            float r = num1 / denom;
            float s = num2 / denom;

            if (r < 0 || r > 1 || s < 0 || s > 1) return false; // 선분이 교차하지 않음

            // 교차점 계산
            intersection = new OpenCvSharp.Point(p1.X + (r * (p2.X - p1.X)), p1.Y + (r * (p2.Y - p1.Y)));
            return true;
        }

        public static bool GetStentProximalDistal(List<LumenStent> lumenStents, out int proximal, out int distal)
        {
            proximal = 0;
            distal = 0;

            int[] numbers = new int[lumenStents.Count];

            bool isValid = false;

            for (int i = 0; i < lumenStents.Count; i++)
            {
                lumenStents[i].IsStent = false;

                if (lumenStents[i].Points == null)
                {
                    lumenStents[i].Points = new List<System.Windows.Point>();
                    lumenStents[i].AppositionLength = new List<double>();
                    numbers[i] = 0;
                }
                else
                {
                    numbers[i] = lumenStents[i].Points.Count;

                    if (lumenStents[i].Points.Count > 0 && !isValid)
                        isValid = true;
                }
            }

            if (!isValid)
                return false;

            List<(List<int> sequence, int startIndex, int endIndex)> sequences = new List<(List<int> sequence, int startIndex, int endIndex)>();
            List<int> currentSequence = new List<int>();
            int startIndex = -1;

            // 연속된 0 이상의 숫자 그룹 찾기
            for (int i = 0; i < numbers.Length; i++)
            {
                int number = numbers[i];

                if (number != 0)
                {
                    if (startIndex == -1)
                    {
                        startIndex = i;
                    }
                    currentSequence.Add(number);
                }
                else
                {
                    if (currentSequence.Count > 0)
                    {
                        sequences.Add((new List<int>(currentSequence), startIndex, i - 1));
                        currentSequence.Clear();
                        startIndex = -1;
                    }
                }
            }

            // 마지막 시퀀스를 추가
            if (currentSequence.Count > 0)
            {
                sequences.Add((currentSequence, startIndex, numbers.Length - 1));
            }

            // 그룹 간의 간격이 3 이하면 합치기
            List<(List<int> sequence, int startIndex, int endIndex)> mergedSequences = new List<(List<int> sequence, int startIndex, int endIndex)>();

            if (sequences.Count > 0)
            {
                var currentMergedSequence = sequences[0].sequence;
                int currentMergedStartIndex = sequences[0].startIndex;
                int currentMergedEndIndex = sequences[0].endIndex;

                for (int i = 1; i < sequences.Count; i++)
                {
                    var (nextSequence, nextStartIndex, nextEndIndex) = sequences[i];

                    if (nextStartIndex - currentMergedEndIndex <= 3)
                    {
                        currentMergedSequence.AddRange(nextSequence);
                        currentMergedEndIndex = nextEndIndex;
                    }
                    else
                    {
                        mergedSequences.Add((new List<int>(currentMergedSequence), currentMergedStartIndex, currentMergedEndIndex));
                        currentMergedSequence = nextSequence;
                        currentMergedStartIndex = nextStartIndex;
                        currentMergedEndIndex = nextEndIndex;
                    }
                }

                // 마지막 시퀀스를 추가
                mergedSequences.Add((new List<int>(currentMergedSequence), currentMergedStartIndex, currentMergedEndIndex));
            }

            // 가장 큰 그룹 찾기
            var largestSequence = mergedSequences[0];
            foreach (var sequenceInfo in mergedSequences)
            {
                if (sequenceInfo.sequence.Count > largestSequence.sequence.Count)
                {
                    largestSequence = sequenceInfo;
                }
            }

            proximal = largestSequence.startIndex;
            distal = largestSequence.endIndex;

            for (int i = proximal; i <= distal; i++)
            {
                lumenStents[i].IsStent = true;
            }

            return true;
        }

        public static BitmapSource DrawSheathIndicator(int imageSize, double sheathDiameter)
        {
            double pxDiameter = (sheathDiameter / Constants.ImageResolution) * imageSize / Constants.OCTImageSize;
            Mat imgSheath = new Mat(imageSize, imageSize, MatType.CV_8UC4);
            Point center = new Point(imgSheath.Width / 2, imgSheath.Height / 2);
            int thickness = 1;
            int radius = (int)(pxDiameter / 2) + thickness;

            imgSheath.SetTo(new Scalar(0x00, 0x00, 0x00, 0x00));
            imgSheath.Circle(center, radius, new Scalar(0x60, 0xd7, 0x1e, 0xff), thickness, LineTypes.AntiAlias);

            for (int i = 1; i < 6; i += 2)
            {
                imgSheath.Ellipse(center,
                    new OpenCvSharp.Size(imgSheath.Width / 2, imgSheath.Height / 2),
                    0,
                    i * 60,
                    i * 60 + 60,
                    new Scalar(0x00, 0x00, 0x00, 0x00),
                    -1);
            }

            BitmapSource bitmap = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgSheath);
            return bitmap;
        }

        public static void SetColormap(string? colorCode)
        {
            if (colorCode == null)
                return;

            if ("GRGR".Equals(colorCode))
            {
                RaySetProperty(Property.Colormap, 0);
            }
            else if ("GRAY".Equals(colorCode))
            {
                RaySetProperty(Property.Colormap, 1);
            }
            else if ("ORNG".Equals(colorCode))
            {
                RaySetProperty(Property.Colormap, 2);
            }
        }

        public static bool IsTestMode(Dictionary<string, bool> testMode, string key)
        {
            if (!testMode.ContainsKey(key))
                return false;

            return testMode[key];
        }

        public static void ReadAngioParams(PatientCase patientCase)
        {
            string file = patientCase.Image;
            string paramsFile = file.Substring(0, file.Length - 3) + "params";

            string directory = Path.Combine(Constants.DataRootPath, patientCase.PatientId);
            string paramsPath = Path.Combine(directory, paramsFile);

            //Read .params
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(paramsPath);

            XmlNode configNode = xmlDoc.SelectSingleNode("/config");
            if (patientCase.AngioFrame == null) patientCase.AngioFrame = new AngioFrame();
            patientCase.AngioFrame.AngioFrameHeight = int.Parse(configNode.SelectSingleNode("AngioFrameHeight").InnerText);
            patientCase.AngioFrame.AngioFrameWidth = int.Parse(configNode.SelectSingleNode("AngioFrameWidth").InnerText);
            patientCase.AngioFrame.Channels = int.Parse(configNode.SelectSingleNode("BitsPerPixel").InnerText) / 8;
        }

        public static void ReadAngioImages(PatientCase patientCase, List<Mat>? angioFrames = null)
        {
            string file = patientCase.Image;
            string angioFile = file.Substring(0, file.Length - 3) + "angioframes";

            string directory = Path.Combine(Constants.DataRootPath, patientCase.PatientId);
            string angioPath = Path.Combine(directory, angioFile);

            int angioHeight = patientCase.AngioFrame.AngioFrameHeight;
            int angioWidth = patientCase.AngioFrame.AngioFrameWidth;
            int angioChannels = patientCase.AngioFrame.Channels;

            using (BinaryReader reader = new BinaryReader(System.IO.File.Open(angioPath, FileMode.Open)))
            {
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    byte[] data = reader.ReadBytes(angioWidth * angioHeight * angioChannels);
                    Mat frame = new Mat(angioHeight, angioWidth, MatType.CV_8UC(angioChannels), data);
                    switch (angioChannels)
                    {
                        case 3:
                            Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2GRAY);
                            break;

                        case 4:
                            Cv2.CvtColor(frame, frame, ColorConversionCodes.RGBA2GRAY);
                            break;
                    }
                    if (angioFrames != null) angioFrames.Add(frame);
                    patientCase.AngioFrame.AngioImage.Add(ConvertMatsToImageSource(frame));
                }
            }
        }

        public static ImageSource ConvertMatsToImageSource(Mat mat)
        {
            using (var stream = new MemoryStream())
            {
                mat.WriteToStream(stream, "." + Constants.ExportStillFrameBitmap);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        public static Mat ApplyNiblackThreshold(Mat img, int windowSize, double k)
        {
            Mat mean = new Mat();
            Mat stddev = new Mat();
            Mat thresholdImg = new Mat(img.Size(), MatType.CV_8UC1);

            // 평균 및 표준 편차 계산
            Cv2.Blur(img, mean, new OpenCvSharp.Size(windowSize, windowSize));
            Mat sqrMean = new Mat();
            Cv2.SqrBoxFilter(img, sqrMean, MatType.CV_32F, new OpenCvSharp.Size(windowSize, windowSize));
            Cv2.Sqrt(sqrMean, stddev);

            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    double threshold = mean.At<byte>(y, x) + k * stddev.At<byte>(y, x);
                    thresholdImg.Set(y, x, img.At<byte>(y, x) > threshold ? (byte)255 : (byte)0);
                }
            }

            return thresholdImg;
        }

        public static Mat ApplyLocalOtsuThreshold(Mat img, int windowSize)
        {
            Mat thresholdImg = new Mat(img.Size(), MatType.CV_8UC1);

            for (int y = 0; y < img.Rows; y += windowSize)
            {
                for (int x = 0; x < img.Cols; x += windowSize)
                {
                    // 로컬 영역 설정
                    OpenCvSharp.Rect rect = new OpenCvSharp.Rect(x, y, Math.Min(windowSize, img.Cols - x), Math.Min(windowSize, img.Rows - y));
                    Mat localRegion = new Mat(img, rect);

                    // Otsu Thresholding 적용
                    Mat localThreshold = new Mat();
                    Cv2.Threshold(localRegion, localThreshold, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

                    // 결과를 전체 이미지에 반영
                    localThreshold.CopyTo(new Mat(thresholdImg, rect));
                }
            }

            return thresholdImg;
        }

        public static Mat ApplyPhansalkarThreshold(Mat img, int windowSize = 9)
        {
            double k = 0.05;
            // K값 커지면 임계값 증가 Default = 0.25
            double r = 0.5;
            // r값 커지면 임계값 감소 Default = 0.5

            double p = 2.0;
            //고정
            double q = 10.0;
            //고정

            Mat convertedImg = new Mat();
            Mat mean = new Mat();
            Mat XminusM = new Mat();
            Mat XminusMSquared = new Mat();
            Mat variance = new Mat();
            Mat thresholdImg = new Mat(img.Size(), MatType.CV_8UC1);

            img.CopyTo(convertedImg);
            convertedImg.ConvertTo(convertedImg, MatType.CV_32F);
            // 평균 계산
            Cv2.Blur(convertedImg, mean, new OpenCvSharp.Size(windowSize, windowSize));

            // (표본 - 평균)
            Cv2.Subtract(convertedImg, mean, XminusM);

            // (표본 - 평균)^2
            Cv2.Pow(XminusM, 2, XminusMSquared);

            // 분산 계산
            Cv2.Blur(XminusMSquared, variance, new OpenCvSharp.Size(windowSize, windowSize));

            // 표준 편차 계산
            Mat stdDev = new Mat();
            Cv2.Sqrt(variance, stdDev);

            // stdDev의 최대 값 찾기
            double minVal, maxVal;
            Cv2.MinMaxLoc(stdDev, out minVal, out maxVal);
            r = maxVal; // r 값을 stdDev의 최대 값으로 설정

            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    float meanValue = mean.At<float>(y, x);
                    float stdDevValue = stdDev.At<float>(y, x);

                    // Phansalkar 임계값 계산
                    //double threshold = meanValue * (1.0 + k * ((stdDevValue / r) - 1.0));
                    double threshold = meanValue * (1.0 + Math.Exp(-q * meanValue) + k * ((stdDevValue / r) - 1.0));

                    // 이진화 적용
                    thresholdImg.Set(y, x, img.At<byte>(y, x) >= threshold ? (byte)255 : (byte)0);
                }
            }
            return thresholdImg;
        }

        public static Mat ApplyMidGreyThreshold(Mat img, int windowSize)
        {
            int halfWindowSize = windowSize / 2;
            Mat thresholdImg = new Mat(img.Size(), MatType.CV_8UC1);

            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    // 지역 창의 범위 설정
                    int xStart = Math.Max(0, x - halfWindowSize);
                    int xEnd = Math.Min(img.Cols - 1, x + halfWindowSize);
                    int yStart = Math.Max(0, y - halfWindowSize);
                    int yEnd = Math.Min(img.Rows - 1, y + halfWindowSize);

                    byte minVal = byte.MaxValue;
                    byte maxVal = byte.MinValue;

                    // 지역 창 내의 최소값과 최대값 계산
                    for (int j = yStart; j <= yEnd; j++)
                    {
                        for (int i = xStart; i <= xEnd; i++)
                        {
                            byte pixelVal = img.At<byte>(j, i);
                            if (pixelVal < minVal)
                            {
                                minVal = pixelVal;
                            }
                            if (pixelVal > maxVal)
                            {
                                maxVal = pixelVal;
                            }
                        }
                    }

                    // Mid Grey 임계값 계산
                    byte threshold = (byte)((minVal + maxVal) / 2);

                    // 이진화 적용
                    thresholdImg.Set(y, x, img.At<byte>(y, x) > threshold ? (byte)255 : (byte)0);
                }
            }

            return thresholdImg;
        }

        public static Mat ApplyMedianThreshold(Mat img, int windowSize)
        {
            Mat medianImg = new Mat();
            Cv2.MedianBlur(img, medianImg, windowSize);

            // Median Thresholding 적용
            Mat thresholdImg = new Mat();
            Cv2.Threshold(img, thresholdImg, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // Median 값을 임계값으로 사용하여 이진화
            thresholdImg = new Mat(img.Size(), MatType.CV_8UC1);
            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    thresholdImg.Set(y, x, img.At<byte>(y, x) > medianImg.At<byte>(y, x) ? (byte)255 : (byte)0);
                }
            }

            return thresholdImg;
        }

        public static double GetRoundScale(double value)
        {
            return Math.Round(value, 5);
        }

        public static void GetStorageSize(out double totalSize, out double freeSize)
        {
            string configDrive = Constants.SystemRootPath + "\\";
            totalSize = 0;
            freeSize = 0;

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.Name.Equals(configDrive))
                {
                    totalSize = CommonUtil.ByteToGB(drive.TotalSize);
                    freeSize = CommonUtil.ByteToGB(drive.AvailableFreeSpace);
                    break;
                }
            }
        }

        public static bool IsStorageAvailable()
        {
            double storageTotalSize, storageFreeSize;
            GetStorageSize(out storageTotalSize, out storageFreeSize);

            if (storageFreeSize < Constants.StorageLimit)
                return false;

            return true;
        }
    }
}
