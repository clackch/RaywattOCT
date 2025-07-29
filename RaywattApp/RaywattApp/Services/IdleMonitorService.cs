using log4net;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.ViewModels.Dialog;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace RaywattApp.Services
{
    public class IdleMonitorService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(IdleMonitorService));

        private IDialogService? _dialogService;
        private AngioManager? _angioManager;
        private SqlManager _sqlManager;
        private IPasswordService _passwordService;

        private DateTime _totalStartTime;
        private DateTime _preAlertStartTime;

        private TimeSpan _totalIdleLimit = TimeSpan.FromMinutes(60);
        private TimeSpan _preAlertLimit = TimeSpan.FromMinutes(5);

        private bool _isPreAlertShown = false;
        private bool _isLogoutPopupShown = false;
        private bool _isUserInputDetected = false;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        List<string> _skipPages = new List<string>()
        {
            Constants.OutsetLoginPage,
            Constants.OutsetLoadingPage,
        };

        List<Type> _skipDialogs = new List<Type>()
        {
            typeof(FileCopyDialogViewModel),
        };

        public TimeSpan TotalIdleLimit { private get => _totalIdleLimit; set => _totalIdleLimit = value; }
        public TimeSpan PreAlertLimit { private get => _preAlertLimit; set => _preAlertLimit = value; }

        public IdleMonitorService(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager, IPasswordService passwordService)
        {
            _log.Debug("IdleMonitorService");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _angioManager = angioManager;
            _passwordService = passwordService;

            Init();
        }

        private void Init()
        {
            StartIdleMonitorLoop();
            RegisterUserActivityEvents();
            ApplyLogoutTimeSettings();
        }
        private void ApplyLogoutTimeSettings()
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "LogoutTime";
            IList<Configuration> logOutTimes = _sqlManager.SelectConfiguration(sqlParameters);

            foreach (var time in logOutTimes)
            {
                var key = time.Key;

                if (key == "TotalTime")
                {
                    _totalIdleLimit = TimeSpan.FromMinutes(Convert.ToDouble(time.Value));
                }
                else if (key == "PreTime")
                {
                    _preAlertLimit = TimeSpan.FromMinutes(Convert.ToDouble(time.Value));
                }
            }
        }
        private void ResetTimers()
        {
            _totalStartTime = DateTime.Now;
            _preAlertStartTime = DateTime.Now;
        }
        private async void StartIdleMonitorLoop()
        {
            ResetTimers();

            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(1000);

                if (ShouldSkipIdleCheck()) continue;
                if (SholdSkipDialogCheck()) continue;

                if (IsUserActive())
                {
                    if (!_isPreAlertShown)
                    {
                        ResetTimers();
                    }
                    else
                    {
                        continue;
                    }
                }

                if (DateTime.Now - _preAlertStartTime > _preAlertLimit)
                {
                    if (!_isPreAlertShown)
                    {
                        _log.Debug($"Pre Alert Limit: {_preAlertLimit}");

                        ShowPreAlertPopup();
                        _isPreAlertShown = true;
                    }
                }

                if (DateTime.Now - _totalStartTime > _totalIdleLimit)
                {
                    _log.Debug($"Total Idle Limit: {_totalIdleLimit}, Pre Alert Limit: {_preAlertLimit}");

                    _cts.Cancel();

                    ResetTimers();
                    ClosePreAlertPopup();
                    ShowLogoutPopup();
                    ShowLoginScreen();

                    await WaitForLogoutPopupToClose();

                    RestartIdleLoop();
                    break;
                }
            }
        }
        private void RegisterUserActivityEvents()
        {
            InputManager.Current.PreNotifyInput += (sender, e) =>
            {
                if (e.StagingItem.Input is MouseEventArgs || e.StagingItem.Input is KeyEventArgs)
                {
                    _isUserInputDetected = true;
                }
            };
        }
        private bool ShouldSkipIdleCheck()
        {
            return _skipPages.Contains(Constants.CurrentPage);
        }
        private bool SholdSkipDialogCheck()
        {
            var openDialogs = _dialogService!.GetOpenDialogs();

            return openDialogs.Any(dialog =>
            {
                var dataContextType = dialog.DataContext?.GetType();
                return dataContextType != null && _skipDialogs.Contains(dataContextType);
            });
        }
        private bool IsUserActive()
        {
            if (_isPreAlertShown)
            {
                return false;
            }

            if (_isUserInputDetected)
            {
                ResetTimers();
                _isUserInputDetected = false;

                return true;
            }

            return false;
        }
        private void ShowPreAlertPopup()
        {
            if (_isPreAlertShown)
            {
                return;
            }

            _isPreAlertShown = true;
            _log.Debug($"Showing Pre Alert Popup with remaining time: {_totalIdleLimit - _preAlertLimit} seconds");

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _passwordService.ShowTimerAlert("Session Timeout", "No activity.\r\nLogging out soon.", true, _totalIdleLimit - _preAlertLimit);
                _isPreAlertShown = false;
                _log.Debug($"Pre Alert Popup closed");
            }));
        }
        private void ClosePreAlertPopup()
        {
            if (!_isPreAlertShown)
            {
                return;
            }

            _dialogService?.CloseAllDialogs();
            _isPreAlertShown = false;
        }
        private void ShowLoginScreen()
        {
            CommonUtil.Exit(ViewModelBase.DeviceStatus, _angioManager);
        }
        private void ShowLogoutPopup()
        {
            _isLogoutPopupShown = true;

            System.Windows.Application.Current.Dispatcher.InvokeAsync(new Action(() =>
           {
               Dictionary<string, object> parameter = new Dictionary<string, object>();
               parameter["title"] = "Logout";
               parameter["message"] = "Session expired due to inactivity";
               DialogResults result = _dialogService!.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

               _isLogoutPopupShown = false;
           }));
        }
        private async Task WaitForLogoutPopupToClose()
        {
            while (_isLogoutPopupShown)
            {
                await Task.Delay(1000);
            }
        }
        private void RestartIdleLoop()
        {
            _cts = new CancellationTokenSource();
            StartIdleMonitorLoop();
        }
    }
}
