using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using Newtonsoft.Json.Linq;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.File;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
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

        private string annotationFilePath;

        private string externalDrive;

        private bool externDriveInit;

        private DirectoryProvider directoryProvider;

        [ObservableProperty]
        private string _importType = Constants.ImportTypeDicom;

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

        public FileImportStep1ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportStep1ViewModel");

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

        private void SelectImportType(string type)
        {
            _log.Debug("SelectImportType: " + type);

            ImportType = type;
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
            else if (ExternalDriveComboBox.Count == 0)
            {
                DirItems = null;
            }
        }

        private void ReadFile(string path)
        {
            _log.Debug("ReadFile : " + path);

            //DICOM, TIFF

            //RAW

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
                List<Patient> patients = new List<Patient>();

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

                _log.Error("File Decrypt Error");
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
                PatientCaseList = patient.PatientCaseList;
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
