using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RayCoreWrapper;
using OpenCvSharp;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileCopyDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileCopyDialogViewModel));

        [ObservableProperty]
        private FileExport? _fileExport;

        [ObservableProperty]
        private IList<PatientCase>? _patientCases;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string? _dbFilePath;

        [ObservableProperty]
        private string? _contents;

        [ObservableProperty]
        private string? _saveFolder;

        [ObservableProperty]
        private bool enableDone = false;

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();
            FileExport = (FileExport)data["fileExport"];
            PatientCases = (IList<PatientCase>)data["patientCases"];

            if (FileExport != null)
            {
                SaveFolder = (FileExport.DiskType == Constants.FileDiskCd) ? Constants.TempFolderPath : FileExport.ExternalDrivePath;

                if (FileExport.Type == Constants.ExportTypeNative)
                {
                    DbFilePath = data["dbFilePath"].ToString();
                    Contents = data["contents"].ToString();

                    FileCopyNative();
                }
                else if (FileExport.Type == Constants.ExportTypeDicom)
                {
                    FileSaveDicom();
                }
                else if (FileExport.Type == Constants.ExportTypeStandard)
                {
                    FileSaveStandard();
                }

                if (FileExport.DiskType == Constants.FileDiskCd)
                {
                    // RayExportWrapper.BurningCD();
                }
            }
        }

        private async void FileCopyNative()
        {
            Dictionary<string, string> fileCopyInfo = new Dictionary<string, string>();
            foreach (PatientCase patientCase in PatientCases)
            {
                string fileName = CommonUtil.GetFileName(patientCase.ImageFullPath);
                string dstFilePath = SaveFolder + "\\" + fileName;
                fileCopyInfo.Add(patientCase.ImageFullPath, dstFilePath);
            }

            await CommonUtil.CopyFiles(fileCopyInfo, prog => Progress = prog);

            if (!String.IsNullOrEmpty(DbFilePath) && !String.IsNullOrEmpty(Contents))
                CommonUtil.Encryptor(DbFilePath, Contents);

            EnableDone = true;
        }

        private async void FileSaveDicom()
        {
            //DICOMDIR Input Folder
            string dicomDirFolder = CommonUtil.CreateFolder(SaveFolder + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            RayExportWrapper.DICOMDIRInputFolder(dicomDirFolder);

            //Group by Patient
            var patientGrp = from arr in PatientCases
                             orderby arr.CreateDate descending
                             group arr by arr.PatientId into g
                             select g;

            int index = 0, studyId, seriesNumber;
            double progressConvert = 100 / PatientCases.Count;

            foreach (var patient in patientGrp)
            {
                studyId = 1;

                //Group by CreateDate
                var dateGrp = from arr in patient
                              orderby arr.CreateDate descending
                              group arr by arr.CreateDate.ToString("yyyyMMdd") into g
                              select g;

                foreach (var date in dateGrp)
                {
                    seriesNumber = 1;

                    foreach (var patientCase in date)
                    {
                        _log.Debug(patientCase.PatientId + " " + patientCase.Id + " " + patientCase.CreateDate);

                        List<int> exportIndices = null;
                        if (FileExport.Material == Constants.ExportMaterialCurrent)
                        {
                            exportIndices = new List<int>();
                            exportIndices.Add(FileExport.CurrentFrame);
                        }
                        else if (FileExport.Material == Constants.ExportMaterialBookmarked)
                        {
                            exportIndices = FileExport.BookmarkedFrames;
                        }

                        List<Mat> convertedImages = new List<Mat>();
                        Mat imgLongitude = await CommonUtil.ConvertImage(patientCase.ImageFullPath, exportIndices, convertedImages, prog => Progress += prog, progressConvert);

                        //Start
                        RayExportWrapper.DicomStart();

                        //Image
                        RayExportWrapper.DicomImageStart(convertedImages.Count);
                        for (int frame = 0; frame < convertedImages.Count; frame++)
                        {
                            Mat imgExport = CommonUtil.MakeImageForExport(convertedImages[frame], imgLongitude, imgLongitude);
                            Cv2.CvtColor(imgExport, imgExport, ColorConversionCodes.RGB2BGR);
                            RayExportWrapper.DicomAddImage(imgExport.Cols, imgExport.Rows, imgExport.Data);
                        }
                        RayExportWrapper.DicomImageFinish();

                        //Property
                        SetProperty(patientCase, studyId, seriesNumber, FileExport.Material != Constants.ExportMaterialCurrent ? 0 : FileExport.CurrentFrame);

                        //Sequence Property
                        SetSequenceProperty();

                        //Save
                        string filePath = Constants.ExportDicomPrefix + string.Format("{0:0000}", index);
                        RayExportWrapper.DicomSave(dicomDirFolder + "\\" + filePath);

                        //DICOMDIR Input File
                        RayExportWrapper.DICOMDIRInputFile(filePath);

                        index++;
                        seriesNumber++;
                    }
                    studyId++;
                }
            }

            //DICOMDIR Write
            RayExportWrapper.DICOMDIRWrite();

            Progress = 100;
            EnableDone = true;
        }

        private async void FileSaveStandard()
        {
            string format = (FileExport.Material == Constants.ExportMaterialPullback) ? FileExport.Pullback : FileExport.StillFrame;

            for (int i = 0; i < PatientCases.Count; i++)
            {
                double progressPerCase = 100 / PatientCases.Count;
                double progressConvert = progressPerCase / 2;
                double progressSave = progressPerCase / 2;

                List<int> exportIndices = null;
                if (FileExport.Material == Constants.ExportMaterialCurrent)
                {
                    exportIndices = new List<int>();
                    exportIndices.Add(FileExport.CurrentFrame);
                }
                else if (FileExport.Material == Constants.ExportMaterialBookmarked)
                {
                    exportIndices = FileExport.BookmarkedFrames;
                }

                List<Mat> convertedImages = new List<Mat>();
                Mat imgLongitude = await CommonUtil.ConvertImage(PatientCases[i].ImageFullPath, exportIndices, convertedImages, prog => Progress += prog, progressConvert);
                for (int frame = 0; frame < convertedImages.Count; frame++)
                {
                    convertedImages[frame] = CommonUtil.MakeImageForExport(convertedImages[frame], imgLongitude, imgLongitude);
                }

                if (format == Constants.ExportPullbackAVI)
                {
                    string fileName = CreateStandardUniqueName(PatientCases[i]);
                    await CommonUtil.SaveVideo(convertedImages, SaveFolder, fileName, format, 10, prog => Progress += prog, progressSave);
                }
                else if (format == Constants.ExportPullbackTIFF)
                {
                    string fileName = CreateStandardUniqueName(PatientCases[i]);
                    await CommonUtil.SaveMultipleFrames(convertedImages, SaveFolder, fileName, format, prog => Progress += prog, progressSave);
                }
                else
                {
                    for (int frame = 0; frame < convertedImages.Count; frame++)
                    {
                        string fileName = CreateStandardUniqueName(PatientCases[i]) + string.Format("-{0:0000}", exportIndices[frame]);
                        CommonUtil.SaveStillFrame(convertedImages[frame], SaveFolder, fileName, format);
                        Progress += (progressSave / convertedImages.Count);
                    }
                }
            }

            Progress = 100;
            EnableDone = true;
        }

        private void SetProperty(PatientCase patientCase, int studyId, int seriesNumber, int instanceNumber)
        {
            RayExportWrapper.DicomStartProperty();

            //(0002, 0001)	File Meta Information Version	-	M	OB
            //Auto Assigned
            //(0002, 0002)	Media Storage SOP Class UID	-	M	UI
            //Auto Assigned
            //(0002, 0003)	Media Storage SOP Instance UID	-	M	UI
            //Auto Assigned
            //(0002, 0010)	Transfer Syntax UID	-	M	UI
            //Auto Assigned
            //(0002, 0012)	Implementation Class UID	-	M	UI
            //Auto Assigned
            //(0002, 0013)	Implementation Version Name	-	C	SH
            RayExportWrapper.DicomAddProperty(0x00020013, "Raywatt Version Name", 0);
            //(0002, 0016)	Source Application Entity Title	-	M	AE
            RayExportWrapper.DicomAddProperty(0x00020016, "Raywatt Title", 0);
            //(0008, 0005)	Specific Character Set	-	C	CS
            //(0008, 0008)	Image Type	-	M	CS
            //(0008, 0012)	Instance Creation Date	-	M	DA
            //(0008, 0013)	Instance Creation Time	-	M	TM
            //(0008, 0016)	SOP Class UID	-	M	UI
            //Auto Assigned (1.2.840.10008.5.1.4.1.1.7.4)
            //(0008, 0018)	SOP Instance UID	-	M	UI
            //Auto Assigned
            //(0008, 0020)	Study Date	-	M	DA
            RayExportWrapper.DicomAddProperty(0x00080020, patientCase.CreateDate.ToString("yyyyMMdd"), 0);
            //(0008, 0021)	Series Date	-	M, C, U	DA
            RayExportWrapper.DicomAddProperty(0x00080021, patientCase.CreateDate.ToString("yyyyMMdd"), 0);
            //(0008, 0022)	Acquisition Date	-	M	DA
            RayExportWrapper.DicomAddProperty(0x00080022, patientCase.CreateDate.ToString("yyyyMMdd"), 0);
            //(0008, 0023)	Content Date	-	C	DA
            RayExportWrapper.DicomAddProperty(0x00080023, patientCase.CreateDate.ToString("yyyyMMdd"), 0);
            //(0008, 0030)	Study Time	-	M	TM
            RayExportWrapper.DicomAddProperty(0x00080030, patientCase.CreateDate.ToString("HHmmss"), 0);
            //(0008, 0031)	Series Time	-	M, C, U	TM
            RayExportWrapper.DicomAddProperty(0x00080031, patientCase.CreateDate.ToString("HHmmss"), 0);
            //(0008, 0032)	Acquisition Time	-	M	TM
            RayExportWrapper.DicomAddProperty(0x00080032, patientCase.CreateDate.ToString("HHmmss"), 0);
            //(0008, 0033)	Content Time	-	C	TM
            RayExportWrapper.DicomAddProperty(0x00080033, patientCase.CreateDate.ToString("HHmmss"), 0);
            //(0008, 0050)	Accession Number	-	M	SH
            RayExportWrapper.DicomAddProperty(0x00080050, patientCase.AccessionNumber, 0);
            //(0008, 0060)	Modality	-	M	CS
            RayExportWrapper.DicomAddProperty(0x00080060, "OCT", 0);
            //(0008, 0064)	Conversion Type	-	U	CS
            RayExportWrapper.DicomAddProperty(0x00080064, "SI", 0);
            //(0008, 0070)	Manufacturer	-	M, C, U	LO
            RayExportWrapper.DicomAddProperty(0x00080070, "Raywatt Manufacturer", 0);
            //(0008, 0080)	Institution Name	-	M	LO
            RayExportWrapper.DicomAddProperty(0x00080080, "XXX Hospital", 0);
            //(0008, 0090)	Referring Physician's Name	-	C	PN
            RayExportWrapper.DicomAddProperty(0x00080090, patientCase.PhysicianName, 0);
            //(0008, 0201)	Timezone Offset From UTC	-	C	SH
            RayExportWrapper.DicomAddProperty(0x00080201, GetTimeOffset(), 0);
            //(0008, 1050)	Performing Physician's Name	-	C	PN
            RayExportWrapper.DicomAddProperty(0x00081050, patientCase.PhysicianName, 0);
            //(0008, 1090)	Manufacturer's Model Name	-	M	LO
            RayExportWrapper.DicomAddProperty(0x00081090, "Raywatt Model Name", 0);
            //(0008, 2144)	Recommended Display Frame Rate	-	U	IS
            //(0010, 0010)	Patient's Name	-	M	PN
            RayExportWrapper.DicomAddProperty(0x00100010, patientCase.PatientName, 0);
            //(0010, 0020)	Patient ID	-	M	LO
            RayExportWrapper.DicomAddProperty(0x00100020, patientCase.PatientId, 0);
            //(0010, 0030)	Patient's Birth Date	-	M	DA
            RayExportWrapper.DicomAddProperty(0x00100030, patientCase.Birthdate.ToString("yyyyMMdd"), 0);
            //(0010, 0032)	Patient's Birth Time	-	M	TM
            RayExportWrapper.DicomAddProperty(0x00100032, patientCase.Birthdate.ToString("HHmmss"), 0);
            //(0010, 0040)	Patient's Sex	-	M	CS
            RayExportWrapper.DicomAddProperty(0x00100040, patientCase.Gender, 0);
            //(0010, 1010)	Patient's Age	-	M	AS
            RayExportWrapper.DicomAddProperty(0x00101010, GetAge(patientCase.Birthdate, patientCase.CreateDate), 0);
            //(0010, 4000)	Patient Comments	-	M	LT
            RayExportWrapper.DicomAddProperty(0x00104000, patientCase.Comment, 0);
            //(0018, 0015)	Body Part Examined	-	M	CS
            RayExportWrapper.DicomAddProperty(0x00180015, "HEART", 0);
            //(0018, 1016)	Secondary Capture Device Manufacturer	-	U	LO
            RayExportWrapper.DicomAddProperty(0x00181016, "Raywatt SC Device Manufacturer", 0);
            //(0018, 1018)	Secondary Capture Device Manufacturer's Model Name	-	U	LO
            RayExportWrapper.DicomAddProperty(0x00181018, "Raywatt SC Device Manufacturer Model Name", 0);
            //(0018, 1019)	Secondary Capture Device Software Versions	-	U	LO
            RayExportWrapper.DicomAddProperty(0x00181019, "Raywatt SC Device SW Version", 0);
            //(0018, 1020)	Software Version(s)	-	C	LO
            RayExportWrapper.DicomAddProperty(0x00181020, "Raywatt SW Version", 0);
            //(0018, 1063)	Frame Time	-	U	DS
            //(0018, 3101)	IVUS Pullback Rate	-	U	DS
            //(0020, 000d)	Study Instance UID	-	M	UI
            //(0020, 000e)	Series Instance UID	-	M	UI
            //(0020, 0010)	Study ID	-	M	SH
            RayExportWrapper.DicomAddProperty(0x00200010, studyId.ToString(), 0);
            //(0020, 0011)	Series Number	-	M, C, U	IS
            RayExportWrapper.DicomAddProperty(0x00200011, seriesNumber.ToString(), 0);
            //(0020, 0013)	Instance Number	-	M	IS
            RayExportWrapper.DicomAddProperty(0x00200013, instanceNumber.ToString(), 0);
            //(0020, 0020)	Patient Orientation	-	C	CS
            //(0028, 0002)	Samples per Pixel	-	M	US
            //(0028, 0004)	Photometric interpretation	-	M	CS
            //(0028, 0006)	Planar Configuration	-	C	US
            //(0028, 0008)	Number of Frames	-	M	IS
            //(0028, 0009)	Frame Increment Pointer	-	C	AT
            //(0028, 0010)	Rows	-	M	US
            //(0028, 0011)	Columns	-	M	US
            //(0028, 0100)	Bits Allocated	-	M	US
            //(0028, 0101)	Bits Stored	-	M	US
            //(0028, 0102)	High Bit	-	M	US
            //(0028, 0103)	Pixel Representation	-	M	US
            //(0028, 0301)	Burned In Annotation	-	M	CS
            //(0028, 2110)	Lossy Image Compression	-	U	CS
            //(0028, 2112)	Lossy Image Compression Ratio	-	U	DS
        }

        private string GetTimeOffset()
        {
            var offset = DateTimeOffset.Now.Offset;
            var hour = Math.Abs(offset.Hours);
            var minute = offset.Minutes;
            var sign = offset.Hours < 0 ? "-" : "+";

            return sign + String.Format("{0:00}", hour) + String.Format("{0:00}", minute);
        }

        private string GetAge(DateTime birthDate, DateTime createDate)
        {
            int age = 0;

            if (birthDate.Month < createDate.Month)
            {
                age = createDate.Year - birthDate.Year;
            }
            else if (birthDate.Month == createDate.Month)
            {
                if (birthDate.Day <= createDate.Day)
                {
                    age = createDate.Year - birthDate.Year;
                }
                else
                {
                    age = createDate.Year - birthDate.Year - 1;
                }
            }
            else
            {
                age = createDate.Year - birthDate.Year - 1;
            }

            return age.ToString();
        }

        private void SetSequenceProperty()
        {
            int itemnum = 4;

            RayExportWrapper.DicomStartSequenceProperty(itemnum);

            for (int i = 0; i < itemnum; i++)
            {
                RayExportWrapper.DicomAddSequenceProperty(i, 0x00186018, "11");
                RayExportWrapper.DicomAddSequenceProperty(i, 0x0018601a, "22");
            }
        }

        private string CreateStandardUniqueName(PatientCase patientCase)
        {
            return "oct-" + patientCase.CreateDate.ToString("yyyy MM dd HH-mm-ss");
        }
    }
}
