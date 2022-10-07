using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.ViewModels
{
    public partial class MainViewModel
    {
        [ObservableProperty]
        private Patient _patientEdit;

        [ObservableProperty]
        private string _genderCodeEdit;

        private ICommand _settingCommand;
        public ICommand SettingCommand
        {
            get { return this._settingCommand ?? (this._settingCommand = new RelayCommand(Setting, CanButtonClick)); }
        }

        private ICommand _patiendEditCommand;
        public ICommand PatientEditCommand
        {
            get { return this._patiendEditCommand ?? (this._patiendEditCommand = new RelayCommand(ShowPatientEdit, CanButtonClick)); }
        }

        private ICommand _patiendEditSaveCommand;
        public ICommand PatientEditSaveCommand
        {
            get { return this._patiendEditSaveCommand ?? (this._patiendEditSaveCommand = new RelayCommand(SavePatientEdit, CanSavePatient)); }
        }

        private bool CanButtonClick()
        {
            _log.Debug("CanButtonClick");

            //View Layer Popup이 열려있는 경우, 다시 열리지 않도록 처리
            return !ShowViewLayerPopup;
        }

        private void Setting()
        {
            _log.Debug("Setting");

            PopupNavigationSource = "Views/Setting/SettingAcquisitionPage.xaml";
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "SettingPopupControl", Type = (int)CommonDefinition.PopupType.Setting });
        }

        private void ShowPatientEdit()
        {
            _log.Debug("ShowPatientEdit");

            PatientEdit = new Patient();
            CopyPatient(Patient, PatientEdit);
            GenderCodeEdit = CodeDefinition.Codes["GEND"].FirstOrDefault(x => x.Value == Patient.Gender).Key;

            PatientEdit.PropertyChanged += PatientEdit_PropertyChanged;

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "PatientEditPopupControl", Type = (int)CommonDefinition.PopupType.PatientEdit });
        }

        private void SavePatientEdit()
        {
            _log.Debug("SavePatientEdit");

            Dictionary<string, Object> commandParameters = new Dictionary<string, Object>();
            string commandText;

            if (!Patient.Id.Equals(PatientEdit.Id.Trim()))
            {
                //Check ID for Duplication
                commandParameters["id"] = Patient.Id;

                commandText =
                    $"SELECT count(*) " +
                    $"FROM rv_schema.patient " +
                    $"WHERE id = @id ";

                int nCnt = _databaseService.GetDataCount(commandText, commandParameters);

                if (nCnt > 0)
                {
                    WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "MessagePopupControl", Type = (int)CommonDefinition.PopupType.Message, Level = (int)CommonDefinition.PopupLevel.Info, Parameter = _l10n["ID is duplicated."] });
                    return;
                }
            }

            //Save New Patient Info
            commandParameters.Clear();
            commandParameters["id"] = PatientEdit.Id.Trim();
            commandParameters["lastname"] = PatientEdit.Lastname.Trim();
            commandParameters["firstname"] = PatientEdit.Firstname.Trim();
            commandParameters["birthdate"] = PatientEdit.Birthdate;
            if (GenderCodeEdit != null)
            {
                commandParameters["gender"] = GenderCodeEdit;
                PatientEdit.Gender = CodeDefinition.Codes["GEND"][GenderCodeEdit];
            }
            else
            {
                commandParameters["gender"] = "";
            }

            commandText =
                $"UPDATE rv_schema.patient " +
                $"SET id=@id, lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, update_date=now() " +
                $"WHERE id=@id";

            int nRows = _databaseService.UpdateData(commandText, commandParameters);

            if (nRows == 1)
            {
                CopyPatient(PatientEdit, Patient);
                WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.PatientEdit });
            }
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
    }
}
