using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Services;
using RaywattApp.Models;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class PatientDetailViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientDetailViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private IList<CustomExpander> _patientCaseByDate;

        [ObservableProperty]
        private IList<PatientCase> _patientCaseList;

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get { return this._exportCommand ?? (this._exportCommand = new RelayCommand(Export)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _patiendEditCommand;
        public ICommand PatientEditCommand
        {
            get { return this._patiendEditCommand ?? (this._patiendEditCommand = new RelayCommand(GoPatientEdit)); }
        }

        private ICommand _newRecordingCommand;
        public ICommand NewRecordingCommand
        {
            get { return this._newRecordingCommand ?? (this._newRecordingCommand = new RelayCommand(NewRecording)); }
        }

        private ICommand _showPatientCaseCommand;
        public ICommand ShowPatientCaseCommand
        {
            get { return this._showPatientCaseCommand ?? (this._showPatientCaseCommand = new RelayCommand<CustomExpander>(ShowPatientCase)); }
        }

        private ICommand _hidePatientCaseCommand;
        public ICommand HidePatientCaseCommand
        {
            get { return this._hidePatientCaseCommand ?? (this._hidePatientCaseCommand = new RelayCommand<CustomExpander>(HidePatientCase)); }
        }

        private ICommand _goReviewCommand;
        public ICommand GoReviewCommand
        {
            get { return this._goReviewCommand ?? (this._goReviewCommand = new RelayCommand<PatientCase>(GoReview)); }
        }

        public PatientDetailViewModel(SqlManager sqlManager)
        {
            _log.Debug("PatientDetailViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.PatientDetailPage;

            _sqlManager = sqlManager;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Patient = (Patient)extraData;
            }

            SetPatientCaseByDate();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml"));
        }

        private void GoPatientEdit()
        {
            _log.Debug("GoPatientEdit");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientEditPage.xaml") { Parameter = Patient });
        }

        private void NewRecording()
        {
            _log.Debug("NewRecording");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/RecordingPage.xaml") { Parameter = Patient });
        }

        private void Export()
        {
            _log.Debug("Export");

            FileExport fileExportData = new FileExport();
            List<string> selectedItem = new List<string>();
            selectedItem.Add("TEST111");
            selectedItem.Add("TEST222");
            selectedItem.Add("TEST333");

            fileExportData.SelectedItem = selectedItem;

            //항목 선택된 건 Parameter로 넘기도록
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "FilePopupControl", Type = (int)CommonDefinition.PopupType.File, FileType = (int)CommonDefinition.FileType.Export, Parameter = fileExportData });
        }

        private void SetPatientCaseByDate()
        {
            _log.Debug("SetPatientCaseByDate");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;

            PatientCaseByDate = _sqlManager.SelectPatientCaseByDate(sqlParameters);

            if(PatientCaseByDate.Count > 0)
            {
                ShowPatientCase(PatientCaseByDate[0]);
            }
        }

        private void ShowPatientCase(CustomExpander patientCase)
        {
            _log.Debug("ShowPatientCase");

            foreach (CustomExpander keyValue in PatientCaseByDate)
            {
                keyValue.IsSelected = false;
            }
            patientCase.IsSelected = true;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;
            sqlParameters["date"] = patientCase.Key;

            PatientCaseList = _sqlManager.SelectPatientCaseList(sqlParameters);
        }

        private void HidePatientCase(CustomExpander patientCase)
        {
            _log.Debug("HidePatientCase");

            patientCase.IsSelected = false;
        }

        private void GoReview(PatientCase patientCase)
        {
            Dictionary<string, Object> parameters = new Dictionary<string, Object>();
            parameters["patient"] = Patient;
            parameters["patientCase"] = patientCase;

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPage.xaml") { Parameter = parameters });
        }
    }
}
