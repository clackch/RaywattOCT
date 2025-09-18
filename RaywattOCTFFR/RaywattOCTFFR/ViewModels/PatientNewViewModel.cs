using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using log4net;
using System.Windows.Navigation;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Util;
using System.ComponentModel;
using RaywattOCTFFR.Views.Dialog;

namespace RaywattOCTFFR.ViewModels
{
    public partial class PatientNewViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientNewViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private Dictionary<string, string> _genderComboBox;

        [ObservableProperty]
        private string _selectedGender;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _newRecordingCommand;
        public ICommand NewRecordingCommand
        {
            get { return this._newRecordingCommand ?? (this._newRecordingCommand = new RelayCommand(NewRecording, CanNewRecording)); }
        }

        private ICommand _selectPhysicianCommand;
        public ICommand SelectPhysicianCommand
        {
            get { return this._selectPhysicianCommand ?? (this._selectPhysicianCommand = new RelayCommand(SelectPhysician)); }
        }

        public PatientNewViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientNewViewModel");

            Constants.CurrentPage = Constants.PatientNewPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Patient = new Patient();
            Patient.Id = "";
            Patient.Lastname = "";
            Patient.Firstname = "";
            Patient.Birthdate = null;
            GenderComboBox = CodeDefinition.Codes["GEND"];

            Patient.PropertyChanged += Patient_PropertyChanged;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                PrevStatus = (PrevStatus)data["prevStatus"];
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage) { Parameter = parameter });
        }

        private bool CanNewRecording()
        {
            _log.Debug("CanNewRecording");

            return Validate();
        }

        private void Patient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _log.Debug("Patient_PropertyChanged");

            (NewRecordingCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void NewRecording()
        {
            _log.Debug("NewRecording");

            //Check ID for Duplication
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id.Trim();

            int nCnt = _sqlManager.CountPatient(sqlParameters);

            if(nCnt > 0)
            {
                Patient.ValidateId = _l10n["ID is duplicated"];

                return;
            }

            //Save New Patient Info
            sqlParameters.Clear();
            sqlParameters["id"] = Patient.Id = Patient.Id.Trim();
            sqlParameters["lastname"] = Patient.Lastname = Patient.Lastname.Trim();
            sqlParameters["firstname"] = Patient.Firstname = Patient.Firstname.Trim();
            sqlParameters["birthdate"] = Patient.Birthdate;
            if (SelectedGender != null)
            {
                sqlParameters["gender"] = SelectedGender;
                Patient.Gender = SelectedGender;
            }
            else
            {
                sqlParameters["gender"] = "";
            }
            sqlParameters["physician_id"] = Patient.PhysicianId;

            int nRows = _sqlManager.InsertPatient(sqlParameters);

            if(nRows == 1)
            {
                CommonUtil.CreateFolder(Constants.DataRootPath + "\\" + Patient.Id);

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                SetListStatusInit();
                parameter["prevStatus"] = PrevStatus;

                Dictionary<string, object> popupParameter = new Dictionary<string, object>();

                if (CommonUtil.IsStorageAvailable())                    
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingPresetPage) { Parameter = parameter });
                else
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
            }
            else
            {
                _log.Error("Insert Error");
            }
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

        private bool Validate()
        {
            _log.Debug("Validate");

            if (string.IsNullOrEmpty(Patient.Id.Trim()))
                return false;

            if (string.IsNullOrEmpty(Patient.Lastname.Trim()))
                return false;

            if (string.IsNullOrEmpty(Patient.Firstname.Trim()))
                return false;

            if (Patient.PhysicianId == 0)
                return false;

            return true;
        }

        private void SelectPhysician()
        {
            _log.Debug("SelectPhysician");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedPhysicianId"] = Patient.PhysicianId;

            var result = _dialogService.OpenDialog(new PhysicianDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                Physician physician = (Physician)data["selectedPhysician"];
                Patient.PhysicianId = physician.Id;
                Patient.PhysicianName = physician.Name;
            }
        }
    }
}
