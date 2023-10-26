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
using RaywattOCT;

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

                GetImageInfo(RaySession.Review);
                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);                
                Playback();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
        }

        private void RedoPullback()
        {
            _log.Debug("RedoPullback");

            DeviceStatus.IsSaveRawDataDone = true;
            DeviceStatus.IsLumenSaved = true;
            DeviceStatus.IsOCTImagingDone = true;
            DeviceStatus.IsPullbackDone = false;

            if (DeviceStatus.IsPaused == false)
            {
                Playback();
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingLiveViewPage) { Parameter = parameter });
        }

        private void Confirm()
        {
            _log.Debug("Confirm");

            DeviceStatus.CatheterStatus = Constants.CatheterStatusUnloading;
            RayUnloadCatheter();

            RaySetSession(RaySession.Review);
            int numOfFrames = (int) RayGetProperty(Property.ImageDepth);

            RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

            //TO-DO 초기값 정의 필요
            PatientCase.PhysicianName = Constants.NotSelected;
            PatientCase.AccessionNumber = "";
            PatientCase.AccessionName = "";
            PatientCase.Comment = "";
            PatientCase.Vessel = Constants.NotSelectedCode;
            PatientCase.NumOfFrames = numOfFrames;
            PatientCase.AngioCoRegistration = DeviceStatus.IsAngioConnected;
            PatientCase.IndicatorDegree = 90;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            SetDetailStatusInit();
            parameter["prevStatus"] = PrevStatus;
            ReviewStatus reviewStatus = new ReviewStatus();
            reviewStatus.NumberOfFrames = numOfFrames;
            parameter["reviewStatus"] = reviewStatus;
            Ray3DWrapper.ray3DStatus = new Ray3DWrapper.Ray3DStatus();
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPresetPage) { Parameter = parameter });
        }

        private void SetDetailStatusInit()
        {
            PrevStatus.DetailSelectedGroup = null;
            PrevStatus.DetailPageOffset = 0;
            PrevStatus.DetailPageGroup = 1;
            PrevStatus.DetailPageNumber = 0;
        }

        protected override void UpdateCrossSectionImage()
        {
            DrawCrossSectionImage();
        }
    }
}
