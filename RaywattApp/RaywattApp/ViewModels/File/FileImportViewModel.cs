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
using Newtonsoft.Json;

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

                if (_diskType.Equals(Constants.FileDiskCd))
                {
                    if (timer.IsEnabled)
                        timer.Stop();

                    //TO-DO : CD 기능 구현 필요
                    curPath = "C:\\DataSave\\";//TEST용 경로
                    directoryProvider.GetDirectoryWithExtension(curPath);
                    DirItems = directoryProvider.DirItems;
                }
                else
                {
                    GetDrive();
                    timer.Start();

                    if (!curPath.Equals(SelectedExternalDrive) && SelectedExternalDrive != null)
                    {
                        directoryProvider.GetDirectoryWithExtension(SelectedExternalDrive);
                        DirItems = directoryProvider.DirItems;
                    }
                    else if(SelectedExternalDrive == null)
                    {
                        DirItems = null;
                    }
                }

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
                if (_selectedExternalDrive != value)
                {
                    _selectedExternalDrive = value;

                    if (_selectedExternalDrive != null)
                    {
                        curPath = _selectedExternalDrive;
                        directoryProvider.GetDirectoryWithExtension(curPath);
                        DirItems = directoryProvider.DirItems;
                    }

                    OnPropertyChanged(nameof(SelectedExternalDrive));
                }
            }
        }

        [ObservableProperty]
        private Dictionary<string, object> _externalDriveList;

        private DirectoryProvider directoryProvider;

        private ObservableCollection<Item> _dirItems;
        public ObservableCollection<Item> DirItems
        {
            get { return _dirItems; }
            set
            {
                _dirItems = value;
                OnPropertyChanged(nameof(DirItems));
            }
        }

        private DirectoryItem _selectedDir;
        public DirectoryItem SelectedDir
        {
            get { return _selectedDir; }
            set 
            { 
                _selectedDir = value;
                ReadFile(_selectedDir.Path);
                OnPropertyChanged(nameof(SelectedDir));
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

            Dictionary<string, string> importfiles = new Dictionary<string, string>();
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
                        if(existPatientCases != null)
                        {
                            bool exist = false;
                            foreach(PatientCase pc in existPatientCases)
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

                        //image
                        string srcPath = CommonUtil.GetDirectoryPath(SelectedDir.Path) + "\\" + patientCase.Image;
                        if (System.IO.File.Exists(srcPath))
                        {
                            string destPath = CommonUtil.CreateFolder(Constants.DataRootPath + "\\" + patientCase.PatientId) + "\\" + patientCase.Image;

                            importfiles.Add(srcPath, destPath);
                            sqlParameters["image"] = destPath;
                        }
                        else
                        {
                            sqlParameters["image"] = "";
                        }

                        _sqlManager.UpsertPatientCase(sqlParameters);
                    }
                }
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["File Import"];
            parameter["files"] = importfiles;
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter);
            Close();
        }

        private void SetCondition()
        {
            //TO-DO 설치할 때, 설치하는 경로의 드라이브 정보 가져오도록 처리 필요
            string configDrive = "C:\\";

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
                DiskType = Constants.FileDiskCd;
        }

        private void CheckDrive(object sender, EventArgs e)
        {
            GetDrive();
        }

        private void GetDrive()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            Dictionary<string, string> currExternalDrive = new Dictionary<string, string>();

            string firstExternalDrive = "";
            bool isFirstExternalDrive = true;

            ExternalDriveList.Clear();

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    if (d.DriveType == DriveType.CDRom)
                    {
                        //TO-DO : CD 기능 구현 필요
                    }
                    else if (d.DriveType == DriveType.Removable)
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

            if(currExternalDrive.Count == 0)
            {
                ExternalDriveComboBox.Clear();
                SelectedExternalDrive = null;
                IsEnableExternalDrive = false;
            }
            else if (ExternalDriveComboBox.Count != currExternalDrive.Count)
            {
                ExternalDriveComboBox = currExternalDrive;
                SelectedExternalDrive = firstExternalDrive;
                IsEnableExternalDrive = true;
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
    }
}
