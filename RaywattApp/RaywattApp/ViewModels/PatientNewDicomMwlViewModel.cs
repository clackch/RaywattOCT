using System.Collections.Generic;
using System.Windows.Navigation;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using RaywattApp.Views.Dialog;
using RaywattApp.Common.Util;
using System.Collections.ObjectModel;

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
            get { return this._searchCommand ?? (this._searchCommand = new RelayCommand(Search)); }
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
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
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
                Dictionary<string, object> param = new Dictionary<string, object>();
                param["title"] = _l10n["Information"];
                param["message"] = _l10n["Invalid format for patient information."];
                _dialogService.OpenDialog(new ConfirmDialogControl(), param, Constants.ApplicationWidth, Constants.ApplicationHeight);
                return;
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
            {
                parameter["accessionNumber"] = SelectedWorklist.AccessionNumber;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingPresetPage) { Parameter = parameter });
            }
            else
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
        }

        private bool ValidateSelectedPatient(bool hasFirstname)
        {
            if (SelectedPatient.Id == null || SelectedPatient.Birthdate == null || SelectedPatient.Gender == null || SelectedPatient.Lastname == null)
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

        private void Search()
        {
            _log.Debug("Search");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["localHostAeTitle"] = LocalHostAeTitle;
            parameter["dicomServer"] = SelectedDicomServer;
            parameter["deviceStatus"] = DeviceStatus;

            var result = _dialogService.OpenDialog(new MwlSearchDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            
            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                List<DicomWorklist> worklists = (List<DicomWorklist>)result.DialogReturn;

                Worklists.Clear();
                foreach (var worklist in worklists)
                {
                    Worklists.Add(worklist);
                }
            }
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
    }
}
