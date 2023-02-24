using log4net;
using RaywattApp.Common.File;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using System;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using RaywattApp.Models;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using RayCoreWrapper;

namespace RaywattApp.ViewModels.File
{
    public partial class FileImportViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        [ObservableProperty]
        private IList<Patient>? _patientList;

        [ObservableProperty]
        private IList<PatientCase>? _patientCaseList;

        private string curPath;

        private string _diskType; //CD/DVD, External Drive
        public string DiskType
        {
            get { return _diskType; }
            set 
            { 
                _diskType = value;
                GetDrive();
                OnPropertyChanged(nameof(DiskType));
            }
        }

        [ObservableProperty]
        private string _selectedFile;

        [ObservableProperty]
        private double _availableSpace;

        [ObservableProperty]
        private double _approximateImportSize;

        [ObservableProperty]
        private Dictionary<string, string> _externalDriveComboBox;

        [ObservableProperty]
        private bool isEnableExternalDrive;

        private string _selectedExternalDrive;
        public string SelectedExternalDrive
        {
            get { return _selectedExternalDrive; }
            set
            {
                if (_selectedExternalDrive != value || !externDriveInit)
                {
                    _selectedExternalDrive = value;
                    externalDrive = _selectedExternalDrive;

                    if (_selectedExternalDrive != null)
                    {
                        curPath = _selectedExternalDrive;
                        directoryProvider.GetDirectoryWithExtension(curPath);
                        DirItems = directoryProvider.DirItems;

                        externDriveInit = true;
                    }
                    else
                    {
                        DirItems = null;
                    }

                    OnPropertyChanged(nameof(SelectedExternalDrive));
                }
            }
        }

        private string externalDrive;

        [ObservableProperty]
        private Dictionary<string, object> _externalDriveList;

        [ObservableProperty]
        private string _mediaType;

        private bool cdInit = false;

        private bool externDriveInit = false;

        private DirectoryProvider directoryProvider;

        private ObservableCollection<Item> _dirItems;
        public ObservableCollection<Item> DirItems
        {
            get { return _dirItems; }
            set
            {
                _dirItems = value;

                if (_dirItems == null)
                {
                    PatientList = null;
                    PatientCaseList = null;
                    ApproximateImportSize = 0;
                }

                OnPropertyChanged(nameof(DirItems));
            }
        }

        private DirectoryItem _selectedDir;
        public DirectoryItem SelectedDir
        {
            get { return _selectedDir; }
            set 
            { 
                if(value != null)
                {
                    _selectedDir = value;
                    ReadFile(_selectedDir.Path);
                    OnPropertyChanged(nameof(SelectedDir));
                }
            }
        }

        private ICommand _showCaseCommand;
        public ICommand ShowCaseCommand
        {
            get { return this._showCaseCommand ?? (this._showCaseCommand = new RelayCommand<Patient>(ShowCase)); }
        }

        public FileImportViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            directoryProvider = new();
            ExternalDriveComboBox = new Dictionary<string, string>();
            ExternalDriveList = new Dictionary<string, object>();

            RayExportWrapper.CDBurnError cDBurnError;
            cDBurnError = RayExportWrapper.initDevice();
            _log.Debug("initDevice : " + cDBurnError);

            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(CheckDrive);
            timer.Start();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
            }

            SetCondition();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Cancel()
        {
            _log.Debug("Cancel");

            Close();
        }

        private void Close()
        {
            if (timer.IsEnabled)
                timer.Stop();

            CloseDialog();
        }

        protected override void Import()
        {
            _log.Debug("Import");

            if (PatientList != null && PatientList.Count > 0)
            {
                List<string> patientCases = new List<string>();
                IList<PatientCase> existPatientCases = new List<PatientCase>();

                foreach (Patient patient in PatientList)
                {
                    if(patient.PatientCaseList != null)
                    {
                        foreach(PatientCase patientCase in patient.PatientCaseList)
                        {
                            patientCases.Add(patientCase.Id);
                        }
                    }
                }

                if(patientCases.Count > 0)
                {
                    Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                    sqlParameters["ids"] = patientCases;
                    existPatientCases = _sqlManager.SelectPatientCaseByList(sqlParameters);
                }

                if(existPatientCases.Count > 0)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Data already exists.\r\nDo you want to import data?"];
                    parameter["patientCaseList"] = existPatientCases;
                    var result = _dialogService.OpenDialog(new FileImportDialogControl(), parameter);

                    if (result != null)
                    {
                        if(result.DialogAnswer == DialogResults.Answer.Yes)
                        {
                            InsertData();
                        }
                        else if(result.DialogAnswer == DialogResults.Answer.Extra)
                        {
                            InsertData(existPatientCases);
                        }
                    }
                }
                else
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Are you sure to import selected file?"];
                    var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter);

                    if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                    {
                        InsertData();
                    }
                }
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["There are no items selected."];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
            }
        }

        private void InsertData(IList<PatientCase> existPatientCases = null)
        {
            _log.Debug("InsertData");

            if (timer.IsEnabled)
                timer.Stop();

            try
            {
                Dictionary<string, string> importfiles = new Dictionary<string, string>();

                foreach (Patient patient in PatientList)
                {
                    if (patient.PatientCaseList != null)
                    {
                        foreach (PatientCase patientCase in patient.PatientCaseList)
                        {
                            if (existPatientCases != null)
                            {
                                bool exist = false;
                                foreach (PatientCase pc in existPatientCases)
                                {
                                    if (patientCase.Id.Equals(pc.Id))
                                    {
                                        exist = true;
                                        break;
                                    }
                                }
                                if (exist)
                                    continue;
                            }

                            //image
                            string srcPath = CommonUtil.GetDirectoryPath(SelectedDir.Path) + "\\" + patientCase.Image;
                            if (System.IO.File.Exists(srcPath))
                            {
                                string destPath = CommonUtil.CreateFolder(Constants.DataRootPath + "\\" + patientCase.PatientId) + "\\" + patientCase.Image;
                                importfiles.Add(srcPath, destPath);
                            }
                        }
                    }
                }

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["File Import"];
                parameter["fileImport"] = importfiles;
                var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Undefined)
                {
                    Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();

                    foreach (Patient patient in PatientList)
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
                                if (existPatientCases != null)
                                {
                                    bool exist = false;
                                    foreach (PatientCase pc in existPatientCases)
                                    {
                                        if (patientCase.Id.Equals(pc.Id))
                                        {
                                            exist = true;
                                            break;
                                        }
                                    }
                                    if (exist)
                                        continue;
                                }

                                sqlParameters.Clear();
                                sqlParameters["id"] = patientCase.Id;
                                sqlParameters["patient_id"] = patientCase.PatientId;
                                sqlParameters["physician_name"] = patientCase.PhysicianName;
                                sqlParameters["accession_number"] = patientCase.AccessionNumber;
                                sqlParameters["accession_name"] = patientCase.AccessionName;
                                sqlParameters["comment"] = patientCase.Comment;
                                sqlParameters["vessel"] = patientCase.Vessel;
                                sqlParameters["procedure"] = patientCase.Procedure;
                                sqlParameters["pullback_type"] = patientCase.PullbackType;
                                sqlParameters["angio_co_registration"] = patientCase.AngioCoRegistration;
                                sqlParameters["indicator_degree"] = patientCase.IndicatorDegree;
                                sqlParameters["preset_name"] = patientCase.PresetName;
                                sqlParameters["calcium_threshold"] = patientCase.CalciumThreshold;
                                sqlParameters["expansion_calculation"] = patientCase.ExpansionCalculation;
                                sqlParameters["expansion_threshold"] = patientCase.ExpansionThreshold;
                                sqlParameters["apposition_threshold"] = patientCase.AppositionThreshold;
                                sqlParameters["measurements"] = patientCase.Measurements;
                                sqlParameters["bookmarks"] = patientCase.Bookmarks;
                                sqlParameters["thumbnail_no"] = patientCase.ThumbnailNo;
                                sqlParameters["still_image_yn"] = patientCase.StillImageYn;
                                sqlParameters["create_date"] = patientCase.CreateDate;
                                sqlParameters["update_date"] = patientCase.UpdateDate;
                                string srcPath = CommonUtil.GetDirectoryPath(SelectedDir.Path) + "\\" + patientCase.Image;
                                sqlParameters["image"] = System.IO.File.Exists(srcPath) ? patientCase.Image : "";

                                _sqlManager.UpsertPatientCase(sqlParameters);
                            }
                        }
                    }

                    Close();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private void SetCondition()
        {
            string configDrive = Constants.SystemRootPath + "\\";

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.Name.Equals(configDrive))
                {
                    AvailableSpace = CommonUtil.ByteToGB(drive.AvailableFreeSpace);
                    break;
                }
            }

            if (DiskType == null)
                DiskType = Constants.FileDiskExternal;
        }

        private void CheckDrive(object sender, EventArgs e)
        {
            GetDrive();
        }

        private void GetDrive()
        {
            if (DiskType.Equals(Constants.FileDiskCd))
            {
                externDriveInit = false;

                RayExportWrapper.CDBurnError cDBurnError;
                cDBurnError = RayExportWrapper.checkDiskOnDrive();
                _log.Debug("checkDiskOnDrive : " + cDBurnError);

                if (cDBurnError == RayExportWrapper.CDBurnError.OK)
                {
                    if (!cdInit)
                    {
                        DriveInfo[] allDrives = DriveInfo.GetDrives();
                        foreach (DriveInfo d in allDrives)
                        {
                            if(d.DriveType == DriveType.CDRom)
                            {
                                curPath = d.Name;
                                break;
                            }
                        }

                        RayExportWrapper.MediaType mediaType;
                        mediaType = RayExportWrapper.getDiskType();
                        _log.Debug("getDiskType : " + mediaType);
                        if (SetMediaType(mediaType))
                        {
                            directoryProvider.GetDirectoryWithExtension(curPath);
                            DirItems = directoryProvider.DirItems;
                        }
                        else
                        {
                            DirItems = null;
                        }

                        cdInit = true;
                    }
                }
                else
                {
                    MediaType = Constants.MediaTypeNoDisc;

                    cdInit = false;

                    DirItems = null;
                }
            }
            else
            {
                cdInit = false;

                Dictionary<string, string> currExternalDrive = new Dictionary<string, string>();

                string firstExternalDrive = "";
                bool isFirstExternalDrive = true;

                ExternalDriveList.Clear();

                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady == true)
                    {
                        if (d.DriveType == DriveType.Removable)
                        {
                            currExternalDrive[d.Name] = d.Name;
                            long[] data = { d.TotalSize, d.AvailableFreeSpace };
                            ExternalDriveList.Add(d.Name, data);

                            if (isFirstExternalDrive)
                            {
                                firstExternalDrive = d.Name;
                                isFirstExternalDrive = false;
                            }
                        }
                    }
                }

                if (ExternalDriveComboBox.Count != currExternalDrive.Count)
                {
                    ExternalDriveComboBox = currExternalDrive;

                    if (ExternalDriveComboBox.Count > 0)
                    {
                        IsEnableExternalDrive = true;

                        if (String.IsNullOrEmpty(externalDrive))
                        {
                            SelectedExternalDrive = firstExternalDrive;
                        }
                        else
                        {
                            SelectedExternalDrive = externalDrive;
                        }
                    }
                    else
                    {
                        IsEnableExternalDrive = false;
                        DirItems = null;
                    }
                }

                if (ExternalDriveComboBox.Count == 1)
                {
                    if (!externDriveInit || !ExternalDriveComboBox.ContainsKey(SelectedExternalDrive))
                    {
                        SelectedExternalDrive = firstExternalDrive;
                    }
                }
                else if(ExternalDriveComboBox.Count == 0)
                {
                    DirItems = null;
                }
            }            
        }

        private void ReadFile(string path)
        {
            _log.Debug("ReadFile : " + path);

            ApproximateImportSize = 0;

            if (!path.ToLower().EndsWith(Constants.FileExtension))
            {
                PatientList = null;
                PatientCaseList = null;
                return;
            }
  
            string[] result = CommonUtil.Decryptor(path);

            if (result[0].Equals("1"))
            {
                IList<Patient> patients = new List<Patient>();

                string json = result[1];
                if (!String.IsNullOrEmpty(json))
                {
                    JObject obj = JObject.Parse(json);

                    ApproximateImportSize = CommonUtil.ByteToGB(GetLongValue(obj, "Size"));

                    JArray patientArray = JArray.Parse(GetStrValue(obj, "PatientList"));
                    foreach (JObject patientObj in patientArray)
                    {
                        Patient patient = new Patient();
                        patient.Id = GetStrValue(patientObj, "Id");
                        patient.Lastname = GetStrValue(patientObj, "Lastname");
                        patient.Firstname = GetStrValue(patientObj, "Firstname");
                        patient.Name = patient.Firstname + ", " + patient.Lastname;
                        patient.Birthdate = GetDateValue(patientObj, "Birthdate");
                        patient.Gender = GetStrValue(patientObj, "Gender");
                        patient.CreateDate = GetDateValue(patientObj, "CreateDate");
                        patient.UpdateDate = GetDateValue(patientObj, "UpdateDate");

                        JArray caseArray = JArray.Parse(GetStrValue(patientObj, "PatientCaseList"));
                        patient.PatientCaseList = new List<PatientCase>();
                        foreach (JObject caseObj in caseArray)
                        {
                            PatientCase patientCase = new PatientCase();
                            patientCase.Id = GetStrValue(caseObj, "Id");
                            patientCase.PatientId = GetStrValue(caseObj, "PatientId");
                            patientCase.PhysicianName = GetStrValue(caseObj, "PhysicianName");
                            patientCase.AccessionNumber = GetStrValue(caseObj, "AccessionNumber");
                            patientCase.AccessionName = GetStrValue(caseObj, "AccessionName");
                            patientCase.Comment = GetStrValue(caseObj, "Comment");
                            patientCase.Vessel = GetStrValue(caseObj, "Vessel");
                            patientCase.Procedure = GetStrValue(caseObj, "Procedure");
                            patientCase.ThumbnailNo = GetIntValue(caseObj, "ThumbnailNo");
                            patientCase.StillImageYn = GetStrValue(caseObj, "StillImageYn");
                            patientCase.Image = GetStrValue(caseObj, "Image");
                            patientCase.PullbackType = GetStrValue(caseObj, "PullbackType");
                            patientCase.AngioCoRegistration = GetBoolValue(caseObj, "AngioCoRegistration");
                            patientCase.IndicatorDegree = GetDoubleValue(caseObj, "IndicatorDegree");
                            patientCase.PresetName = GetStrValue(caseObj, "PresetName");
                            patientCase.CalciumThreshold = GetIntValue(caseObj, "CalciumThreshold");
                            patientCase.ExpansionCalculation = GetStrValue(caseObj, "ExpansionCalculation");
                            patientCase.ExpansionThreshold = GetIntValue(caseObj, "ExpansionThreshold");
                            patientCase.AppositionThreshold = GetDoubleValue(caseObj, "AppositionThreshold");
                            patientCase.Measurements = GetStrValue(caseObj, "Measurements");
                            patientCase.Bookmarks = GetStrValue(caseObj, "Bookmarks");
                            patientCase.CreateDate = GetDateValue(caseObj, "CreateDate");
                            patientCase.UpdateDate = GetDateValue(caseObj, "UpdateDate");

                            patient.PatientCaseList.Add(patientCase);
                        }
                        patients.Add(patient);
                    }
                }
                PatientList = patients;
            }
            else
            {
                PatientList = null;
                PatientCaseList = null;

                _log.Error("File Decrypt Error");
            }
        }

        private void ShowCase(Patient patient)
        {
            _log.Debug("ShowCase");

            if(patient == null || patient.PatientCaseList == null)
            {
                PatientCaseList = null;
            }
            else
            {
                PatientCaseList = patient.PatientCaseList;
            }
        }

        private string GetStrValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return "";

            return obj[key].ToString();
        }

        private DateTime GetDateValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return DateTime.Now;

            return Convert.ToDateTime(obj[key]);
        }

        private long GetLongValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return 0;

            return (long)obj[key];
        }

        private double GetDoubleValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return 0;

            return (double)obj[key];
        }

        private int GetIntValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return 0;

            return (int)obj[key];
        }

        private bool GetBoolValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return false;

            return (bool)obj[key];
        }

        private bool SetMediaType(RayExportWrapper.MediaType mediaType)
        {
            bool res;

            switch (mediaType)
            {
                case RayExportWrapper.MediaType.NotSupportDisc:
                    MediaType = Constants.MediaTypeNotSupportDisc;
                    res = false;
                    break;
                case RayExportWrapper.MediaType.TYPE_CDR:
                    MediaType = Constants.MediaTypeCDR;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_CDRW:
                    MediaType = Constants.MediaTypeCDRW;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDDASHR:
                    MediaType = Constants.MediaTypeDVDDASHR;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDDASHRW:
                    MediaType = Constants.MediaTypeDVDDASHRW;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDPLUSR:
                    MediaType = Constants.MediaTypeDVDPLUSR;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDPLUSRW:
                    MediaType = Constants.MediaTypeDVDPLUSRW;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_BDR:
                    MediaType = Constants.MediaTypeBDR;
                    res = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_BDRE:
                    MediaType = Constants.MediaTypeBDRE;
                    res = true;
                    break;
                default:
                    MediaType = Constants.MediaTypeNotSupportDisc;
                    res = false;
                    break;
            }

            return res;
        }
    }
}
