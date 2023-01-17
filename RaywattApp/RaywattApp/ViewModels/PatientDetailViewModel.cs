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
using RaywattApp.Views.Dialog;
using RaywattApp.Common.Dialog;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class PatientDetailViewModel : PagingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientDetailViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private IList<PatientCaseByDate> _patientCaseByDateList;

        [ObservableProperty]
        private PatientCaseByDate _curPatientCaseByDate;

        [ObservableProperty]
        private IList<PatientCase> _patientCaseList;

        [ObservableProperty]
        private bool? _checkBoxAllSelected;

        private List<string> selectedItem;

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get { return this._exportCommand ?? (this._exportCommand = new RelayCommand(Export)); }
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get { return this._deleteCommand ?? (this._deleteCommand = new RelayCommand(Delete)); }
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

        public PatientDetailViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientDetailViewModel");

            Constants.CurrentPage = Constants.PatientDetailPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            PagingSelectedPageSize = Constants.PageSizeDetail;

            //Initialize Complete
            bCheckInit = true;

            selectedItem = new List<string>();
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

                Search();
                ShowFirstLast();
                SetPrevStatus();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Search()
        {
            _log.Debug("Search");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;

            //Paging을 위한 전체 Row 수 Count
            PagingTotalCnt = _sqlManager.PageCountPatientCaseByDate(sqlParameters);

            Dictionary<string, Object> sqlAdditionalCondition = new Dictionary<string, Object>();
            sqlAdditionalCondition["ORDER"] = "key DESC";
            sqlAdditionalCondition["LIMIT"] = PagingSelectedPageSize;
            sqlAdditionalCondition["OFFSET"] = PagingOffset;

            PatientCaseByDateList = _sqlManager.PageSelectPatientCaseByDate(sqlParameters, sqlAdditionalCondition);

            //목록이 있을 경우, 첫 건 Expand 처리
            if (PatientCaseByDateList.Count > 0)
            {
                ShowPatientCase(PatientCaseByDateList[0]);
            }
        }

        private void SetPrevStatus()
        {
            PagingOffset = PrevStatus.DetailPageOffset;

            if (PrevStatus.DetailPageGroup == 0)
                PrevStatus.DetailPageGroup = 1;

            ShowPageNo(PrevStatus.DetailPageGroup);
            MovePageNo((PrevStatus.DetailPageNumber + 1).ToString());

            //조회한 Page의 List가 없을 경우, 이전 Page 조회
            if (PatientCaseByDateList.Count == 0)
            {
                if (PrevStatus.DetailPageNumber % Constants.PageNumberMax == 0)
                {
                    PrevStatus.DetailPageGroup -= Constants.PageNumberMax;
                }

                PrevStatus.DetailPageNumber--;

                if (PrevStatus.DetailPageNumber < 0)
                    return;

                ShowPageNo(PrevStatus.DetailPageGroup);
                MovePageNo((PrevStatus.DetailPageNumber + 1).ToString());
            }

            //기존에 선택한 Group이 있을 경우, 해당 Group 표시(Expand)
            if (PrevStatus.DetailSelectedGroup != null)
            {
                foreach (PatientCaseByDate keyValue in PatientCaseByDateList)
                {
                    if (keyValue.Key.Equals(PrevStatus.DetailSelectedGroup))
                    {
                        ShowPatientCase(keyValue);
                        break;
                    }
                }
            }
        }

        private void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml") { Parameter = PrevStatus});
        }

        private void GoPatientEdit()
        {
            _log.Debug("GoPatientEdit");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            GetDetailStatus();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientEditPage.xaml") { Parameter = parameter });
        }

        private void NewRecording()
        {
            _log.Debug("NewRecording");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            GetDetailStatus();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/RecordingSetupPage.xaml") { Parameter = parameter });
        }

        private void GetSelectedItem()
        {
            selectedItem.Clear();

            foreach (PatientCaseByDate patientCaseByDate in PatientCaseByDateList)
            {
                if (patientCaseByDate.PatientCaseList == null)
                    continue;

                foreach (PatientCase patientCase in patientCaseByDate.PatientCaseList)
                {
                    if (patientCase.IsChecked)
                        selectedItem.Add(patientCase.Id);
                }
            }
        }

        private void Export()
        {
            _log.Debug("Export");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["fileType"] = Constants.FileTypeExport;
            FileExport fileExport = new FileExport();
            fileExport.PatientId = Patient.Id;
            GetSelectedItem();
            fileExport.SelectedItem = selectedItem;
            parameter["fileExport"] = fileExport;

            var result = _dialogService.OpenDialog(new FileDialogControl(), parameter);
        }

        private void Delete()
        {
            _log.Debug("Delete");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            DialogResults? result = null;

            GetSelectedItem();

            if (PagingTotalCnt == 0 || selectedItem.Count == 0)
            {
                parameter.Clear();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["There are no items selected."];
                result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);

                return;
            }

            GetDetailStatus();

            parameter.Clear();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Are you sure to delete selected patient case?"];
            result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                DeletePatientCase();
            }
        }

        private void DeletePatientCase()
        {
            int cntDel = 0;
            int resDel = 0;

            foreach(string id in selectedItem)
            {
                cntDel++;
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = id;

                resDel += _sqlManager.DeletePatientCase(sqlParameters);
            }

            if (cntDel == resDel)
            {
                Search();
                SetPrevStatus();
            }
            else
            {
                _log.Error("Delete Error - Total : " + cntDel + " Deleted Cnt : " + resDel);
            }
        }

        private void ShowPatientCase(PatientCaseByDate patientCaseByDate)
        {
            _log.Debug("ShowPatientCase");

            if (patientCaseByDate == null)
                return;

            CurPatientCaseByDate = patientCaseByDate;

            foreach (PatientCaseByDate keyValue in PatientCaseByDateList)
            {
                keyValue.IsSelected = false;
            }
            patientCaseByDate.IsSelected = true;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;
            sqlParameters["date"] = patientCaseByDate.Key;

            if (patientCaseByDate.PatientCaseList == null)
                patientCaseByDate.PatientCaseList = _sqlManager.SelectPatientCaseListByDate(sqlParameters);

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
            _log.Debug("GoReview");

            if (patientCase == null)
                return;

            RaySetProperty(Property.BackgroundColor, 0x333333);
            RayStartReview(patientCase.Image);

            Dictionary<string, Object> parameter = new Dictionary<string, Object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = patientCase;
            GetDetailStatus();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPage.xaml") { Parameter = parameter });
        }

        private void GetDetailStatus()
        {
            PrevStatus.DetailPageGroup = (PagingNoIdx / Constants.PageNumberMax) * Constants.PageNumberMax + 1;
            PrevStatus.DetailPageNumber = PagingNoIdx;
            PrevStatus.DetailPageOffset = PagingOffset;
            if(CurPatientCaseByDate != null)
                PrevStatus.DetailSelectedGroup = CurPatientCaseByDate.Key;
        }
    }
}
