using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Threading;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    internal sealed partial class AlertTimerDialogViewModel : DialogViewModelBase
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

        private string _message = string.Empty;

        public override void SetParameter(object parameter)
        {
            _log.Debug("SetParameter");

            var data = parameter as Dictionary<string, object>;

            if (data == null)
            {
                _log.Debug("SetParameter: Parameter is null.");
                return;
            }

            Title = data["title"]?.ToString();
            _message = data["message"]?.ToString();

            _remainingTime = data["wait_seconds"] is TimeSpan ts ? ts : TimeSpan.FromSeconds(0);

            if (data.TryGetValue("show_button", out var obj))
            {
                OkButtonVisibility = obj is bool b ? b : false;
            }

            if (data.TryGetValue("error", out var errorObj) && errorObj is bool error)
                IsError = error;
            else
                IsError = false;

            WaitForShowDialog(0);
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

            DispatcherTimer _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            UpdateMessage();

            _timer.Tick += (s, e) =>
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

                if (_remainingTime.TotalSeconds <= 0)
                {
                    _timer.Stop();

                    if (!OkButtonVisibility)
                        _dialogService.CloseAllDialogs();
                }

                UpdateMessage();
            };

            _timer.Start();
        }

        private void UpdateMessage()
        {
            int minutes = _remainingTime.Minutes;
            int seconds = _remainingTime.Seconds;

            string timeMessage;

            if (_remainingTime.TotalMinutes >= 1)
                timeMessage = $"{minutes} minutes {seconds} seconds";
            else
                timeMessage = $"{seconds} seconds";

            Message = string.Format(CultureInfo.CurrentCulture, _message, timeMessage);

            OnPropertyChanged(nameof(Message));
        }
    }
}
