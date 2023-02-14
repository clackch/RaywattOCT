using System.Runtime.InteropServices;

namespace RayCoreWrapper
{
    public class RayExportWrapper
    {
        public enum DicomRWError : int
        {
            Normal = 0,
            OutPlugNull = 1,
            I2dNull = 2,
            FailInsertPixelData_DifferentColsOrRows = 3,
            DatasetNull = 4,
            SequenceNull = 5,
            SaveFileFail = 6,
            FailInsertFirstPixelData = 7,
            MetaInfoNull = 8,
            DicomImageNotExist = 9,
            DcmItemNull = 10,
            PixelDataNotExist = 11,
            LoadFileFail = 12,
            FailImageFinish = 13,
            FailAddProperty = 14,
        }

        [DllImport("makedcmDLL.dll")]
        public static extern int DicomStart();
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomImageStart(int numberOfFrames);
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomAddImage(int width, int height, IntPtr pixelData);
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomImageFinish();
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomStartProperty();
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomAddProperty(int tag, char[] value, int bufLen = 0);
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomStartSequenceProperty(int numberOfItems);
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomAddSequenceProperty(int itemnum, int tag, char[] value);
        [DllImport("makedcmDLL.dll")]
        public static extern int DicomSave(string path);
    }
}
