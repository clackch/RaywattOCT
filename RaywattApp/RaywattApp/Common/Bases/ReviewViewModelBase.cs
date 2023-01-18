using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace RaywattApp.Common.Bases
{
    public abstract partial class ReviewViewModelBase : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModelBase));

        protected readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private bool _expandLeftUpMenu;

        [ObservableProperty]
        private bool _expandLeftDownMenu;

        [ObservableProperty]
        private bool _expandRightMenu;

        private int frameNumber = 0;
        public int FrameNumber
        {
            get { return frameNumber; }
            set
            {
                frameNumber = value;
                OnPropertyChanged(nameof(FrameNumber));

                syncWithCoreSystem();
            }
        }

        private ICommand _reviewTypeSwitchCommand;
        public ICommand ReviewTypeSwitchCommand
        {
            get { return this._reviewTypeSwitchCommand ?? (this._reviewTypeSwitchCommand = new RelayCommand<string>(ReviewTypeSwitch)); }
        }

        private ICommand _editCaseCommand;
        public ICommand EditCaseCommand
        {
            get { return this._editCaseCommand ?? (this._editCaseCommand = new RelayCommand(EditCase)); }
        }

        private ICommand _editPresetCommand;
        public ICommand EditPresetCommand
        {
            get { return this._editPresetCommand ?? (this._editPresetCommand = new RelayCommand(EditPreset)); }
        }

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get { return this._exportCommand ?? (this._exportCommand = new RelayCommand(Export)); }
        }

        private ICommand _endReviewCommand;
        public ICommand EndReviewCommand
        {
            get { return this._endReviewCommand ?? (this._endReviewCommand = new RelayCommand(EndReview)); }
        }

        private ICommand _newRecordingCommand;
        public ICommand NewRecordingCommand
        {
            get { return this._newRecordingCommand ?? (this._newRecordingCommand = new RelayCommand(NewRecording)); }
        }

        private ICommand _expandCollapseCommand;
        public ICommand ExpandCollapseCommand
        {
            get { return this._expandCollapseCommand ?? (this._expandCollapseCommand = new RelayCommand<string>(ExpandCollapseMenu)); }
        }

        public ReviewViewModelBase()
        {
            _log.Debug("ReviewViewModelBase");
        }

        public ReviewViewModelBase(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewViewModelBase");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        private void ReviewTypeSwitch(string url)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(url) { Parameter = parameter });
        }

        private void EditCase()
        {
            _log.Debug("EditCase");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["vessel"] = PatientCase.Vessel;
            parameter["procedure"] = PatientCase.Procedure;
            parameter["physicianName"] = PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;
            var result = _dialogService.OpenDialog(new EditCaseInfoDialogControl(), parameter);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                PatientCase.Vessel = data["vessel"].ToString();
                PatientCase.Procedure = data["procedure"].ToString();
                PatientCase.PhysicianName = data["physicianName"].ToString();
                PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                PatientCase.Comment = data["comment"].ToString();
            }
        }

        private void EditPreset()
        {
            _log.Debug("EditPreset");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patientCase"] = PatientCase;
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPresetPage.xaml") { Parameter = parameter });
        }

        private void Export()
        {
            _log.Debug("Export");

            //화면 변경 사항에 대해서도 Export 하기 위해서, Save 처리
            Save();

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["fileType"] = Constants.FileTypeExport;
            FileExport fileExport = new FileExport();
            fileExport.PatientId = Patient.Id;
            fileExport.SelectedItem = new List<string>();
            fileExport.SelectedItem.Add(PatientCase.Id);
            fileExport.IsFromReview = true;
            fileExport.CurrentFrame = FrameNumber;
            fileExport.BookmarkedFrames = new List<int>();
            //TO-DO : Bookmark 기능 추가 후, bookmark 된 내역 전달 필요
            parameter["fileExport"] = fileExport;

            var result = _dialogService.OpenDialog(new FileDialogControl(), parameter);
        }

        protected virtual void Save() { }

        private void EndReview()
        {
            _log.Debug("EndReview");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
        }

        private void NewRecording()
        {
            _log.Debug("NewRecording");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/RecordingSetupPage.xaml") { Parameter = parameter });
        }

        private void ExpandCollapseMenu(string param)
        {
            switch (param)
            {
                case Constants.LeftUpMenu:
                    ExpandLeftUpMenu = !ExpandLeftUpMenu;
                    break;
                case Constants.LeftDownMenu:
                    ExpandLeftDownMenu = !ExpandLeftDownMenu;
                    break;
                case Constants.RightMenu:
                    ExpandRightMenu = !ExpandRightMenu;
                    break;
                default:
                    break;
            }
        }
    }
}
