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
using RaywattApp.Common.Paging;
using System.Windows.Controls;

namespace RaywattApp.ViewModels
{
    public partial class PatientDetailViewModel : PagingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientDetailViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private IList<PatientCaseByDate> _patientCaseByDate;

        [ObservableProperty]
        private IList<PatientCase> _patientCaseList;

        [ObservableProperty]
        private bool? _checkBoxAllSelected;

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
            get { return this._showPatientCaseCommand ?? (this._showPatientCaseCommand = new RelayCommand<PatientCaseByDate>(ShowPatientCase)); }
        }

        private ICommand _hidePatientCaseCommand;
        public ICommand HidePatientCaseCommand
        {
            get { return this._hidePatientCaseCommand ?? (this._hidePatientCaseCommand = new RelayCommand<PatientCaseByDate>(HidePatientCase)); }
        }

        private ICommand _checkBoxToggleCommand;
        public ICommand CheckBoxToggleCommand
        {
            get { return this._checkBoxToggleCommand ?? (this._checkBoxToggleCommand = new RelayCommand<CheckBox>(ToggleCheckBox)); }
        }

        private ICommand _checkBoxClickCommand;
        public ICommand CheckBoxClickCommand
        {
            get { return this._checkBoxClickCommand ?? (this._checkBoxClickCommand = new RelayCommand(ChangeCheckBoxHeader)); }
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

            PagingSelectedPageSize = 5;

            //Initialize Complete
            bCheckInit = true;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Patient = (Patient)extraData;
                Search();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        override protected void Search()
        {
            _log.Debug("Search");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;

            Dictionary<string, Object> sqlAdditionalCondition = new Dictionary<string, Object>();
            sqlAdditionalCondition["ORDER"] = "key DESC";
            sqlAdditionalCondition["LIMIT"] = PagingSelectedPageSize;
            sqlAdditionalCondition["OFFSET"] = PagingOffset;

            //Paging을 위한 전체 Row 수 Count
            PagingTotalCnt = _sqlManager.PageCountPatientCaseByDate(sqlParameters);

            PatientCaseByDate = _sqlManager.PageSelectPatientCaseByDate(sqlParameters, sqlAdditionalCondition);

            if (PatientCaseByDate.Count > 0)
            {
                ShowPatientCase(PatientCaseByDate[0]);
            }
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

            if(PatientCaseList != null)
            {
                foreach (PatientCase patientCase in PatientCaseList)
                {
                    if (patientCase.IsChecked)
                        selectedItem.Add(patientCase.Id);
                }
            }
            fileExportData.SelectedItem = selectedItem;

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "FilePopupControl", Type = (int)CommonDefinition.PopupType.File, FileType = (int)CommonDefinition.FileType.Export, Parameter = fileExportData });
        }

        private void ShowPatientCase(PatientCaseByDate patientCaseByDate)
        {
            _log.Debug("ShowPatientCase");

            if (patientCaseByDate == null)
                return;
            
            foreach (PatientCaseByDate keyValue in PatientCaseByDate)
            {
                keyValue.IsSelected = false;
            }
            patientCaseByDate.IsSelected = true;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;
            sqlParameters["date"] = patientCaseByDate.Key;

            if (patientCaseByDate.PatientCaseList == null)
                patientCaseByDate.PatientCaseList = _sqlManager.SelectPatientCaseList(sqlParameters);

            PatientCaseList = patientCaseByDate.PatientCaseList;

            ChangeCheckBoxHeader();
        }

        private void HidePatientCase(PatientCaseByDate patientCaseByDate)
        {
            _log.Debug("HidePatientCase");

            if(patientCaseByDate != null)
                patientCaseByDate.IsSelected = false;
        }

        private void ToggleCheckBox(CheckBox checkBox)
        {
            if(checkBox.IsChecked == true)
            {
                foreach (PatientCase patientCase in PatientCaseList)
                {
                    patientCase.IsChecked = true;
                }
            }
            else if(checkBox.IsChecked == false)
            {
                foreach (PatientCase patientCase in PatientCaseList)
                {
                    patientCase.IsChecked = false;
                }
            }
        }

        private void ChangeCheckBoxHeader()
        {
            bool isChecked = false;
            bool isNotChecked = false;

            foreach (PatientCase patientCase in PatientCaseList)
            {
                if (patientCase.IsChecked == true)
                    isChecked = true;

                if (patientCase.IsChecked == false)
                    isNotChecked = true;
            }

            if (isChecked && isNotChecked)
            {
                CheckBoxAllSelected = null;
            }
            else if(isChecked && !isNotChecked)
            {
                CheckBoxAllSelected = true;
            }
            else if(!isChecked && isNotChecked)
            {
                CheckBoxAllSelected = false;
            }
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
