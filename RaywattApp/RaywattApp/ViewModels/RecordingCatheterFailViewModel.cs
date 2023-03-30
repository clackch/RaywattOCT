using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using System.Windows.Threading;

namespace RaywattApp.ViewModels
{
    public partial class RecordingCatheterFailViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingCatheterFailViewModel));

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private bool _isStep1;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _nextStepCommand;
        public ICommand NextStepCommand
        {
            get { return this._nextStepCommand ?? (this._nextStepCommand = new RelayCommand(NextStep)); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next)); }
        }

        public RecordingCatheterFailViewModel()
        {
            _log.Debug("RecordingCatheterFailViewModel");

            Constants.CurrentPage = Constants.RecordingCatheterFailPage;
            
            IsStep1 = true;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            if(Patient == null || PrevStatus == null)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = this.Patient;
                parameter["prevStatus"] = this.PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
            }
        }

        private DispatcherTimer timer = new DispatcherTimer();//Test

        private void NextStep()
        {
            _log.Debug("NextStep");

            IsStep1 = false;

            //Test
            timer.Interval = TimeSpan.FromMilliseconds(2000);
            timer.Tick += new EventHandler(StepChange);
            timer.Start();
        }

        //Test
        private void StepChange(object sender, EventArgs e)
        {
            _log.Debug("StepChange : " + DeviceStatus.CatheterStatus);

            if (DeviceStatus.CatheterStatus == Constants.CatheterStatusFailed)
            {
                DeviceStatus.CatheterStatus = Constants.CatheterStatusUnlocked;
            }
            else if(DeviceStatus.CatheterStatus == Constants.CatheterStatusUnlocked)
            {
                DeviceStatus.CatheterStatus = Constants.CatheterStatusUnloaded;
            }
            else if (DeviceStatus.CatheterStatus == Constants.CatheterStatusUnloaded)
            {
                DeviceStatus.CatheterStatus = Constants.CatheterStatusDisconnected;
                timer.Stop();
            }
        }

        private void Next()
        {
            _log.Debug("Next");

            if (Patient == null || PrevStatus == null)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = this.Patient;
                parameter["prevStatus"] = this.PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingSetupPage) { Parameter = parameter });
            }
        }
    }
}
