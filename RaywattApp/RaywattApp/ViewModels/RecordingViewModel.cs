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
using RaywattOCT;
using static RaywattOCT.RayCoreWrapper;
using System.Windows.Threading;
using RaywattApp.Common.Util;

namespace RaywattApp.ViewModels
{
    public partial class RecordingViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

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

        public RecordingViewModel(SqlManager sqlManager)
        {
            _log.Debug("RecordingViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.RecordingPage;

            _sqlManager = sqlManager;

            PatientCase = new PatientCase();
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

                //Preset 화면으로 갔다가, Back 한 경우
                if (data.ContainsKey("patientCase"))
                    PatientCase = (PatientCase)data["patientCase"];

                RaySetProperty(Property.BackgroundColor, Constants.BackgroundColor);
            }

            timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
            timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
            timerUpdateImage.Start();

        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void RedoPullback()
        {
            _log.Debug("RedoPullback");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/LiveViewPage.xaml") { Parameter = parameter });
        }

        private void Confirm()
        {
            _log.Debug("Confirm");

            //TO-DO 초기값 정의 및 Preset 화면에서 Back해서 돌아온 경우에 대한 처리 필요
            PatientCase.PhysicianName = Constants.NotSelected;
            PatientCase.AccessionNumber = "";
            PatientCase.AccessionName = "";
            PatientCase.Comment = "";
            PatientCase.Vessel = "$000";
            PatientCase.Procedure = "$000";
            PatientCase.ThumbnailNo = 1;
            PatientCase.StillImageYn = "N";
            PatientCase.Image = "";
            PatientCase.PullbackType = "LONG";
            PatientCase.Brightness = 30;
            PatientCase.Contrast = 10;
            PatientCase.AngioCoRegistration = false;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            SetDetailStatusInit();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPresetPage.xaml") { Parameter = parameter });
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
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);
            }
            if (imgLongitude != null)
            {
                RayScannerState state = (RayScannerState)RayGetProperty(Property.CurrentState);

                if (state == RayScannerState.Review)
                {
                    LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgLongitude);
                }
            }
        }
        private string generateFileName(string ext)
        {
            string filename = "{" + 
                CommonUtil.GetRandomText(8) + "-" + 
                CommonUtil.GetRandomText(4) + "-" + 
                CommonUtil.GetRandomText(4) + "-" + 
                CommonUtil.GetRandomText(4) + "-" + 
                CommonUtil.GetRandomText(12) + 
                "}."+ext;

            _log.Debug("generateFileName : " + filename);

            return filename;
        }

        protected override void handleState(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayScannerState state)
        {
        }

        protected override void handleProgress(RayCoreWrapper.RayCallbackRequest request, int progress)
        {
        }

        protected override void handleError(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayError error)
        {
        }

        protected override void handleWorkDone(RayCoreWrapper.RayCallbackRequest request, RayCoreWrapper.RayWorkItem work)
        {
        }
    }
}
