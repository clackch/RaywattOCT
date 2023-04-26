using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using static RaywattOCT.RayCoreWrapper;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Annotation.Models;
using Newtonsoft.Json;

namespace RaywattApp.ViewModels
{
    public partial class ReviewLumenEditViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewLumenEditViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        private int frameNumber;
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

        private List<LumenContour> _lumenContours;
        public List<LumenContour> LumenContours { get { return _lumenContours; } set { _lumenContours = value; OnPropertyChanged(nameof(LumenContours)); } }

        private string _lumenContourCommand;
        public string LumenContourCommand { get { return _lumenContourCommand; } set { _lumenContourCommand = value; OnPropertyChanged(nameof(LumenContourCommand)); } }

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand(Ok)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _informationCommand;
        public ICommand InformationCommand
        {
            get { return this._informationCommand ?? (this._informationCommand = new RelayCommand(Information)); }
        }

        private ICommand _zoomInCommand;
        public ICommand ZoomInCommand
        {
            get { return this._zoomInCommand ?? (this._zoomInCommand = new RelayCommand(ZoomIn)); }
        }

        private ICommand _zoomOutCommand;
        public ICommand ZoomOutCommand
        {
            get { return this._zoomOutCommand ?? (this._zoomOutCommand = new RelayCommand(ZoomOut)); }
        }

        private ICommand _restoreCommand;
        public ICommand RestoreCommand
        {
            get { return this._restoreCommand ?? (this._restoreCommand = new RelayCommand(Restore)); }
        }

        private ICommand _resetCommand;
        public ICommand ResetCommand
        {
            get { return this._resetCommand ?? (this._resetCommand = new RelayCommand(Reset)); }
        }

        private ICommand _autoDetectCommand;
        public ICommand AutoDetectCommand
        {
            get { return this._autoDetectCommand ?? (this._autoDetectCommand = new RelayCommand(AutoDetect)); }
        }

        private ICommand _cmdPlayback;
        public ICommand CmdPlayback
        {
            get { return this._cmdPlayback ?? (this._cmdPlayback = new RelayCommand<object>(Playback)); }
        }

        public ReviewLumenEditViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewLumenEditViewModel");

            Constants.CurrentPage = Constants.ReviewLumenEditPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                PatientCase = (PatientCase)data["patientCase"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];

                LumenContours = JsonConvert.DeserializeObject<List<LumenContour>>(PatientCase.LumenContour);

                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");

            if (timerUpdateImage.IsEnabled)
                timerUpdateImage.Stop();
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (DrawCrossSectionImage())
            {
                FrameNumber = crossSectionFrameInfo[0].curFrame;
            }
        }

        private void Ok()
        {
            _log.Debug("Ok");

            GoToPreviousPage(true);
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            GoToPreviousPage(false);
        }

        private void GoToPreviousPage(bool isSave)
        {
            _log.Debug("GoToPreviousPage");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            if (isSave)
            {
                PatientCase.LumenContour = ConvertLumenContourToJson(LumenContours);
            }
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(ReviewStatus.CurrentPage) { Parameter = parameter });
        }

        private string ConvertLumenContourToJson(List<LumenContour> lumenContours)
        {
            foreach(LumenContour lumenContour in lumenContours)
            {
                lumenContour.PointsAll = null;
            }

            return JsonConvert.SerializeObject(lumenContours, Formatting.Indented);
        }

        private void Information()
        {
            _log.Debug("Information");
        }

        private void ZoomIn()
        {
            _log.Debug("ZoomIn");

            LumenContourCommand = Constants.LumenContourZoomIn;
        }

        private void ZoomOut()
        {
            _log.Debug("ZoomOut");

            LumenContourCommand = Constants.LumenContourZoomOut;
        }

        private void Restore()
        {
            _log.Debug("Restore");

            LumenContourCommand = Constants.LumenContourRestore;
        }

        private void Reset()
        {
            _log.Debug("Reset");

            LumenContourCommand = Constants.LumenContourReset;
        }

        private void AutoDetect()
        {
            _log.Debug("AutoDetect");

            LumenContourCommand = Constants.LumenContourAutoDetect;
        }

        private void Playback(object param)
        {
            string action = (string)param;
            RayError result = RayError.OK;

            if (action.ToLower().Equals("prev"))
            {
                result = (RayError)RayPrevFrame();
            }
            else if (action.ToLower().Equals("next"))
            {
                result = (RayError)RayNextFrame();
            }
        }
    }
}
