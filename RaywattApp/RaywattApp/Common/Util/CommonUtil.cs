using OpenCvSharp;
using System.Runtime.InteropServices;
using System;
using System.Text.RegularExpressions;

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

        public static Mat byteMemoryToCvMat(IntPtr data, int width, int height, int ch)
        {
            int byteLength = width * height * ch;
            byte[] imgData = new byte[byteLength];
            Marshal.Copy(data, imgData, 0, byteLength);

            return new Mat(height, width, MatType.CV_8UC3, data);
        }
    }
}
