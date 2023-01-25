using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class CalibrationViewModel : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CalibrationViewModel));

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        private DispatcherTimer timerUpdateImage = new DispatcherTimer();

        private ICommand _cmdBack;
        public ICommand CmdBack
        {
            get { return _cmdBack ?? (this._cmdBack = new RelayCommand(Back)); }
        }

        private ICommand _cmdManualZoomIn;
        public ICommand CmdManualZoomIn
        { 
            get { return _cmdManualZoomIn ?? (this._cmdManualZoomIn = new RelayCommand<bool>(ManualZoomIn)); }
        }

        private ICommand _cmdAutoCalibration;
        public ICommand CmdAutoCalibration
        { 
            get { return _cmdAutoCalibration ?? (this._cmdAutoCalibration = new RelayCommand(AutoCalibration)); }
        }

        public CalibrationViewModel()
        {
            _log.Debug("CalibrationViewModel");

            Constants.CurrentPage = Constants.CalibrationPage;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];

                RaySetProperty(Property.BackgroundColor, Constants.BackgroundColor);

                timerUpdateImage.Interval = TimeSpan.FromMilliseconds(Constants.UpdateImageInterval);
                timerUpdateImage.Tick += new EventHandler(timerFuncUpdateImage);
                timerUpdateImage.Start();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
        }


        private void Back()
        {
            _log.Debug("Back");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.LiveViewPage) { Parameter = parameter });
        }

        private void ManualZoomIn(bool zoomIn)
        {
            _log.Debug("ManualZoomIn : " + ((zoomIn) ? "IN" : "OUT"));
            
            RayManualCalibration(zoomIn);
        }

        private void AutoCalibration() {
            RayAutoCalibration();
            DeviceStatus.CanExecuteCalibration = false;
        }

        private void timerFuncUpdateImage(object sender, EventArgs e)
        {
            if (imgCrossSection != null)
            {
                CrossSectionImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imgCrossSection);
            }
        }
    }
}
