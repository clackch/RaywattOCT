using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class PatientEditViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientEditViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private Patient _patientEdit;

        [ObservableProperty]
        private string _genderCodeEdit;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _patiendEditSaveCommand;
        public ICommand PatientEditSaveCommand
        {
            get { return this._patiendEditSaveCommand ?? (this._patiendEditSaveCommand = new RelayCommand(SavePatientEdit, CanSavePatient)); }
        }

        public PatientEditViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientEditViewModel");

            Constants.CurrentPage = Constants.PatientEditPage;

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
                Patient = (Patient)data["patient"];
                PrevStatus = (PrevStatus)data["prevStatus"];

                PatientEdit = new Patient();
                CopyPatient(Patient, PatientEdit);

                GenderCodeEdit = Patient.Gender;

                PatientEdit.PropertyChanged += PatientEdit_PropertyChanged;
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
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
        }

        private void SavePatientEdit()
        {
            _log.Debug("SavePatientEdit");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();

            if (!Patient.Id.Equals(PatientEdit.Id.Trim()))
            {
                //Check ID for Duplication
                sqlParameters["id"] = PatientEdit.Id.Trim();

                int nCnt = _sqlManager.CountPatient(sqlParameters);

                if (nCnt > 0)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["ID is duplicated."];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);

                    return;
                }
            }

            //Save New Patient Info
            sqlParameters.Clear();
            sqlParameters["originId"] = Patient.Id.Trim();
            sqlParameters["id"] = PatientEdit.Id = PatientEdit.Id.Trim();
            sqlParameters["lastname"] = PatientEdit.Lastname = PatientEdit.Lastname.Trim();
            sqlParameters["firstname"] = PatientEdit.Firstname = PatientEdit.Firstname.Trim();
            sqlParameters["birthdate"] = PatientEdit.Birthdate;
            if (GenderCodeEdit != null)
            {
                sqlParameters["gender"] = GenderCodeEdit;
                PatientEdit.Gender = CodeDefinition.Codes["GEND"][GenderCodeEdit];
            }
            else
            {
                sqlParameters["gender"] = "";
            }

            int nRows = _sqlManager.UpdatePatient(sqlParameters);

            if (nRows == 1)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = PatientEdit;
                SetDetailStatusInit();
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
            }
        }

        private void SetDetailStatusInit()
        {
            PrevStatus.DetailSelectedGroup = null;
            PrevStatus.DetailPageOffset = 0;
            PrevStatus.DetailPageGroup = 1;
            PrevStatus.DetailPageNumber = 0;
        }

        private bool CanSavePatient()
        {
            _log.Debug("CanNewRecording");

            return ValidatePatient();
        }

        private void PatientEdit_PropertyChanged(object sender, EventArgs e)
        {
            _log.Debug("PatientEdit_PropertyChanged");

            (PatientEditSaveCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private bool ValidatePatient()
        {
            _log.Debug("ValidatePatient");

            if (string.IsNullOrEmpty(PatientEdit.Id.Trim()))
                return false;

            if (string.IsNullOrEmpty(PatientEdit.Lastname.Trim()))
                return false;

            if (string.IsNullOrEmpty(PatientEdit.Firstname.Trim()))
                return false;

            return true;
        }

        private void CopyPatient(Patient src, Patient dest)
        {
            _log.Debug("CopyPatient");

            dest.Id = src.Id.Trim();
            dest.Lastname = src.Lastname.Trim();
            dest.Firstname = src.Firstname.Trim();
            dest.Birthdate = src.Birthdate;
            dest.Gender = src.Gender;
        }
    }
}
