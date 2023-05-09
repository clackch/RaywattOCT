using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Services;
using System.Windows.Navigation;
using System;
using System.Collections.Generic;
using static RaywattOCT.RayCoreWrapper;
using System.Windows.Threading;

namespace RaywattApp.ViewModels
{
    public partial class RecordingConfirmViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingConfirmViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private bool _isPullbackDone = false;

        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _redoPullbackCommand;
        public ICommand RedoPullbackCommand
        {
            get { return this._redoPullbackCommand ?? (this._redoPullbackCommand = new RelayCommand(RedoPullback)); }
        }

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand
        {
            get { return this._confirmCommand ?? (this._confirmCommand = new RelayCommand(Confirm)); }
        }

        public RecordingConfirmViewModel(SqlManager sqlManager)
        {
            _log.Debug("RecordingConfirmViewModel");

            Constants.CurrentPage = Constants.RecordingConfirmPage;

            _sqlManager = sqlManager;
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

        private void RedoPullback()
        {
            _log.Debug("RedoPullback");

            RayEndReview();

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.LiveViewPage) { Parameter = parameter });
        }

        private void Confirm()
        {
            _log.Debug("Confirm");

            RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

            //TO-DO 초기값 정의 필요
            PatientCase.PhysicianName = Constants.NotSelected;
            PatientCase.AccessionNumber = "";
            PatientCase.AccessionName = "";
            PatientCase.Comment = "";
            PatientCase.Vessel = Constants.NotSelectedCode;
            PatientCase.ThumbnailNo = 1;
            PatientCase.StillImageYn = "N";
            PatientCase.AngioCoRegistration = DeviceStatus.IsAngioConnected;
            PatientCase.IndicatorDegree = 90;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            SetDetailStatusInit();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPresetPage) { Parameter = parameter });
        }

        private void SetDetailStatusInit()
        {
            PrevStatus.DetailSelectedGroup = null;
            PrevStatus.DetailPageOffset = 0;
            PrevStatus.DetailPageGroup = 1;
            PrevStatus.DetailPageNumber = 0;
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            DrawCrossSectionImage();
            if (DrawLongitudeImage())
            {
                // when generating longitude image is completed
                if (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame)
                {
                    IsPullbackDone = true;
                }
            }
        }

    }
}
