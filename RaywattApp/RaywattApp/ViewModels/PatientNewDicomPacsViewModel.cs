using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RayCoreWrapper;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class PatientNewDicomPacsViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientNewDicomPacsViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private TextValidator _searchPatientId = new TextValidator();

        [ObservableProperty]
        private ObservableCollection<Patient> _patients = new ObservableCollection<Patient>();

        [ObservableProperty]
        private Patient _selectedPatient;

        [ObservableProperty]
        private DicomServer _selectedDicomServer;

        [ObservableProperty]
        private string _localHostAeTitle;

        [ObservableProperty]
        private bool _isChecking = false;

        private IntPtr dicomClient;
        private IntPtr dicomPatients;

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _newRecordingCommand;
        public ICommand NewRecordingCommand
        {
            get { return this._newRecordingCommand ?? (this._newRecordingCommand = new RelayCommand(NewRecording, CanNext)); }
        }

        private ICommand _searchCommand;
        public ICommand SearchCommand
        {
            get { return this._searchCommand ?? (this._searchCommand = new RelayCommand(async () => await Search())); }
        }

        private ICommand _selectionChangedCommand;
        public ICommand SelectionChangedCommand
        {
            get { return this._selectionChangedCommand ?? (this._selectionChangedCommand = new RelayCommand<Patient>(SelectionChanged)); }
        }

        public PatientNewDicomPacsViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientNewDicomPacsViewModel");

            Constants.CurrentPage = Constants.PatientNewDicomPacsPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            dicomClient = RayExportWrapper.CreateDcmClient();            
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                PrevStatus = (PrevStatus)data["prevStatus"];
                LocalHostAeTitle = (string)data["localHostAeTitle"];
                SelectedDicomServer = (DicomServer)data["selectedDicomServer"];

                bool usePeerVerification = CommonUtil.IsTestMode(DeviceStatus.TestMode, "CertIgnore") == true ? false : true;
                RayExportWrapper.DicomNetRWError res = (RayExportWrapper.DicomNetRWError)RayExportWrapper.Initialize(dicomClient, LocalHostAeTitle, SelectedDicomServer.IpAddress, int.Parse(SelectedDicomServer.Port), SelectedDicomServer.AeTitle, SelectedDicomServer.TlsYn, usePeerVerification, SelectedDicomServer.CaFilePath);
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            RayExportWrapper.DestroyDcmClient(dicomClient);
            dicomClient = IntPtr.Zero;
        }

        private void Back()
        {
            _log.Debug("Back");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            parameter["selectedDicomServer"] = SelectedDicomServer;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientNewDicomPage) { Parameter = parameter });
        }

        private void NewRecording()
        {
            _log.Debug("NewRecording");

            if (!ValidateSelectedPatient(SelectedPatient.HasFirstname))
            {
                Dictionary<string, object> confirmParam = new Dictionary<string, object>();
                confirmParam["title"] = _l10n["Information"];
                confirmParam["message"] = _l10n["Invalid format for patient information. Enter manually?"];
                var confirmResult = _dialogService.OpenDialog(new ConfirmDialogControl(), confirmParam, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (confirmResult == null || confirmResult.DialogAnswer != DialogResults.Answer.Yes)
                {
                    return;
                }

                Dictionary<string, object> inputParam = new Dictionary<string, object>();
                inputParam["patient"] = SelectedPatient;
                var inputResult = _dialogService.OpenDialog(new PatientInputDialogControl(), inputParam, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (inputResult == null || inputResult.DialogAnswer != DialogResults.Answer.Yes)
                {
                    return;
                }

                Dictionary<string, Object> data = (Dictionary<string, Object>)inputResult.DialogReturn;
                SelectedPatient.Id = data["id"].ToString();
                SelectedPatient.Firstname = data["firstname"].ToString();
                SelectedPatient.Lastname = data["lastname"].ToString();
                SelectedPatient.Birthdate = (DateTime?)data["birthdate"];
                SelectedPatient.Gender = data["gender"].ToString();
            }

            bool isExist = false;
            if (!Validate(out isExist))
                return;

            if (!SelectPhysician())
                return;

            if (!Save(isExist))
                return;

            CommonUtil.CreateFolder(Constants.DataRootPath + "\\" + SelectedPatient.Id);

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = SelectedPatient;
            SetListStatusInit();
            parameter["prevStatus"] = PrevStatus;

            if (CommonUtil.IsStorageAvailable())
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingPresetPage) { Parameter = parameter });
            else
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
        }

        private bool ValidateSelectedPatient(bool hasFirstname)
        {
            if (SelectedPatient.Id == null || SelectedPatient.Lastname == null)
                return false;

            if (hasFirstname && SelectedPatient.Firstname == string.Empty)
                return false;

            return true;
        }

        private bool Validate(out bool isExist)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = SelectedPatient.Id;

            int nCnt = _sqlManager.CountPatient(sqlParameters);

            if (nCnt > 0)
            {
                isExist = true;

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["$MSG026"];
                var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                {
                    return true;
                }
                else
                {
                    return false;
                }                                    
            }
            else
            {
                isExist = false;

                return true;
            }
        }

        private bool SelectPhysician()
        {
            _log.Debug("SelectPhysician");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedPhysicianId"] = SelectedPatient.PhysicianId;

            var result = _dialogService.OpenDialog(new PhysicianDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                Physician physician = (Physician)data["selectedPhysician"];
                SelectedPatient.PhysicianId = physician.Id;
                SelectedPatient.PhysicianName = physician.Name;

                return true;
            }

            return false;
        }

        private bool Save(bool isExist)
        {
            _log.Debug("Save");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = SelectedPatient.Id;
            sqlParameters["lastname"] = SelectedPatient.Lastname;
            sqlParameters["firstname"] = SelectedPatient.Firstname;
            sqlParameters["birthdate"] = SelectedPatient.Birthdate;
            sqlParameters["gender"] = SelectedPatient.Gender;
            sqlParameters["physician_id"] = SelectedPatient.PhysicianId;

            int res = 0;

            if (isExist)
            {
                sqlParameters["originId"] = SelectedPatient.Id;
                res = _sqlManager.UpdatePatient(sqlParameters);
            }
            else
            {                
                res = _sqlManager.InsertPatient(sqlParameters);
            }

            if (res != 1)
            {
                _log.Error(isExist ? "Update Error" : "Insert Error");
                return false;
            }

            return true;
        }

        private async Task Search()
        {
            _log.Debug("Search");

            Patients.Clear();

            if (string.IsNullOrEmpty(SearchPatientId.Text))
            {
                SearchPatientId.Msg = _l10n["Enter ID"].ToString();
                return;
            }

            if (Regex.IsMatch(SearchPatientId.Text, @"[\*\?]"))
            {
                SearchPatientId.Msg = _l10n["Patient ID cannot contain * or ?."].ToString();
                return;
            }

            string patientIdParam = SearchPatientId.Text.Trim();

            IsChecking = true;
            RayExportWrapper.DicomNetRWError res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Echo(dicomClient));
            _log.DebugFormat("Echo : {0}", res);
            IsChecking = false;

            if (res == RayExportWrapper.DicomNetRWError.NoConnection || res == RayExportWrapper.DicomNetRWError.EchoFail)
            {
                IsChecking = true;
                bool usePeerVerification = CommonUtil.IsTestMode(DeviceStatus.TestMode, "CertIgnore") == true ? false : true;
                res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Initialize(dicomClient, LocalHostAeTitle, SelectedDicomServer.IpAddress, int.Parse(SelectedDicomServer.Port), SelectedDicomServer.AeTitle, SelectedDicomServer.TlsYn, usePeerVerification, SelectedDicomServer.CaFilePath));
                _log.DebugFormat("Initialize : {0}", res);
                IsChecking = false;

                if (res != RayExportWrapper.DicomNetRWError.Normal)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = CommonUtil.GetDicomResultMessage(res);
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    return;
                }
            }

            int count = 0;
            IsChecking = true;
            dicomPatients = await Task.Run(() => (RayExportWrapper.FindPatients(dicomClient, patientIdParam, "*" /* PatientName */, out count)));
            _log.Debug($"FindPatient : PatientId = {patientIdParam}, ResultCount = {count}");
            IsChecking = false;

            if (dicomPatients != IntPtr.Zero)
            {
                IntPtr current = dicomPatients;

                for (int i = 0; i < count; i++)
                {
                    var dicomPatient = Marshal.PtrToStructure<DicomPatient>(current);

                    Patient patient = new Patient();
                    patient.Id = dicomPatient.PatientId;
                    string lastname, firstname;
                    CommonUtil.ParseDicomName(dicomPatient.PatientName, out lastname, out firstname);
                    patient.Lastname = lastname;
                    patient.Firstname = firstname;
                    patient.HasFirstname = firstname != string.Empty ? true : false;
                    patient.Name = dicomPatient.PatientName;
                    patient.Gender = dicomPatient.PatientSex;
                    DateTime birthdate;
                    if (DateTime.TryParseExact(dicomPatient.PatientBirthDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out birthdate))
                    {
                        patient.Birthdate = birthdate;
                    }
                    else
                    {
                        patient.Birthdate = null;
                    }
                    Patients.Add(patient);
                    current += Marshal.SizeOf<DicomPatient>();
                }
            }
            RayExportWrapper.FreeMemory(dicomPatients);
        }

        private bool CanNext()
        {
            _log.Debug("CanNext");

            return SelectedPatient == null ? false : true;
        }

        private void SelectionChanged(Patient patient)
        {
            _log.Debug("SelectionChanged");

            (NewRecordingCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void SetListStatusInit()
        {
            PrevStatus.ListKeyword = "";
            PrevStatus.ListSortField = "LastCase";
            PrevStatus.ListSortDirection = false;
            PrevStatus.ListSort = "last_case DESC";
            PrevStatus.ListPageOffset = 0;
            PrevStatus.ListPageSize = 10;
            PrevStatus.ListPageGroup = 1;
            PrevStatus.ListPageNumber = 0;
        }
    }
}
