using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using log4net;

namespace RaywattApp.ViewModels
{
    public partial class PatientNewViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientNewViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private string _genderCode;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _newRecordingCommand;
        public ICommand NewRecordingCommand
        {
            get { return this._newRecordingCommand ?? (this._newRecordingCommand = new RelayCommand(NewRecording, CanNewRecording)); }
        }

        public PatientNewViewModel(SqlManager sqlManager)
        {
            _log.Debug("PatientNewViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.PatientNewPage;

            _sqlManager = sqlManager;

            Patient = new Patient();
            Patient.Id = "";
            Patient.Lastname = "";
            Patient.Firstname = "";
            Patient.Birthdate = System.DateTime.Today;

            Patient.PropertyChanged += Patient_PropertyChanged;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml"));
        }

        private void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("GoBack"));
        }

        private bool CanNewRecording()
        {
            _log.Debug("CanNewRecording");

            return Validate();
        }

        private void Patient_PropertyChanged(object sender, EventArgs e)
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
                //Sample로 넣어봄 -> popup으로 할지, 화면에서 표시할지 결정 필요
                WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "MessagePopupControl", Type = (int)CommonDefinition.PopupType.Message, Level = (int)CommonDefinition.PopupLevel.Info, Parameter = _l10n["ID is duplicated."] });
                return;
            }

            //Save New Patient Info
            sqlParameters.Clear();
            sqlParameters["id"] = Patient.Id = Patient.Id.Trim();
            sqlParameters["lastname"] = Patient.Lastname = Patient.Lastname.Trim();
            sqlParameters["firstname"] = Patient.Firstname = Patient.Firstname.Trim();
            sqlParameters["birthdate"] = Patient.Birthdate;
            if (GenderCode != null)
            {
                sqlParameters["gender"] = GenderCode;
                Patient.Gender = CodeDefinition.Codes["GEND"][GenderCode];
            }
            else
            {
                sqlParameters["gender"] = "";
            }              

            int nRows = _sqlManager.InsertPatient(sqlParameters);

            if(nRows == 1)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/RecordingPage.xaml") { Parameter = Patient });
            }
            else
            {
                _log.Error("Insert Error");
            }
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

            return true;
        }
    }
}
