using Accord.Statistics.Kernels;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace RaywattApp.ViewModels.Dialog
{
    partial class AlertTimerDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AlertDialogViewModel));

        IDialogService _dialogService;

        public AlertTimerDialogViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        [ObservableProperty]
        bool _okButtonVisibility = true;

        private TimeSpan _remainingTime;

        public override void SetParameter(object parameter)
        {
            _log.Debug("SetParameter");

            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;

            Title = data["title"].ToString();
            Message = data["message"].ToString();
            _remainingTime = data["wait_seconds"] is TimeSpan ts ? ts : TimeSpan.FromSeconds(0);

            if (data.TryGetValue("show_button", out var okButtonVisibleObj))
            {
                if (okButtonVisibleObj is bool isVisibility)
                {
                    OkButtonVisibility = isVisibility;
                }
            }

            if (data.TryGetValue("error", out var errorObj) && errorObj is bool error)
                IsError = error;
            else
                IsError = false;

            WaitForShowDialog(2);
        }

        private void WaitForShowDialog(int waitSeconds)
        {
            _log.Debug("WaitForShowDialog");

            var delayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(waitSeconds)
            };
            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                StartReducingTime();
            };

            delayTimer.Start();
        }

        private void StartReducingTime()
        {
            _log.Debug("StartReducingTime");

            DispatcherTimer _timer = new DispatcherTimer();

            _timer.Interval = TimeSpan.FromSeconds(1);

            _timer.Tick += (s, e) =>
            {
                if (_remainingTime.TotalSeconds <= 0)
                {
                    _dialogService.CloseAllDialogs();
                    _timer.Stop();
                }
                else
                {
                    _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
                    int minutes = _remainingTime.Minutes;
                    int seconds = _remainingTime.Seconds;

                    if (_remainingTime.TotalMinutes >= 1)
                    {
                        Message = $"{minutes} minutes {seconds} seconds";
                    }
                    else
                    {
                        Message = $"{seconds} seconds";
                    }
                }
                OnPropertyChanged(nameof(Message));
            };
            _timer.Start();
        }

    }
}
