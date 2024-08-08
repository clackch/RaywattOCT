using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace RayCoreWrapper
{
    public class RayExportWrapper
    {
        //DICOM
        public enum DicomRWError : int
        {
            Normal = 0,
            OutPlugNull = -1,
            I2dNull = -2,
            FailInsertPixelData_DifferentColsOrRows = -3,
            DatasetNull = -4,
            SequenceNull = -5,
            SaveFileFail = -6,
            FailInsertFirstPixelData = -7,
            MetaInfoNull = -8,
            DicomImageNotExist = -9,
            DcmItemNull = -10,
            PixelDataNotExist = -11,
            LoadFileFail = -12,
            FailImageFinish = -13,
            FailAddProperty = -14,
            FailDeallocMemory = -15
        }

        public enum DICOMDIRRWError : int
        {
            Normal = 0,
            DataDictionaryNotLoaded = -1,
            DirectoryNotExist = -2,
            DICOMDIRNotCreated = -3,
            OtherError = -4,
            CannotAddDICOMDIR = -5,
            NoInputFiles = -6,
            DICOMDIRLoadFail = -7,
            DICOMDIRRecodeLoadFail = -8
        }

        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomStart();
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomImageStart(int numberOfFrames);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomAddImage(int width, int height, IntPtr pixelData);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomImageFinish();
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomStartProperty();
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomAddProperty(int tag, string value, int bufLen = 0);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomStartSequenceProperty(int numberOfItems);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomAddSequenceProperty(int itemnum, int tag, string value);
        [DllImport("makedcmDLL.dll")]
        public static extern DicomRWError DicomSave(string path);
        [DllImport("makedcmDLL.dll")]
        public static extern DICOMDIRRWError DICOMDIRInputFolder(string path);
        [DllImport("makedcmDLL.dll")]
        public static extern DICOMDIRRWError DICOMDIRInputFile(string path);
        [DllImport("makedcmDLL.dll")]
        public static extern DICOMDIRRWError DICOMDIRWrite();
        [DllImport("makedcmDLL.dll")]
        public static extern long DicomApprSize();

        [DllImport("HessianMatrixDll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void useFrangi2d(IntPtr imageData, out IntPtr outputData, int width, int height, int channels, out int outwidth, out int outheight, out int outchannels);
    }
}
