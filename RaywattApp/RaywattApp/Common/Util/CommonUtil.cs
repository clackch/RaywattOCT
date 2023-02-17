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
            MatType type = ch == 3 ? MatType.CV_8UC3 : MatType.CV_8UC1;
            return new Mat(height, width, type, data).Clone();
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

        public static bool Encryptor(string filePath, string contents)
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
                                encryptWriter.WriteLine(contents);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _log.Error($"The encryption failed. {ex}");
                return false;
            }
        }

        public static string[] Decryptor(string filePath)
        {
            string[] result = new string[2];

            try
            {
                string contents = "";

                using (FileStream fileStream = new(filePath, FileMode.Open))
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
                result[0] = "1";
                result[1] = contents;
                return result;
            }
            catch (Exception ex)
            {
                _log.Error($"The decryption failed. {ex}");
                result[0] = "0";
                result[1] = ex.ToString();
                return result;
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
            Directory.Move(oldPath, newPath);
        }

        public static void DeleteFolder(string path)
        {
            Directory.Delete(path, true);
        }

        public static async Task CopyFiles(Dictionary<string, string> files, Action<double> progressCallback)
        {
            long total_size = files.Keys.Select(x => new FileInfo(x).Length).Sum();

            long total_read = 0;

            double progress_size = 100.0;

            foreach (var item in files)
            {
                long total_read_for_file = 0;

                var from = item.Key;
                var to = item.Value;

                using (var outStream = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    using (var inStream = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await CopyStream(inStream, outStream, x =>
                        {
                            total_read_for_file = x;
                            progressCallback(((total_read + total_read_for_file) / (double)total_size) * progress_size);
                        });
                    }
                }

                total_read += total_read_for_file;
            }
        }

        public static async Task<Mat> ConvertImage(string filePath, List<int>? bookmarkedIndices, List<Mat> convertedImages, Action<double> progressCallback, double progress)
        {
            RayOpenImage(filePath);

            int numOfFrames = (int)RayGetProperty(Property.ImageDepth);
            int width = (int)RayGetProperty(Property.ImageWidth);
            int height = (int)RayGetProperty(Property.ImageHeight);
            int channels = (int)RayGetProperty(Property.ImageChannels);

            int totalNum = (bookmarkedIndices == null) ? numOfFrames : bookmarkedIndices.Count;

            // convert all frames
            for (int index = 0; index < numOfFrames; index++)
            {
                await Task.Run(() => {
                    IntPtr data = RayGetImageData(index);
                    if (convertedImages != null)
                    {
                        if (bookmarkedIndices == null || bookmarkedIndices.Contains(index))
                        {
                            Mat img = CommonUtil.ByteMemoryToCvMat(data, width, height, channels);
                            convertedImages.Add(img);

                            progressCallback(progress / totalNum);
                        }
                    }
                });
            }

            // get longitude from core
            width = (int)RayGetProperty(Property.LongitudeImageWidth);
            height = (int)RayGetProperty(Property.LongitudeImageHeight);
            channels = (int)RayGetProperty(Property.LongitudeImageChannels);
            IntPtr data = RayGetLongitudeData(45);
            Mat imgLongitude = CommonUtil.ByteMemoryToCvMat(data, width, height, channels);

            RayCloseImage();

            return imgLongitude;
        }

        public static Mat MakeImageForExport(Mat crossSection, Mat? longitude, Mat? lumenProfile) {
            Mat imgExport = new Mat();
            imgExport.Create(Constants.ApplicationHeight, Constants.ApplicationWidth, MatType.CV_8UC3);
            imgExport.SetTo(0x00);

            if (crossSection == null) return imgExport;

            Size szRemain = new Size(imgExport.Width, imgExport.Height);
            Size szLongitudeInfo = new Size(imgExport.Width, 30);
            Size szLongitude = new Size(imgExport.Width, (imgExport.Height / 2 - szLongitudeInfo.Height) / 2);
            if (longitude != null)
            {
                Mat imgLongitude = new Mat();
                Cv2.Resize(longitude, imgLongitude, szLongitude);
                Cv2.CopyTo(imgLongitude, imgExport[new Rect(0, szRemain.Height - szLongitude.Height, szLongitude.Width, szLongitude.Height)]);
                szRemain.Height -= szLongitude.Height;

                // draw longitude info
                szRemain.Height -= szLongitudeInfo.Height;
            }
            if (lumenProfile != null)
            {
                Mat imgLumenProfile = new Mat();
                Cv2.Resize(lumenProfile, imgLumenProfile, szLongitude);
                Cv2.CopyTo(imgLumenProfile, imgExport[new Rect(0, szRemain.Height - szLongitude.Height, szLongitude.Width, szLongitude.Height)]);

                szRemain.Height -= szLongitude.Height;
            }

            int diameter = Math.Min(szRemain.Width, szRemain.Height);

            Mat imgCrossSection = new Mat();
            Cv2.Resize(crossSection, imgCrossSection, new Size(diameter, diameter));
            Cv2.CopyTo(imgCrossSection, imgExport[new Rect((szRemain.Width - diameter) / 2, 0, diameter, diameter)]);

            return imgExport;
        }

        public static void SaveStillFrame(Mat image, string rootPath, string fileName, string format) 
        {
            string filePath = rootPath + "\\" + fileName + "." + format.ToLower();

            Cv2.ImWrite(filePath, image);
        }

        public static async Task SaveVideo(List<Mat> images, string rootPath, string fileName, string format, double fps, Action<double> progressCallback, double progress)
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
                    });
                }
                videoWriter.Release();
            }
        }

        public static async Task SaveMultipleFrames(List<Mat> images, string rootPath, string fileName, string format, Action<double> progressCallback, double progress)
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
                        });
                    }

                    tiff.Close();
                }
            }
        }

        public static async Task CopyStream(Stream from, Stream to, Action<long> progress)
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

        public static double ByteToGB(double bytes)
        {
            double div = 1024.0;
            return Math.Round(bytes / div / div / div, 3);
        }
    }
}
