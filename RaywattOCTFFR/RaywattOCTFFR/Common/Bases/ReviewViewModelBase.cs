using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using Newtonsoft.Json;
using RaywattOCTFFR.Common.Annotation.Models;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattOCTFFR.Common.Bases
{
    public abstract partial class ReviewViewModelBase : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModelBase));

        protected readonly SqlManager _sqlManager;

        protected IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus;

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

        private int frameNumber = -1;
        public int FrameNumber
        {
            get { return frameNumber; }
            set
            {
                frameNumber = value;
                OnPropertyChanged(nameof(FrameNumber));
                DisplayFrameNumber = FrameNumber + 1;
            }
        }

        [ObservableProperty]
        private int displayFrameNumber;

        [ObservableProperty]
        private ObservableCollection<Bookmark> bookmarks;

        private ICommand _reviewTypeSwitchCommand;
        public ICommand ReviewTypeSwitchCommand
        {
            get { return this._reviewTypeSwitchCommand ?? (this._reviewTypeSwitchCommand = new RelayCommand<string>(ReviewTypeSwitch)); }
        }

        private ICommand _editPresetCommand;
        public ICommand EditPresetCommand
        {
            get { return this._editPresetCommand ?? (this._editPresetCommand = new RelayCommand(EditPreset)); }
        }

        private ICommand _editLumenContourCommand;
        public ICommand EditLumenContourCommand
        {
            get { return this._editLumenContourCommand ?? (this._editLumenContourCommand = new RelayCommand(EditLumenContour)); }
        }

        private ICommand _manualCalibrationCommand;
        public ICommand ManualCalibrationCommand
        {
            get { return this._manualCalibrationCommand ?? (this._manualCalibrationCommand = new RelayCommand(ManualCalibration)); }
        }

        private ICommand _endReviewCommand;
        public ICommand EndReviewCommand
        {
            get { return this._endReviewCommand ?? (this._endReviewCommand = new RelayCommand(EndReview)); }
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

        public ReviewViewModelBase(IDialogService dialogService)
        {
            _log.Debug("ReviewViewModelBase");

            _dialogService = dialogService;
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
            parameter["reviewStatus"] = ReviewStatus;

            WeakReferenceMessenger.Default.Send(new NavigationMessage(url) { Parameter = parameter });
        }

        private void EditPreset()
        {
            _log.Debug("EditPreset");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPresetPage) { Parameter = parameter });
        }

        private void EditLumenContour()

        {
            _log.Debug("EditLumenContour");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewLumenEditPage) { Parameter = parameter });
        }

        private void ManualCalibration()
        {
            _log.Debug("ManualCalibration");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewCalibrationPage) { Parameter = parameter });
        }

        private List<int> GetBookmarks()
        {
            List<int> bookmarks = new List<int>();

            if(Constants.CurrentPage != Constants.ReviewPage)
            {
                Bookmarks = JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(PatientCase.Bookmark);
            }

            foreach(Bookmark bookmark in Bookmarks)
            {
                bookmarks.Add(bookmark.FrameNumber);
            }

            bookmarks.Sort();

            return bookmarks;
        }

        protected virtual void Save() { }

        private void EndReview()
        {
            _log.Debug("EndReview");

            DeviceStatus.IsOCTImagingDone = true;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
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

        protected void StopPlayback()
        {
            if (!DeviceStatus.IsPaused)
            {
                Playback();
                ReviewStatus.IsPlay = false;
            }
                
        }
    }
}
