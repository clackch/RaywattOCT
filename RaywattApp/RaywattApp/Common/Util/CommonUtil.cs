using OpenCvSharp;
using System.Runtime.InteropServices;
using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using log4net;

namespace RaywattApp.Common.Util
{
    public class CommonUtil
    {
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
            int byteLength = width * height * ch;
            byte[] imgData = new byte[byteLength];
            Marshal.Copy(data, imgData, 0, byteLength);

            return new Mat(height, width, MatType.CV_8UC3, data);
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
    }
}
