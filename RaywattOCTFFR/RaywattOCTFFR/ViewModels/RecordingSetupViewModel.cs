using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Models;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using RaywattOCTFFR.Common.Messages;
using System.Windows.Threading;

namespace RaywattOCTFFR.ViewModels
{
    public partial class RecordingSetupViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingSetupViewModel));

        private DispatcherTimer timer = new DispatcherTimer();

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        public RecordingSetupViewModel()
        {
            _log.Debug("RecordingSetupViewModel");

            Constants.CurrentPage = Constants.RecordingSetupPage;

            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += new EventHandler(CheckCatheterStatus);
            timer.Start();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PatientCase = (PatientCase)data["patientCase"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];

                if (DeviceStatus.CatheterStatus == Constants.CatheterStatusEnable)
                {
                    Next();
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            if (timer.IsEnabled)
                timer.Stop();
        }

        private void Cancel()
        {
            _log.Debug("Back");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingPresetPage) { Parameter = parameter });
        }

        private void Next()
        {
            _log.Debug("Next");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingLiveViewPage) { Parameter = parameter });
        }

        private void CheckCatheterStatus(object sender, EventArgs e)
        {
            if(DeviceStatus.CatheterStatus == Constants.CatheterStatusEnable)
                Next();
        }
    }
}
