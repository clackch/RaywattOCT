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
using System.Threading.Tasks;
using RaywattApp.Services;
using Newtonsoft.Json.Linq;
using static RaywattOCT.RayCoreWrapper;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Configuration;
using Configuration = RaywattApp.Models.Configuration;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileCopyDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileCopyDialogViewModel));

        private readonly SqlManager _sqlManager;

        private Dictionary<string, string> copyfiles;

        private Dictionary<string, string> dicomProperty;

        [ObservableProperty]
        private FileExport? _fileExport;

        [ObservableProperty]
        private IList<Patient>? _patients;

        [ObservableProperty]
        private IList<PatientCase>? _patientCases;

        [ObservableProperty]
        private string? _progressText;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string? _dbFilePath;

        [ObservableProperty]
        private string? _contents;

        [ObservableProperty]
        private string? _path;

        [ObservableProperty]
        private string? _annotationFilePath;

        [ObservableProperty]
        private string? _annotations;

        [ObservableProperty]
        private string? _saveFolder;

        [ObservableProperty]
        private bool enableDone = false;

        private bool isPacs;

        private IntPtr dicomClient;

        [ObservableProperty]
        private DicomServer _selectedDicomServer;

        [ObservableProperty]
        private string _localHostAeTitle;

        private bool usePeerVerification = true;

        public FileCopyDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Title = data["title"].ToString();

            //Export
            if (data.TryGetValue("fileExport", out var fileExportObj) && fileExportObj is FileExport export)
            {
                FileExport = export;
                PatientCases = (IList<PatientCase>)data["patientCases"];

                if (FileExport.Type == Constants.ExportTypeNative)
                {
                    DbFilePath = data["dbFilePath"].ToString();
                    Contents = data["contents"].ToString();
                    AnnotationFilePath = data["annotationFilePath"].ToString();
                    Annotations = data["annotations"].ToString();
                }
                else if (FileExport.Type == Constants.ExportTypeDicom)
                {
                    dicomProperty = (Dictionary<string, string>)data["dicomProperty"];
                    this.isPacs = (bool)data["isPacs"];
                    if (this.isPacs)
                    {
                        LocalHostAeTitle = (string)data["localHostAeTitle"];
                        SelectedDicomServer = (DicomServer)data["selectedDicomServer"];
                        this.usePeerVerification = (bool)data["usePeerVerification"];
                    }
                }

                if (FileExport != null)
                    FileExportAction();
            }
            else if (data.TryGetValue("fileImport", out var importObj) && importObj is Dictionary<string, string> importFiles)//Import
            {
                copyfiles = importFiles;
                Patients = (IList<Patient>)data["patients"];
                Path = data["path"].ToString();
                AnnotationFilePath = data["annotationFilePath"].ToString();

                if (copyfiles != null)
                    FileImportAction();
            }
            else if (data.TryGetValue("logExport", out var logObj) && logObj is Dictionary<string, string> logFiles)//logExport
            {
                copyfiles = logFiles;

                if (copyfiles != null)
                    LogExportAction();
            }
        }

        private async void FileExportAction()
        {
            SaveFolder = FileExport.ExternalDrivePath;
            bool isError = false;

            if (FileExport.Type == Constants.ExportTypeNative)
            {
                await FileSaveNative();
            }
            else if (FileExport.Type == Constants.ExportTypeDicom)
            {
                isError = await FileSaveDicom();
            }
            else if (FileExport.Type == Constants.ExportTypeStandard)
            {
                await FileSaveStandard();
            }

            if(!isError)
                ProgressText = Constants.ExportStatusCompleted;
            EnableDone = true;
        }

        private async void FileImportAction()
        {
            await FileImport();

            ProgressText = Constants.ExportStatusCompleted;
            EnableDone = true;
        }

        private async void LogExportAction()
        {
            await LogExport();

            ProgressText = Constants.ExportStatusCompleted;
            EnableDone = true;
        }

        private async Task FileSaveNative()
        {
            double progressSize = 80.0;
            double annotationPs = 15;
            double contentPs = 5;

            Dictionary<string, string> fileCopyInfo;
            fileCopyInfo = new Dictionary<string, string>();
            foreach (PatientCase patientCase in PatientCases)
            {
                string fileName = CommonUtil.GetFileName(patientCase.ImageFullPath);
                string dstFilePath = SaveFolder + "\\" + fileName;
                fileCopyInfo.Add(patientCase.ImageFullPath, dstFilePath);


                if (patientCase.AngioYn)
                {
                    List<string> extensions = new List<string> { Constants.AngioImageExtension, Constants.AngioParmasExtension };
                    fileName = fileName.Substring(0, patientCase.Image.Length - 3);
                    string srcFilePath = patientCase.ImageFullPath.Substring(0, patientCase.ImageFullPath.Length - 3);
                    foreach (string ext in extensions)
                    {
                        string tmpFileName = fileName + ext;
                        string tmpSrcFilePath = srcFilePath + ext;
                        dstFilePath = SaveFolder + "\\" + tmpFileName;
                        fileCopyInfo.Add(tmpSrcFilePath, dstFilePath);
                    }
                }
            }

            await CommonUtil.CopyFiles(fileCopyInfo, prog => Progress = prog, progressSize, progText => ProgressText = progText);

            await CommonUtil.Encryptor(SaveFolder + "\\" + AnnotationFilePath, Annotations, prog => Progress += prog, annotationPs, progText => ProgressText = progText);

            await CommonUtil.Encryptor(SaveFolder + "\\" + DbFilePath, Contents, prog => Progress += prog, contentPs, progText => ProgressText = progText);
        }

        private async Task<bool> FileSaveDicom()
        {
            //AE Title
            string aeTitle = "";
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "LocalHost";
            IList<Configuration> localHost = _sqlManager.SelectConfiguration(sqlParameters);
            if (localHost != null && localHost.Count > 0)
            {
                aeTitle = localHost.FirstOrDefault(x => x.Key == "AeTitle").Value;
            }

            //DICOMDIR Input Folder
            string dicomDirFolder = this.isPacs ? Constants.DicomTempFolderPath : SaveFolder + "\\" + DateTime.Now.ToString("yyyyMMddHHmmss");
            CommonUtil.DeleteFolder(dicomDirFolder);
            dicomDirFolder = CommonUtil.CreateFolder(dicomDirFolder);
            RayExportWrapper.DICOMDIRInputFolder(dicomDirFolder);

            //Group by Patient
            var patientGrp = from arr in PatientCases
                             orderby arr.CreateDate descending
                             group arr by arr.PatientId into g
                             select g;

            int index = 0, studyId, seriesNumber;
            double progressConvert = 100.0 / (this.isPacs ? 4 : 3) / PatientCases.Count;

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

                        RaySetProperty(Property.Brightness, patientCase.Brightness);
                        RaySetProperty(Property.Contrast, patientCase.Contrast);
                        CommonUtil.SetColormap(patientCase.Colormap);
                        List<Mat> imgCrossSections = new List<Mat>();
                        Mat? imgLongitude = await CommonUtil.ConvertImage(patientCase.ImageFullPath, patientCase.ImageResolution, patientCase.ZOffset, patientCase.IndicatorDegree, imgCrossSections, prog => Progress += prog, progressConvert, progText => ProgressText = progText);
                        List<Mat> convertedImages = await CommonUtil.MakeImageForExport(patientCase, imgCrossSections, imgLongitude, exportIndices, FileExport, prog => Progress += prog, progressConvert, progText => ProgressText = progText);

                        await Task.Run(() =>
                        {
                            //Start
                            RayExportWrapper.DicomStart();

                            //Image
                            RayExportWrapper.DicomImageStart(convertedImages.Count);
                            for (int frame = 0; frame < convertedImages.Count; frame++)
                            {
                                Mat imgExport = convertedImages[frame];
                                Cv2.CvtColor(imgExport, imgExport, ColorConversionCodes.RGB2BGR);
                                RayExportWrapper.DicomAddImage(imgExport.Cols, imgExport.Rows, imgExport.Data);
                            }
                            RayExportWrapper.DicomImageFinish();

                            //Property
                            SetProperty(patientCase, studyId, seriesNumber, FileExport.Material != Constants.ExportMaterialCurrent ? 0 : FileExport.CurrentFrame, aeTitle);

                            //Sequence Property
                            SetSequenceProperty();

                            //File Size
                            long dicomApprSize = RayExportWrapper.DicomApprSize();

                            //file path
                            string filePath = Constants.ExportDicomPrefix + string.Format("{0:0000}", index);

                            //DICOM Save Check Start
                            var t = Task.Run(() => CommonUtil.CheckFileSaveDone(dicomDirFolder + "\\" + filePath, dicomApprSize, prog => Progress = prog, Progress, progressConvert, progText => ProgressText = progText));

                            //DICOM Save
                            RayExportWrapper.DicomSave(dicomDirFolder + "\\" + filePath);

                            //DICOM Save Check Done
                            t.Wait();

                            //DICOMDIR Input File
                            RayExportWrapper.DICOMDIRInputFile(filePath);
                        });

                        index++;
                        seriesNumber++;
                    }
                    studyId++;
                }
            }

            //DICOMDIR Write
            RayExportWrapper.DICOMDIRWrite();

            if (this.isPacs)
            {
                bool isDicomError = false;
                ProgressText = Constants.ExportStatusTransferDicom;

                dicomClient = RayExportWrapper.CreateDcmClient();               

                RayExportWrapper.DicomNetRWError res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Initialize(dicomClient, LocalHostAeTitle, SelectedDicomServer.IpAddress, int.Parse(SelectedDicomServer.Port), SelectedDicomServer.AeTitle, SelectedDicomServer.TlsYn, this.usePeerVerification, SelectedDicomServer.CaFilePath));
                _log.DebugFormat("Initialize : {0}", res);

                if (res == RayExportWrapper.DicomNetRWError.Normal)
                {
                    var files = Directory.GetFiles(Constants.DicomTempFolderPath)
                        .Where(f => System.IO.Path.GetFileName(f).StartsWith(Constants.ExportDicomPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

                    int count = files.Count;

                    foreach (var file in files)
                    {
                        _log.Debug(file);

                        res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.StoreFile(dicomClient, file));
                        _log.DebugFormat("StoreFile : {0}", res);

                        if (res == RayExportWrapper.DicomNetRWError.Normal)
                        {
                            Progress = Progress + progressConvert / count;
                        }
                        else
                        {
                            isDicomError = true;
                            ProgressText = CommonUtil.GetDicomResultMessage(res);
                            break;
                        }
                    }
                }
                else
                {
                    isDicomError = true;
                    ProgressText = CommonUtil.GetDicomResultMessage(res);
                }

                RayExportWrapper.DestroyDcmClient(dicomClient);

                CommonUtil.DeleteFolder(Constants.DicomTempFolderPath);

                if (isDicomError)
                    return true;
            }

            return false;
        }

        private async Task FileSaveStandard()
        {
            foreach (PatientCase patientCase in PatientCases)
            {
                string format = (FileExport.Material == Constants.ExportMaterialPullback) ? FileExport.Pullback : FileExport.StillFrame;
                double progressPerCase = 100.0 / PatientCases.Count;
                double progressConvert = progressPerCase / 3;
                double progressSave = progressPerCase / 3;

                List<int>? exportIndices = null;
                if (FileExport.Material == Constants.ExportMaterialCurrent)
                {
                    exportIndices = new List<int>();
                    exportIndices.Add(FileExport.CurrentFrame);
                }
                else if (FileExport.Material == Constants.ExportMaterialBookmarked)
                {
                    exportIndices = FileExport.BookmarkedFrames;
                }

                RaySetProperty(Property.Brightness, patientCase.Brightness);
                RaySetProperty(Property.Contrast, patientCase.Contrast);
                CommonUtil.SetColormap(patientCase.Colormap);
                List<Mat> imgCrossSections = new List<Mat>();
                Mat? imgLongitude = await CommonUtil.ConvertImage(patientCase.ImageFullPath, patientCase.ImageResolution, patientCase.ZOffset, patientCase.IndicatorDegree, imgCrossSections, prog => Progress += prog, progressConvert, progText => ProgressText = progText);
                List<Mat> convertedImages = await CommonUtil.MakeImageForExport(patientCase, imgCrossSections, imgLongitude, exportIndices, FileExport, prog => Progress += prog, progressConvert, progText => ProgressText = progText);

                if (format == Constants.ExportPullbackAVI)
                {
                    string fileName = CreateStandardUniqueName(patientCase);
                    await CommonUtil.SaveVideo(convertedImages, SaveFolder, fileName, format, 10, prog => Progress += prog, progressSave, progText => ProgressText = progText);
                }
                else if (format == Constants.ExportPullbackTIFF)
                {
                    string fileName = CreateStandardUniqueName(patientCase);
                    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

                    Thread threadTiffProcessByPython = new Thread(() => CommonUtil.TiffProcessByPython(convertedImages, SaveFolder, fileName, format, _cancellationTokenSource));
                    threadTiffProcessByPython.Start();

                    double progressBase = Progress;
                    await Task.Run(() => ProgressPipeWithPython(_cancellationTokenSource.Token, convertedImages.Count, progressSave));
                    Progress = progressBase + progressSave;
                }
                else
                {
                    double progressIncrease = progressSave / convertedImages.Count;
                    for (int frame = 0; frame < convertedImages.Count; frame++)
                    {
                        string fileName = CreateStandardUniqueName(patientCase) + string.Format("-{0:0000}", exportIndices[frame] + 1);
                        await CommonUtil.SaveStillFrame(convertedImages[frame], SaveFolder, fileName, format, prog => Progress += prog, progressIncrease, progText => ProgressText = progText);
                        Progress += (progressSave / convertedImages.Count);
                    }
                }
            }
        }

        private void ProgressPipeWithPython(CancellationToken token, int totalNum, double progressLeft)
        {
            _log.Debug("[START]ThreadProgressPipe");

            double progressBase = Progress;
            double convertProgressCnt = 0;

            while (!token.IsCancellationRequested)
            {
                using (var pipeServer = new NamedPipeServerStream("progress_pipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances))
                {
                    try
                    {
                        // WaitForConnection 호출 시 CancellationToken을 감안
                        var waitTask = Task.Run(() => pipeServer.WaitForConnection(), token);

                        // CancellationToken에 따라 대기
                        waitTask.Wait(token);

                        // 연결이 성공적으로 완료된 경우
                        using (StreamReader reader = new StreamReader(pipeServer))
                        {
                            string message;
                            while ((message = reader.ReadLine()) != null)
                            {
                                _log.Debug($"Progress update: {message}");

                                if (message.StartsWith("_converting_"))
                                {
                                    Progress = progressBase + (progressLeft / 2) * (++convertProgressCnt / totalNum);                                    
                                }
                                else if (message.StartsWith("_save_"))
                                {
                                    ProgressText = Constants.ExportStatusSaveMultipleFrames;
                                    Progress = progressBase + (progressLeft / 2);
                                }
                                else if (message.StartsWith("_move_"))
                                {
                                    Progress = progressBase + (progressLeft / 2) + (progressLeft / 4);
                                }
                                else if (message.StartsWith("_moving_"))
                                {
                                    double moveProgress = double.Parse(message.Substring(9));
                                    Progress = progressBase + (progressLeft / 2) + (progressLeft / 4) + (progressLeft / 4) * moveProgress;
                                }
                                else if (message.StartsWith("_end_"))
                                {
                                    Progress = progressBase + progressLeft;
                                    return;
                                }
                                else if (message.StartsWith("_error_"))
                                {
                                    _log.Error(message);
                                    return;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _log.Debug("Cancellation requested. Exiting loop.");
                        break;
                    }
                    catch (IOException ex)
                    {
                        _log.Debug($"Connection error: {ex.Message}");
                    }
                }
            }

            _log.Debug("[END]ThreadProgressPipe");
        }

        private async Task FileImport()
        {
            double progressSize = 90.0;
            double progressInsert = 10.0;
            if (copyfiles.Count > 0)
                await CommonUtil.CopyFiles(copyfiles, prog => Progress = prog, progressSize, progText => ProgressText = progText);
            else
                progressInsert = 100.0;
            await InsertData(Patients, Path, AnnotationFilePath, prog => Progress += prog, progressInsert, progText => ProgressText = progText);
        }

        private async Task InsertData(IList<Patient> patients, string path, string annotationPath, Action<double> progressCallback, double progress, Action<string> progressTextCallback)
        {

            List<PatientCaseAnnotation> annotations = new List<PatientCaseAnnotation>();
            if (annotationPath != null && annotationPath.Length > 0)
            {
                Tuple<bool, string> result = CommonUtil.Decryptor(CommonUtil.GetDirectoryPath(path) + "\\" + annotationPath);

                if (result.Item1)
                {
                    string json = result.Item2;
                    if (!String.IsNullOrEmpty(json))
                    {
                        JArray arr = JArray.Parse(json);
                        foreach (JObject obj in arr)
                        {
                            PatientCaseAnnotation patientCaseAnnotation = new PatientCaseAnnotation();
                            patientCaseAnnotation.Id = obj["Id"]?.ToString() ?? "null";
                            patientCaseAnnotation.Bookmark = obj["Bookmark"]?.ToString() ?? "null";
                            patientCaseAnnotation.Longitude = obj["Longitude"]?.ToString() ?? "null";
                            patientCaseAnnotation.CrossSection = obj["CrossSection"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenContour = obj["LumenContour"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenSidebranch = obj["LumenSidebranch"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenStent = obj["LumenStent"]?.ToString() ?? "null";
                            patientCaseAnnotation.LumenGuidewire = obj["LumenGuidewire"]?.ToString() ?? "null";
                            patientCaseAnnotation.FfrPlaque = obj["FfrPlaque"]?.ToString() ?? "null";
                            patientCaseAnnotation.CoRegistration = obj["CoRegistration"]?.ToString() ?? "null";
                            annotations.Add(patientCaseAnnotation);
                        }
                    }
                }
            }

            int cnt = 0;
            foreach (Patient patient in patients)
            {
                cnt += patient.PatientCaseList.Count;
            }

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();

            foreach (Patient patient in patients)
            {
                sqlParameters.Clear();
                sqlParameters["id"] = patient.Id;
                sqlParameters["lastname"] = patient.Lastname;
                sqlParameters["firstname"] = patient.Firstname;
                sqlParameters["birthdate"] = patient.Birthdate;
                sqlParameters["gender"] = patient.Gender;
                sqlParameters["create_date"] = patient.CreateDate;
                sqlParameters["update_date"] = patient.UpdateDate;
                _sqlManager.UpsertPatient(sqlParameters);

                if (patient.PatientCaseList != null)
                {
                    foreach (PatientCase patientCase in patient.PatientCaseList)
                    {
                        await Task.Run(() => {
                            sqlParameters.Clear();
                            sqlParameters["id"] = patientCase.Id;
                            sqlParameters["patient_id"] = patientCase.PatientId;
                            sqlParameters["physician_name"] = patientCase.PhysicianName;
                            sqlParameters["accession_number"] = patientCase.AccessionNumber;
                            sqlParameters["comment"] = patientCase.Comment;
                            sqlParameters["vessel"] = patientCase.Vessel;
                            sqlParameters["location"] = patientCase.Location;
                            sqlParameters["procedure"] = patientCase.Procedure;
                            sqlParameters["pullback_type"] = patientCase.PullbackType;
                            sqlParameters["pullback_length"] = patientCase.PullbackLength;
                            sqlParameters["angio_yn"] = patientCase.AngioYn;
                            sqlParameters["angio_co_registration"] = patientCase.AngioCoRegistration;
                            sqlParameters["indicator_degree"] = patientCase.IndicatorDegree;
                            sqlParameters["flush_media"] = patientCase.FlushMedia;
                            sqlParameters["pullback_trigger"] = patientCase.PullbackTrigger;
                            sqlParameters["colormap"] = patientCase.Colormap;
                            sqlParameters["calcium_threshold"] = patientCase.CalciumThreshold;
                            sqlParameters["expansion_calculation"] = patientCase.ExpansionCalculation;
                            sqlParameters["expansion_threshold"] = patientCase.ExpansionThreshold;
                            sqlParameters["apposition_threshold"] = patientCase.AppositionThreshold;
                            sqlParameters["num_of_frames"] = patientCase.NumOfFrames;
                            sqlParameters["field_of_view"] = patientCase.FieldOfView;
                            sqlParameters["brightness"] = patientCase.Brightness;
                            sqlParameters["contrast"] = patientCase.Contrast;
                            sqlParameters["sheath_diameter"] = patientCase.SheathDiameter;
                            sqlParameters["section_proximal"] = patientCase.SectionProximal;
                            sqlParameters["section_distal"] = patientCase.SectionDistal;
                            sqlParameters["create_date"] = patientCase.CreateDate;
                            sqlParameters["update_date"] = patientCase.UpdateDate;
                            string srcPath = CommonUtil.GetDirectoryPath(path) + "\\" + patientCase.Image;
                            sqlParameters["image"] = System.IO.File.Exists(srcPath) ? patientCase.Image : "";
                            sqlParameters["image_resolution"] = patientCase.ImageResolution;
                            sqlParameters["guidewire_radius"] = patientCase.GuidewireRadius;
                            sqlParameters["z_offset"] = patientCase.ZOffset;

                            var nRows = _sqlManager.UpsertPatientCase(sqlParameters);
                            if (nRows == 1)
                            {
                                foreach (PatientCaseAnnotation annotation in annotations)
                                {
                                    if (annotation.Id == patientCase.Id)
                                    {
                                        sqlParameters.Clear();
                                        sqlParameters["id"] = patientCase.Id;
                                        sqlParameters["cross_section"] = annotation.CrossSection;
                                        sqlParameters["longitude"] = annotation.Longitude;
                                        sqlParameters["bookmark"] = annotation.Bookmark;
                                        sqlParameters["lumen_contour"] = annotation.LumenContour;
                                        sqlParameters["lumen_sidebranch"] = annotation.LumenSidebranch;
                                        sqlParameters["lumen_stent"] = annotation.LumenStent;
                                        sqlParameters["lumen_guidewire"] = annotation.LumenGuidewire;
                                        sqlParameters["ffr_plaque"] = annotation.FfrPlaque;
                                        sqlParameters["co_registration"] = annotation.CoRegistration;
                                        nRows = _sqlManager.UpsertPatientCaseAnnotation(sqlParameters);
                                        if (nRows == 0)
                                            _log.Error("Upsert Error");

                                        break;
                                    }
                                }
                            }
                            else
                            {
                                _log.Error("Upsert Error");
                            }

                            progressCallback(progress / cnt);
                            progressTextCallback(Constants.ExportStatusSaveFile);
                        });
                    }
                }
            }

            if (cnt == 0)
            {
                await Task.Run(() => {
                    progressCallback(progress);
                    progressTextCallback(Constants.ExportStatusSaveFile);
                });
            }
        }

        private async Task LogExport()
        {
            await CommonUtil.CopyFiles(copyfiles, prog => Progress = prog, 100.0, progText => ProgressText = progText);
        }

        private void SetProperty(PatientCase patientCase, int studyId, int seriesNumber, int instanceNumber, string aeTitle)
        {
            string dateTimeNow = DateTime.Now.ToString("yyyyMMddHHmmss");

            RayExportWrapper.DicomStartProperty();

            string implementationClassUID = dicomProperty["ORG_RT"] + "." + dicomProperty["APP_ID"];

            //(0002, 0001)	File Meta Information Version	-	M	OB
            //Auto Assigned
            //(0002, 0002)	Media Storage SOP Class UID	-	M	UI
            //Auto Assigned
            //(0002, 0003)	Media Storage SOP Instance UID	-	M	UI
            RayExportWrapper.DicomAddProperty(0x00020003, implementationClassUID + "." + dateTimeNow, 0);
            //(0002, 0010)	Transfer Syntax UID	-	M	UI
            //Auto Assigned
            //(0002, 0012)	Implementation Class UID	-	M	UI
            RayExportWrapper.DicomAddProperty(0x00020012, implementationClassUID, 0);
            //(0002, 0013)	Implementation Version Name	-	C	SH
            RayExportWrapper.DicomAddProperty(0x00020013, ConfigurationManager.AppSettings.Get("SoftwareName"), 0);
            //(0002, 0016)	Source Application Entity Title	-	M	AE
            RayExportWrapper.DicomAddProperty(0x00020016, aeTitle, 0);
            //(0008, 0005)	Specific Character Set	-	C	CS
            //(0008, 0008)	Image Type	-	M	CS
            //(0008, 0012)	Instance Creation Date	-	M	DA
            //(0008, 0013)	Instance Creation Time	-	M	TM
            //(0008, 0016)	SOP Class UID	-	M	UI
            //Auto Assigned (1.2.840.10008.5.1.4.1.1.7.4)
            //(0008, 0018)	SOP Instance UID	-	M	UI
            RayExportWrapper.DicomAddProperty(0x00080018, implementationClassUID + "." + dateTimeNow, 0);
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
            RayExportWrapper.DicomAddProperty(0x00080060, dicomProperty["00080060"], 0);
            //(0008, 0064)	Conversion Type	-	U	CS
            RayExportWrapper.DicomAddProperty(0x00080064, dicomProperty["00080064"], 0);
            //(0008, 0070)	Manufacturer	-	M, C, U	LO
            RayExportWrapper.DicomAddProperty(0x00080070, ConfigurationManager.AppSettings.Get("Manufacturer"), 0);
            //(0008, 0080)	Institution Name	-	M	LO
            RayExportWrapper.DicomAddProperty(0x00080080, dicomProperty["00080080"], 0);
            //(0008, 0090)	Referring Physician's Name	-	C	PN
            RayExportWrapper.DicomAddProperty(0x00080090, patientCase.PhysicianName, 0);
            //(0008, 0201)	Timezone Offset From UTC	-	C	SH
            RayExportWrapper.DicomAddProperty(0x00080201, GetTimeOffset(), 0);
            //(0008, 1050)	Performing Physician's Name	-	C	PN
            RayExportWrapper.DicomAddProperty(0x00081050, patientCase.PhysicianName, 0);
            //(0008, 1090)	Manufacturer's Model Name	-	M	LO
            RayExportWrapper.DicomAddProperty(0x00081090, ConfigurationManager.AppSettings.Get("ModelName"), 0);
            //(0008, 2144)	Recommended Display Frame Rate	-	U	IS
            //(0010, 0010)	Patient's Name	-	M	PN
            RayExportWrapper.DicomAddProperty(0x00100010, patientCase.PatientName.Replace(", ", "^"), 0);//Convert to DICOM Format (Firstname + ^ + Lastname)
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
            RayExportWrapper.DicomAddProperty(0x00180015, dicomProperty["00180015"], 0);
            //(0018, 1016)	Secondary Capture Device Manufacturer	-	U	LO
            RayExportWrapper.DicomAddProperty(0x00181016, ConfigurationManager.AppSettings.Get("Manufacturer"), 0);
            //(0018, 1018)	Secondary Capture Device Manufacturer's Model Name	-	U	LO
            RayExportWrapper.DicomAddProperty(0x00181018, ConfigurationManager.AppSettings.Get("ModelName"), 0);
            //(0018, 1019)	Secondary Capture Device Software Versions	-	U	LO
            RayExportWrapper.DicomAddProperty(0x00181019, ConfigurationManager.AppSettings.Get("SoftwareVersion"), 0);
            //(0018, 1020)	Software Version(s)	-	C	LO
            RayExportWrapper.DicomAddProperty(0x00181020, ConfigurationManager.AppSettings.Get("SoftwareVersion"), 0);
            //(0018, 0025)  Angio Flag	-	?	CS
            RayExportWrapper.DicomAddProperty(0x00180025, FileExport.AngioView ? "Y" : "N", 0);
            //(0018, 1063)	Frame Time	-	U	DS
            //(0018, 3101)	IVUS Pullback Rate	-	U	DS
            //(0020, 000d)	Study Instance UID	-	M	UI
            RayExportWrapper.DicomAddProperty(0x0020000d, implementationClassUID + "." + patientCase.CreateDate.ToString("yyyyMMdd") + "000000." + studyId.ToString(), 0);
            //(0020, 000e)	Series Instance UID	-	M	UI
            RayExportWrapper.DicomAddProperty(0x0020000e, implementationClassUID + "." + patientCase.CreateDate.ToString("yyyyMMddhhmmss") + "." + seriesNumber.ToString(), 0);
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

        private static string GetTimeOffset()
        {
            var offset = DateTimeOffset.Now.Offset;
            var hour = Math.Abs(offset.Hours);
            var minute = offset.Minutes;
            var sign = offset.Hours < 0 ? "-" : "+";

            return sign + String.Format("{0:00}", hour) + String.Format("{0:00}", minute);
        }

        private static string GetAge(DateTime birthDate, DateTime createDate)
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

        private static void SetSequenceProperty()
        {
            int itemnum = 4;

            RayExportWrapper.DicomStartSequenceProperty(itemnum);

            for (int i = 0; i < itemnum; i++)
            {
                //(0018, 6018)  Region Location Min X0	-	?	UL
                RayExportWrapper.DicomAddSequenceProperty(i, 0x00186018, "11");
                //(0018, 601a)  Region Location Min Y0	-	?	UL
                RayExportWrapper.DicomAddSequenceProperty(i, 0x0018601a, "22");
                //(0018, 602c)  Physical Delta X	-	?	FD
                RayExportWrapper.DicomAddSequenceProperty(i, 0x0018602c, "33");
                //(0018, 602e)  Physical Delta Y	-	?	FD
                RayExportWrapper.DicomAddSequenceProperty(i, 0x0018602e, "44");
            }
        }

        private string CreateStandardUniqueName(PatientCase patientCase)
        {
            return "oct-" + patientCase.CreateDate.ToString("yyyy MM dd HH-mm-ss");
        }
    }
}
