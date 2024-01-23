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
using System.Linq;

namespace RaywattApp.ViewModels.File
{
    public partial class FileImportViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        private string curPath;

        private string annotationFilePath;

        private string externalDrive;

        private bool externDriveInit = false;

        private DirectoryProvider directoryProvider;

        [ObservableProperty]
        private IList<Patient>? _patientList;

        [ObservableProperty]
        private IList<PatientCase>? _patientCaseList;

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
                        directoryProvider.GetDirectoryWithExtension(curPath.Replace("\\", ""));
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

        [ObservableProperty]
        private Dictionary<string, object> _externalDriveList;

        [ObservableProperty]
        private string _mediaType;

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
                    parameter["message"] = _l10n["Data already exists. Overwrite existing data?"];
                    parameter["patientCaseList"] = existPatientCases;
                    var result = _dialogService.OpenDialog(new FileImportDialogControl(), parameter, Constants.FileImportDialogWidth, Constants.FileImportDialogHeight);

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
                    parameter["message"] = _l10n["Confirm import of selected file"];
                    var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.FileImportDialogWidth, Constants.FileImportDialogHeight);

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
                parameter["message"] = _l10n["No items have been selected"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.FileImportDialogWidth, Constants.FileImportDialogHeight);
            }
        }

        private void InsertData(IList<PatientCase> existPatientCases = null)
        {
            _log.Debug("InsertData");

            if (timer.IsEnabled)
                timer.Stop();

            try
            {
                if(existPatientCases != null)
                {
                    foreach (Patient patient in PatientList)
                    {
                        if (patient.PatientCaseList != null && patient.PatientCaseList.Count > 0)
                        {
                            foreach (PatientCase patientCase in existPatientCases)
                                patient.PatientCaseList.Remove(patient.PatientCaseList.Where(x => x.Id == patientCase.Id).First());
                        }                            
                    }
                }

                Dictionary<string, string> importfiles = new Dictionary<string, string>();

                foreach (Patient patient in PatientList)
                {
                    if (patient.PatientCaseList != null && patient.PatientCaseList.Count > 0)
                    {
                        foreach (PatientCase patientCase in patient.PatientCaseList)
                        {
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
                parameter["patients"] = PatientList;
                parameter["path"] = SelectedDir.Path;
                parameter["annotationFilePath"] = this.annotationFilePath;
                var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter, Constants.FileImportDialogWidth, Constants.FileImportDialogHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Undefined)
                {
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
        }

        private void CheckDrive(object sender, EventArgs e)
        {
            GetDrive();
        }

        private void GetDrive()
        {
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
                        string driveName = d.Name.Replace("\\", "");

                        currExternalDrive[driveName] = driveName;
                        long[] data = { d.TotalSize, d.AvailableFreeSpace };
                        ExternalDriveList.Add(driveName, data);

                        if (isFirstExternalDrive)
                        {
                            firstExternalDrive = driveName;
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

            Tuple<bool, string> result = CommonUtil.Decryptor(path);

            if (result.Item1)
            {
                IList<Patient> patients = new List<Patient>();

                string json = result.Item2;
                if (!String.IsNullOrEmpty(json))
                {
                    JObject obj = JObject.Parse(json);

                    ApproximateImportSize = CommonUtil.ByteToGB(GetLongValue(obj, "Size"));
                    this.annotationFilePath = GetStrValue(obj, "AnnotationFilePath");

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
                            patientCase.NumOfFrames = GetIntValue(caseObj, "NumOfFrames");
                            patientCase.Image = GetStrValue(caseObj, "Image");
                            patientCase.PullbackType = GetStrValue(caseObj, "PullbackType");
                            patientCase.PullbackLength = GetStrValue(caseObj, "PullbackLength");
                            patientCase.AngioYn = GetBoolValue(caseObj, "AngioYn");
                            patientCase.AngioCoRegistration = GetBoolValue(caseObj, "AngioCoRegistration");
                            patientCase.IndicatorDegree = GetDoubleValue(caseObj, "IndicatorDegree");
                            patientCase.PresetName = GetStrValue(caseObj, "PresetName");
                            patientCase.CalciumThreshold = GetIntValue(caseObj, "CalciumThreshold");
                            patientCase.ExpansionCalculation = GetStrValue(caseObj, "ExpansionCalculation");
                            patientCase.ExpansionThreshold = GetIntValue(caseObj, "ExpansionThreshold");
                            patientCase.AppositionThreshold = GetDoubleValue(caseObj, "AppositionThreshold");
                            patientCase.Brightness = GetIntValue(caseObj, "Brightness");
                            patientCase.Contrast = GetIntValue(caseObj, "Contrast");
                            patientCase.SectionProximal = GetIntValue(caseObj, "SectionProximal");
                            patientCase.SectionDistal = GetIntValue(caseObj, "SectionDistal");
                            patientCase.Bookmark = GetStrValue(caseObj, "Bookmark");
                            patientCase.Longitude = GetStrValue(caseObj, "Longitude");
                            patientCase.CrossSection = GetStrValue(caseObj, "CrossSection");
                            patientCase.StrLumenContour = GetStrValue(caseObj, "StrLumenContour");
                            patientCase.StrLumenSidebranch = GetStrValue(caseObj, "StrLumenSidebranch");
                            patientCase.StrLumenStent = GetStrValue(caseObj, "StrLumenStent");
                            patientCase.StrLumenGuidewire = GetStrValue(caseObj, "StrLumenGuidewire");
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
    }
}
