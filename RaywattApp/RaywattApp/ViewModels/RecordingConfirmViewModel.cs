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
using RaywattApp.Common.Angio;

namespace RaywattApp.ViewModels
{
    public partial class RecordingConfirmViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingConfirmViewModel));

        private readonly SqlManager _sqlManager;
        private readonly AngioManager _angioManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private Zoom _zoom = new Zoom(Constants.CrossSectionConfirmSize);

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

        public RecordingConfirmViewModel(SqlManager sqlManager, AngioManager angioManager)
        {
            _log.Debug("RecordingConfirmViewModel");

            Constants.CurrentPage = Constants.RecordingConfirmPage;

            _sqlManager = sqlManager;
            _angioManager = angioManager;
            
            RayLaserOnOff(false);
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

                Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);

                GetImageInfo(RaySession.Review);
                RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

                MoveToFrame(RaySession.Review, DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current);
                Playback();
            }

            _angioManager.ReadyToRecv = false;
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

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            parameter["patientCase"] = PatientCase;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingSetupPage) { Parameter = parameter });
        }

        private void Confirm()
        {
            _log.Debug("Confirm");

            //RayError result = (RayError) RayUnloadCatheter();
            //if (result == RayError.OK)
            //{
            //    DeviceStatus.CatheterStatus = Constants.CatheterStatusUnloading;
            //}
            //else {
            //    _log.Debug("RayUnloadCatheter - " + result);
            //}

            RaySetSession(RaySession.Review);
            int numOfFrames = (int) RayGetProperty(Property.ImageDepth);

            RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);

            //초기값 설정
            PatientCase.Id = Patient.Id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            PatientCase.PatientId = Patient.Id;
            PatientCase.NumOfFrames = numOfFrames;
            PatientCase.AngioYn = DeviceStatus.IsAngioInitialized;
            PatientCase.IndicatorDegree = 90;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["patient_id"] = PatientCase.PatientId;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["comment"] = PatientCase.Comment;
            sqlParameters["vessel"] = PatientCase.Vessel;
            sqlParameters["location"] = PatientCase.Location;
            sqlParameters["procedure"] = PatientCase.Procedure;
            sqlParameters["num_of_frames"] = PatientCase.NumOfFrames;
            sqlParameters["image"] = PatientCase.Image;
            sqlParameters["image_resolution"] = PatientCase.ImageResolution;
            sqlParameters["manual_calibration"] = PatientCase.ManualCalibration;
            sqlParameters["field_of_view"] = PatientCase.FieldOfView;
            sqlParameters["pullback_type"] = PatientCase.PullbackType;
            sqlParameters["pullback_length"] = PatientCase.PullbackLength;
            sqlParameters["angio_yn"] = PatientCase.AngioYn;
            sqlParameters["angio_co_registration"] = PatientCase.AngioCoRegistration;
            sqlParameters["indicator_degree"] = PatientCase.IndicatorDegree;
            sqlParameters["flush_media"] = PatientCase.FlushMedia;
            sqlParameters["pullback_trigger"] = PatientCase.PullbackTrigger;
            sqlParameters["colormap"] = PatientCase.Colormap;
            sqlParameters["calcium_threshold"] = PatientCase.CalciumThreshold;
            sqlParameters["expansion_calculation"] = PatientCase.ExpansionCalculation;
            sqlParameters["expansion_threshold"] = PatientCase.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = PatientCase.AppositionThreshold;
            sqlParameters["brightness"] = PatientCase.Brightness;
            sqlParameters["contrast"] = PatientCase.Contrast;
            PatientCase.SectionProximal = 0;
            sqlParameters["section_proximal"] = PatientCase.SectionProximal;
            PatientCase.SectionDistal = PatientCase.NumOfFrames - 1;
            sqlParameters["section_distal"] = PatientCase.SectionDistal;

            int nRows = _sqlManager.InsertPatientCase(sqlParameters);

            if (nRows == 1)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                parameter["patientCase"] = PatientCase;
                SetDetailStatusInit();
                parameter["prevStatus"] = PrevStatus;
                ReviewStatus reviewStatus = new ReviewStatus();
                reviewStatus.NumberOfFrames = numOfFrames;
                parameter["reviewStatus"] = reviewStatus;
                Ray3DWrapper.ray3DStatus = new Ray3DWrapper.Ray3DStatus();
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPage) { Parameter = parameter });
            }
            else
            {
                _log.Error("Insert Error");
            }            
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
