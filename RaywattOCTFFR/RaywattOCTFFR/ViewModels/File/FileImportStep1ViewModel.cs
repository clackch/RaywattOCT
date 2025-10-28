using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using Newtonsoft.Json.Linq;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.File;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep1ViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep1ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        private string curPath;

        private string externalDrive;

        private DirectoryProvider directoryProvider;

        [ObservableProperty]
        private FileImport _fileImport = new FileImport();

        [ObservableProperty]
        private string? _importType;

        [ObservableProperty]
        private IList<Patient>? _patientList;

        [ObservableProperty]
        private IList<PatientCase>? _patientCaseList;

        [ObservableProperty]
        private double _availableSpace;

        [ObservableProperty]
        private double _approximateImportSize;

        [ObservableProperty]
        private Dictionary<string, string> _externalDriveComboBox;

        [ObservableProperty]
        private bool _isEnableExternalDrive;

        [ObservableProperty]
        private bool _isSingleColumnMode = true;

        private string _selectedExternalDrive;
        public string SelectedExternalDrive
        {
            get { return _selectedExternalDrive; }
            set
            {
                if (_selectedExternalDrive != value)
                {
                    _selectedExternalDrive = value;
                    externalDrive = _selectedExternalDrive;

                    if (_selectedExternalDrive != null && ImportType != null)
                    {
                        curPath = _selectedExternalDrive;
                        directoryProvider.GetDirectoryWithExtension(curPath.Replace("\\", ""), CommonUtil.GetFileExtension(ImportType));
                        DirItems = directoryProvider.DirItems;
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
                if (value != null)
                {
                    _selectedDir = value;
                    ReadFile(_selectedDir.Path);
                    OnPropertyChanged(nameof(SelectedDir));
                }
            }
        }

        private ICommand _selectImportTypeCommand;

        public ICommand SelectImportTypeCommand
        {
            get { return this._selectImportTypeCommand ?? (this._selectImportTypeCommand = new RelayCommand<string>(SelectImportType)); }
        }

        private ICommand _showCaseCommand;
        public ICommand ShowCaseCommand
        {
            get { return this._showCaseCommand ?? (this._showCaseCommand = new RelayCommand<Patient>(ShowCase)); }
        }

        private ICommand _selectCaseCommand;
        public ICommand SelectCaseCommand
        {
            get { return this._selectCaseCommand ?? (this._selectCaseCommand = new RelayCommand<PatientCase>(SelectCase)); }
        }

        public FileImportStep1ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportStep1ViewModel");

            Constants.CurrentPage = Constants.FileImportStep1Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            directoryProvider = new();
            ExternalDriveComboBox = new Dictionary<string, string>();
            ExternalDriveList = new Dictionary<string, object>();

            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(CheckDrive);
            timer.Start();

            ImportType = Constants.ImportTypeDicom;

            GetDrive();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                if (data.TryGetValue("fileImport", out var fileImportObj) && fileImportObj is FileImport fileImportData)
                {
                    FileImport = fileImportData;
                    SelectImportType(FileImport.PatientCase.ImportType);
                }
            }

            SetCondition();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            if (timer.IsEnabled)
                timer.Stop();
        }

        protected override void Cancel()
        {
            _log.Debug("Cancel");

            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
        }

        protected override void Next()
        {
            _log.Debug("Next");

            bool canNext = true;

            if(SelectedDir == null || !SelectedDir.IsFile)
            {
                canNext = false;
            }
            else if (Constants.ImportTypeRaw.Equals(ImportType) && (FileImport.Patient == null || FileImport.PatientCase == null))
            {
                canNext = false;                
            }

            if (canNext)
            {
                FileImport.FilePath = SelectedDir.Path;
                if (FileImport.Patient == null)
                    FileImport.Patient = new();
                if (FileImport.PatientCase == null)
                    FileImport.PatientCase = new();
                FileImport.PatientCase.ImportType = ImportType;

                if(!string.IsNullOrEmpty(CommonUtil.CheckFile(FileImport.FilePath, FileImport.PatientCase.Image)))
                {
                    Dictionary<string, Object> parameter = new Dictionary<string, Object>();
                    parameter["fileImport"] = FileImport;
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.FileImportStep2Page) { Parameter = parameter });
                }
                else
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["No image file found."];
                    var result2 = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                }
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["No items have been selected"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            }
        }

        private void SelectImportType(string type)
        {
            _log.Debug("SelectImportType: " + type);

            if (ImportType != null && ImportType.Equals(type))
                return;

            ImportType = type;

            ApproximateImportSize = 0;
            PatientList = null;
            PatientCaseList = null;
            FileImport.Patient = null;
            FileImport.PatientCase = null;

            if (Constants.ImportTypeRaw.Equals(ImportType))
            {
                IsSingleColumnMode = false;
            }
            else
            {
                IsSingleColumnMode = true;
            }

            if (SelectedExternalDrive != null)
            {
                directoryProvider.GetDirectoryWithExtension(SelectedExternalDrive.Replace("\\", ""), CommonUtil.GetFileExtension(ImportType));
                DirItems = directoryProvider.DirItems;
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

            try
            {
                var searcher = new ManagementObjectSearcher(@"Select * From Win32_DiskDrive");

                foreach (var drive in searcher.Get())
                {
                    var mediaType = drive["MediaType"]?.ToString();
                    var interfaceType = drive["InterfaceType"]?.ToString();

                    if (interfaceType == "USB" || mediaType == "Removable Media" || mediaType == "External hard disk media")
                    {
                        //디스크 드라이브에 있는 모든 파티션 반환
                        var partitionsQuery = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{drive["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                        foreach (var partition in partitionsQuery.Get())
                        {
                            //각 파티션에 부여된 드라이브 이름 반환 (C, D, E)
                            var logicalDisksQuery = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                            foreach (var logicalDisk in logicalDisksQuery.Get())
                            {
                                var d = new DriveInfo(logicalDisk["Name"].ToString());
                                if (d.IsReady)
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
                    }
                }
            }
            catch(Exception e)
            {
                _log.Error("External Drive Disconnected: " + e.Message);
            }
            finally
            {
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

                if (ExternalDriveComboBox.Count == 0)
                {
                    DirItems = null;
                }
            }
        }

        private void ReadFile(string path)
        {
            _log.Debug("ReadFile : " + path);

            if (Constants.ImportTypeRaw.Equals(ImportType))
            {
                if (!path.ToLower().EndsWith(Constants.FileExtension))
                {
                    PatientList = null;
                    PatientCaseList = null;
                    return;
                }

                Tuple<bool, string> result = CommonUtil.Decryptor(path);

                if (result.Item1)
                {
                    List<Patient> patients = new List<Patient>();

                    string json = result.Item2;
                    if (!String.IsNullOrEmpty(json))
                    {
                        JObject obj = JObject.Parse(json);
                        JArray patientArray = JArray.Parse(GetStrValue(obj, "PatientList"));
                        foreach (JObject patientObj in patientArray)
                        {
                            Patient patient = new Patient();
                            patient.Id = GetStrValue(patientObj, "Id");
                            patient.Lastname = GetStrValue(patientObj, "Lastname");
                            patient.Firstname = GetStrValue(patientObj, "Firstname");
                            patient.Name = patient.Firstname + ", " + patient.Lastname;
                            patient.Birthdate = GetDateValueNullable(patientObj, "Birthdate");
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
                                patientCase.Comment = GetStrValue(caseObj, "Comment");
                                patientCase.Vessel = GetStrValue(caseObj, "Vessel");
                                patientCase.Location = GetStrValue(caseObj, "Location");
                                patientCase.Procedure = GetStrValue(caseObj, "Procedure");
                                patientCase.NumOfFrames = GetIntValue(caseObj, "NumOfFrames");
                                patientCase.Image = GetStrValue(caseObj, "Image");
                                patientCase.ImageResolution = GetDoubleValue(caseObj, "ImageResolution");
                                patientCase.ImageSize = GetLongValue(caseObj, "ImageSize");
                                patientCase.ZOffset = GetIntValue(caseObj, "ZOffset");
                                patientCase.FieldOfView = GetDoubleValue(caseObj, "FieldOfView");
                                patientCase.PullbackType = GetStrValue(caseObj, "PullbackType");
                                patientCase.PullbackLength = GetStrValue(caseObj, "PullbackLength");
                                patientCase.IndicatorDegree = GetDoubleValue(caseObj, "IndicatorDegree");
                                patientCase.FlushMedia = GetStrValue(caseObj, "FlushMedia");
                                patientCase.PullbackTrigger = GetStrValue(caseObj, "PullbackTrigger");
                                patientCase.Colormap = GetStrValue(caseObj, "Colormap");
                                patientCase.CalciumThreshold = GetIntValue(caseObj, "CalciumThreshold");
                                patientCase.ExpansionCalculation = GetStrValue(caseObj, "ExpansionCalculation");
                                patientCase.ExpansionThreshold = GetIntValue(caseObj, "ExpansionThreshold");
                                patientCase.AppositionThreshold = GetDoubleValue(caseObj, "AppositionThreshold");
                                patientCase.Brightness = GetIntValue(caseObj, "Brightness");
                                patientCase.Contrast = GetIntValue(caseObj, "Contrast");
                                patientCase.SheathDiameter = GetDoubleValue(caseObj, "SheathDiameter");
                                patientCase.SectionProximal = GetIntValue(caseObj, "SectionProximal");
                                patientCase.SectionDistal = GetIntValue(caseObj, "SectionDistal");
                                patientCase.Bookmark = GetStrValue(caseObj, "Bookmark");
                                patientCase.Longitude = GetStrValue(caseObj, "Longitude");
                                patientCase.CrossSection = GetStrValue(caseObj, "CrossSection");
                                patientCase.StrLumenContour = GetStrValue(caseObj, "StrLumenContour");
                                patientCase.StrLumenSidebranch = GetStrValue(caseObj, "StrLumenSidebranch");
                                patientCase.StrLumenStent = GetStrValue(caseObj, "StrLumenStent");
                                patientCase.StrLumenGuidewire = GetStrValue(caseObj, "StrLumenGuidewire");
                                patientCase.FfrPlaque = GetStrValue(caseObj, "FfrPlaque");
                                patientCase.StrCoRegistration = GetStrValue(caseObj, "StrCoRegistration");
                                patientCase.CreateDate = GetDateValue(caseObj, "CreateDate");
                                patientCase.UpdateDate = GetDateValue(caseObj, "UpdateDate");
                                patientCase.GuidewireRadius = GetDoubleValue(caseObj, "GuidewireRadius");

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
                    FileImport.Patient = null;
                    FileImport.PatientCase = null;

                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["The file format is invalid."];
                    var pupupResult = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                }
            }
            else
            {
                ApproximateImportSize = CommonUtil.ByteToGB(CommonUtil.GetFileSize(path));
            }
        }

        private void ShowCase(Patient patient)
        {
            _log.Debug("ShowCase");

            if (patient == null || patient.PatientCaseList == null)
            {
                PatientCaseList = null;
            }
            else
            {
                FileImport.Patient = patient;
                PatientCaseList = patient.PatientCaseList;
            }
        }

        private void SelectCase(PatientCase patientCase)
        {
            _log.Debug("SelectCase");

            if(patientCase == null)
            {
                ApproximateImportSize = 0;
            }
            else
            {
                FileImport.PatientCase = patientCase;
                ApproximateImportSize = CommonUtil.ByteToGB(patientCase.ImageSize);
            }
        }

        private static string GetStrValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return "";

            return obj[key].ToString();
        }

        private static DateTime GetDateValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return DateTime.Now;

            return Convert.ToDateTime(obj[key]);
        }

        private DateTime? GetDateValueNullable(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return DateTime.Now;

            if (String.IsNullOrEmpty(obj[key].ToString()))
                return null;

            if (obj[key].Equals("null"))
                return null;

            return Convert.ToDateTime(obj[key]);
        }

        private static long GetLongValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return 0;

            return (long)obj[key];
        }

        private static double GetDoubleValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return 0;

            return (double)obj[key];
        }

        private static int GetIntValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return 0;

            return (int)obj[key];
        }

        private static bool GetBoolValue(JObject obj, string key)
        {
            if (!obj.ContainsKey(key))
                return false;

            return (bool)obj[key];
        }
    }
}
