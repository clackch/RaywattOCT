using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Enums;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattOCT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Navigation;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class ReviewPresetViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewPresetViewModel));

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

        private IList<Code> pullbackTypes;

        [ObservableProperty]
        private string _pbLength;

        [ObservableProperty]
        private string _pbSpeed;

        [ObservableProperty]
        private string _pbTime;

        [ObservableProperty]
        private Dictionary<string, string> _colormapList;

        [ObservableProperty]
        private string _selectedColormap;

        [ObservableProperty]
        private int _maxCalciumThreshold;

        [ObservableProperty]
        private int _minCalciumThreshold;

        private int originCalciumThreshold;

        [ObservableProperty]
        private int _maxExpansionThreshold;

        [ObservableProperty]
        private int _minExpansionThreshold;

        private int originExpansionThreshold;

        [ObservableProperty]
        private double _maxAppositionThreshold;

        [ObservableProperty]
        private double _minAppositionThreshold;

        [ObservableProperty]
        private LongitudeOrientation _longitudeOrientation;

        private double originAppositionThreshold;

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

        public ReviewPresetViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewPresetViewModel");

            Constants.CurrentPage = Constants.ReviewPresetPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);

            ColormapList = CodeDefinition.Codes["CLMP"];

            MaxCalciumThreshold = Constants.MaxCalciumThreshold;
            MinCalciumThreshold = Constants.MinCalciumThreshold;
            MaxExpansionThreshold = Constants.MaxExpansionThreshold;
            MinExpansionThreshold = Constants.MinExpansionThreshold;
            MaxAppositionThreshold = Constants.MaxAppositionThreshold;
            MinAppositionThreshold = Constants.MinAppositionThreshold;

            // TODO: db 읽어서 가져와야 함 hwjung
            //LongitudeOrientation = PatientCase.LongitudeOrientation;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];

                CheckLongitudeOrientation();
                LongitudeOrientation = PatientCase.LongitudeOrientation;

                Code pullback = pullbackTypes.FirstOrDefault(x => x.Key == PatientCase.PullbackType);
                if (pullback != null)
                {
                    string[] temp = pullback.Buffer1.Split("|");
                    PbLength = temp[0];
                    PbSpeed = temp[1];
                    PbTime = temp[2];
                }

                SelectedColormap = PatientCase.Colormap;
                originCalciumThreshold = PatientCase.CalciumThreshold;
                originExpansionThreshold = PatientCase.ExpansionThreshold;
                originAppositionThreshold = PatientCase.AppositionThreshold;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Ok()
        {
            _log.Debug("Ok");

            ConfirmAndGoToReview();
        }
        private void Cancel()
        {
            _log.Debug("Cancel");

            PatientCase.CalciumThreshold = originCalciumThreshold;
            PatientCase.ExpansionThreshold = originExpansionThreshold;
            PatientCase.AppositionThreshold = originAppositionThreshold;

            GoToReview();
        }

        private void GoToReview()
        {
            _log.Debug("GoToReview");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPage) { Parameter = parameter });
        }

        private void ConfirmAndGoToReview()
        {
            _log.Debug("ConfirmAndGoToReview");

            if(PatientCase.LongitudeOrientation == _longitudeOrientation)
            {
                _log.Debug("[hwjung] not changed : " + _longitudeOrientation);
                Cancel();
                return;
            }

            RayError _ = (RayError)RayEndReview();
            Thread.Sleep(300);

            PatientCase.Colormap = SelectedColormap;
            CommonUtil.SetColormap(PatientCase.Colormap);

            RayError result = (RayError)RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }

            /* 방향 변경 */
            if (PatientCase.LongitudeOrientation != _longitudeOrientation)
            {
                PatientCase.LongitudeOrientationChanged = true;
                UpdateLongitudeOreinetation();
            }

            PatientCase.LongitudeOrientation = _longitudeOrientation;
            RaySetProperty(Property.LongitudeOrientation, (double)PatientCase.LongitudeOrientation);

            int numOfFrames = RayStartReview(PatientCase.ImageFullPath, PatientCase.ImageResolution, PatientCase.ZOffset);
            if (numOfFrames < (int)RayError.OK)
            {
                _log.Error("numOfFrames < (int)RayError.OK");
                _log.Error("patientCase.ImageFullPath : " + PatientCase.ImageFullPath);
                return;
            }
            else
            {
                for (int i = 0; i < 100; i++)
                {
                    if ((RayScannerState)RayGetProperty(Property.CurrentState) == RayScannerState.Review)
                        break;
                    Thread.Sleep(5);
                }
            }

            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total = 0;
            DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Current = 0;
            DeviceStatus.ReviewImageInfos[(int)RaySession.Compare].Total = 0;
            DeviceStatus.IsOCTImagingDone = false;



            Dictionary<string, Object> parameter = new Dictionary<string, Object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            ReviewStatus reviewStatus = new ReviewStatus();
            reviewStatus.NumberOfFrames = numOfFrames;
            parameter["reviewStatus"] = reviewStatus;
            Ray3DWrapper.ray3DStatus = new Ray3DWrapper.Ray3DStatus();
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPage) { Parameter = parameter });
        }

        private void CheckLongitudeOrientation()
        {
            // TODO: hwjung, PhysicianId로 DB에서 PatientCase로 전환 하기.
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.PhysicianId;
            IList<Physician> Physicians = _sqlManager.SelectPhysician(sqlParameters);

            if (Physicians.Count == 0)
            {
                _log.Error("not find physician infomation");
                PatientCase.LongitudeOrientation = LongitudeOrientation.DistalToProximal;
                return;
            }

            if (Physicians[0].Isdistaltoproximal) PatientCase.LongitudeOrientation = LongitudeOrientation.DistalToProximal;
            else PatientCase.LongitudeOrientation = LongitudeOrientation.ProximalToDistal;

            LongitudeOrientation = PatientCase.LongitudeOrientation;
        }

        private void UpdateLongitudeOreinetation()
        {
            // load
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.PhysicianId;
            IList<Physician> Physicians = _sqlManager.SelectPhysician(sqlParameters);

            if (Physicians.Count == 0)
            {
                _log.Error("not find physician infomation");
                PatientCase.LongitudeOrientation = LongitudeOrientation.DistalToProximal;
                return;
            }

            // update value
            Physicians[0].Isdistaltoproximal = LongitudeOrientation == LongitudeOrientation.DistalToProximal;

            // update
            int result = _sqlManager.UpdatePhysician(Physicians[0]);
        }
    }
}
