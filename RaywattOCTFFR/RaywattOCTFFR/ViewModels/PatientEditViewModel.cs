using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattOCTFFR.ViewModels
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
        private Dictionary<string, string> _genderComboBox;

        [ObservableProperty]
        private string _selectedGender;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get { return this._deleteCommand ?? (this._deleteCommand = new RelayCommand(Delete)); }
        }

        private ICommand _patiendEditSaveCommand;
        public ICommand PatientEditSaveCommand
        {
            get { return this._patiendEditSaveCommand ?? (this._patiendEditSaveCommand = new RelayCommand(SavePatientEdit, CanSavePatient)); }
        }

        private ICommand _selectPhysicianCommand;
        public ICommand SelectPhysicianCommand
        {
            get { return this._selectPhysicianCommand ?? (this._selectPhysicianCommand = new RelayCommand(SelectPhysician)); }
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

                SelectedGender = Patient.Gender;
                GenderComboBox = new Dictionary<string, string>();
                foreach (var gender in CodeDefinition.Codes["GEND"])
                {
                    GenderComboBox.Add(gender.Key, _l10n[gender.Value]);
                }

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
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
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
                    PatientEdit.ValidateId = _l10n["ID is duplicated"];

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
            if (!String.IsNullOrEmpty(SelectedGender))
            {
                sqlParameters["gender"] = SelectedGender;
                PatientEdit.Gender = SelectedGender;
            }
            else
            {
                sqlParameters["gender"] = "";
            }
            sqlParameters["physician_id"] = PatientEdit.PhysicianId;

            int nRows = _sqlManager.UpdatePatient(sqlParameters);

            if (nRows == 1)
            {
                _sqlManager.UpdatePatientCaseId(sqlParameters);

                CommonUtil.RenameFolder(Constants.DataRootPath + "\\" + Patient.Id.Trim(), Constants.DataRootPath + "\\" + PatientEdit.Id);

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                PatientEdit.Name = PatientEdit.Firstname + ", " + PatientEdit.Lastname;
                parameter["patient"] = PatientEdit;
                SetDetailStatusInit();
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
            }
            else
            {
                _log.Error("Update Error");
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
            _log.Debug("CanSavePatient");

            return ValidatePatient();
        }

        private void PatientEdit_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

            if (PatientEdit.PhysicianId == 0)
                return false;

            return true;
        }

        private static void CopyPatient(Patient src, Patient dest)
        {
            _log.Debug("CopyPatient");

            dest.Id = src.Id.Trim();
            dest.Lastname = src.Lastname.Trim();
            dest.Firstname = src.Firstname.Trim();
            dest.Birthdate = src.Birthdate;
            dest.Gender = src.Gender;
            dest.PhysicianId = src.PhysicianId;
            dest.PhysicianName = src.PhysicianName;
        }

        private void Delete()
        {
            _log.Debug("Delete Patient");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Confirm deletion of selected patient"];
            var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = Patient.Id;
                int res = _sqlManager.DeletePatient(sqlParameters);

                if(res == 1)
                {
                    CommonUtil.DeleteFolder(Constants.DataRootPath + "\\" + Patient.Id);
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
                }
                else
                {
                    _log.Error("Delete Error : id=" + Patient.Id);
                }
            }
        }

        private void SelectPhysician()
        {
            _log.Debug("SelectPhysician");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedPhysicianId"] = PatientEdit.PhysicianId;

            var result = _dialogService.OpenDialog(new PhysicianDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                Physician physician = (Physician)data["selectedPhysician"];
                PatientEdit.PhysicianId = physician.Id;
                PatientEdit.PhysicianName = physician.Name;
            }
        }
    }
}
