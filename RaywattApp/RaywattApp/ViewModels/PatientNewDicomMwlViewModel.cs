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
    public partial class PatientNewDicomMwlViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientNewDicomMwlViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ObservableCollection<DicomWorklist> _worklists = new ObservableCollection<DicomWorklist>();

        [ObservableProperty]
        private DicomWorklist _selectedWorklist;

        [ObservableProperty]
        private Patient _selectedPatient;

        [ObservableProperty]
        private DicomServer _selectedDicomServer;

        [ObservableProperty]
        private string _localHostAeTitle;

        [ObservableProperty]
        private bool _isChecking = false;

        [ObservableProperty]
        private TextValidator _searchPatientId = new TextValidator();

        [ObservableProperty]
        private DateTime? _spsStartDateFrom;

        [ObservableProperty]
        private DateTime? _spsStartDateTo;

        [ObservableProperty]
        private string _spsMsg;

        private IntPtr dicomClient;
        private IntPtr dicomWorklists;

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
            get { return this._selectionChangedCommand ?? (this._selectionChangedCommand = new RelayCommand<DicomWorklist>(SelectionChanged)); }
        }

        public PatientNewDicomMwlViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientNewDicomMwlViewModel");

            Constants.CurrentPage = Constants.PatientNewDicomMwlPage;

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
            SelectedPatient = new Patient();

            SelectedPatient.Id = SelectedWorklist.PatientId;
            SelectedPatient.Birthdate = SelectedWorklist.PatientBirthDate;
            SelectedPatient.Gender = SelectedWorklist.PatientSex;

            string lastname, firstname;
            CommonUtil.ParseDicomName(SelectedWorklist.PatientName, out lastname, out firstname);
            SelectedPatient.Lastname = lastname;
            SelectedPatient.Firstname = firstname;
            SelectedPatient.HasFirstname = (firstname != string.Empty);

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
            bool needPhysician = true;
            if (!Validate(out isExist, out needPhysician))
                return;

            if (needPhysician && !SelectPhysician())
                return;

            if (!Save(isExist))
                return;

            CommonUtil.CreateFolder(Constants.DataRootPath + "\\" + SelectedPatient.Id);

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = SelectedPatient;
            SetListStatusInit();
            parameter["prevStatus"] = PrevStatus;

            if (CommonUtil.IsStorageAvailable())
            {
                parameter["accessionNumber"] = SelectedWorklist.AccessionNumber;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingPresetPage) { Parameter = parameter });
            }
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

        private bool Validate(out bool isExist, out bool needPhysician)
        {
            needPhysician = true;
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = SelectedPatient.Id;

            IList<Patient> Patients = _sqlManager.SelectPatient(sqlParameters);

            if (Patients.Count > 0)
            {
                isExist = true;

                Patient patient = Patients[0];
                sqlParameters["id"] = patient.PhysicianId;
                IList<Physician> Physicians = _sqlManager.SelectPhysician(sqlParameters);

                if (Physicians.Count > 0)
                {
                    needPhysician = false;
                    SelectedPatient.PhysicianId = Physicians[0].Id;
                    SelectedPatient.PhysicianName = Physicians[0].Name;
                }

                if (patient.Lastname == SelectedPatient.Lastname && 
                    patient.Firstname == SelectedPatient.Firstname && 
                    patient.Birthdate == SelectedPatient.Birthdate && 
                    patient.Gender == SelectedPatient.Gender)
                {
                    return true;
                }

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

            Worklists.Clear();

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

            if (SpsStartDateFrom?.Date > SpsStartDateTo?.Date)
            {
                SpsMsg = _l10n["$MSG025"].ToString();
                return;
            }

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

            string searchPatientName = SearchPatientId.Text.Trim();
            string searchSpsStartDateFrom = SpsStartDateFrom.HasValue ? SpsStartDateFrom.Value.ToString("yyyyMMdd") : "00010101";
            string searchSpsStartDateTo = SpsStartDateTo.HasValue ? SpsStartDateTo.Value.ToString("yyyyMMdd") : "99991231";

            dicomWorklists = await Task.Run(() => RayExportWrapper.FindWorklist(dicomClient, searchPatientName, "*" /* PatientName */, "*" /* AccessionNumber */, "OCT", "*" /* ScheduledStationAe */, searchSpsStartDateFrom, searchSpsStartDateTo, "*" /* ProcedureId */, out count));
            _log.Debug($"FindWorklist : PatientId = {searchPatientName}, SpsStartDate = {searchSpsStartDateFrom}-{searchSpsStartDateTo}, ResultCount = {count}");
            IsChecking = false;

            if (dicomWorklists != IntPtr.Zero)
            {
                IntPtr current = dicomWorklists;

                for (int i = 0; i < count; i++)
                {
                    var dicomWorklist = Marshal.PtrToStructure<DicomWorklist>(current);

                    _log.Debug($"[{i}] PatientID: [{dicomWorklist.PatientId}]");
                    _log.Debug($"[{i}] PatientName: [{dicomWorklist.PatientName}]");
                    _log.Debug($"[{i}] PatientSex: [{dicomWorklist.PatientSex}]");
                    _log.Debug($"[{i}] PatientBirthDate: [{dicomWorklist.PatientBirthDate}]");
                    _log.Debug($"[{i}] PatientBirthTime: [{dicomWorklist.PatientBirthTime}]");
                    _log.Debug($"[{i}] PatientAge: [{dicomWorklist.PatientAge}]");
                    _log.Debug($"[{i}] AccessionNumber: [{dicomWorklist.AccessionNumber}]");
                    _log.Debug($"[{i}] Modality: [{dicomWorklist.Modality}]");
                    _log.Debug($"[{i}] ReferencedSOPClassUID: [{dicomWorklist.ReferencedSOPClassUID}]");
                    _log.Debug($"[{i}] ReferencedSOPInstanceUID: [{dicomWorklist.ReferencedSOPInstanceUID}]");
                    _log.Debug($"[{i}] IssuerOfPatientID: [{dicomWorklist.IssuerOfPatientID}]");
                    _log.Debug($"[{i}] TypeOfPatientID: [{dicomWorklist.TypeOfPatientID}]");
                    _log.Debug($"[{i}] PatientComments: [{dicomWorklist.PatientComments}]");

                    Worklists.Add(dicomWorklist);

                    current += Marshal.SizeOf<DicomWorklist>();
                }
            }
            RayExportWrapper.FreeMemory(dicomWorklists);
        }

        private bool CanNext()
        {
            _log.Debug("CanNext");

            return SelectedWorklist == null ? false : true;
        }

        private void SelectionChanged(DicomWorklist worklist)
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

        partial void OnSpsStartDateFromChanged(DateTime? value)
        {
            if (SpsStartDateFrom?.Date <= SpsStartDateTo?.Date)
                SpsMsg = "";
        }
    }
}
