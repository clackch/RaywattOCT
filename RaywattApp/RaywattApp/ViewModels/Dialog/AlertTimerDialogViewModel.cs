using log4net;
using RaywattApp.Common.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace RaywattApp.ViewModels.Dialog
{
    public class AlertTimerDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(AlertDialogViewModel));

        private TimeSpan _remainingTime;

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;

            if (data.TryGetValue("timer", out var timerObj))
            {
                if (timerObj is TimeSpan ts)
                {
                    _remainingTime = ts;
                    StartReducingTime();
                }
            }

            Title = data["title"].ToString();
            Message = data["message"].ToString();

            if (data.TryGetValue("error", out var errorObj) && errorObj is bool error)
                IsError = error;
            else
                IsError = false;
        }

        private void StartReducingTime()
        {
            DispatcherTimer _timer = new DispatcherTimer();

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                if (_remainingTime.TotalSeconds <= 0)
                {
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
