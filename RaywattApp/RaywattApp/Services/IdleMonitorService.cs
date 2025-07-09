using RaywattApp.Common.Angio;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.ViewModels;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static RaywattOCT.Ray3DWrapper;

namespace RaywattApp.Services
{
    public class IdleMonitorService : ViewModelBase, IDisposable
    {
        // TODO: Log 기능 추가

        private IDialogService? _dialogService;
        private AngioManager? _angioManager;

        private DateTime _totalStartTime;
        private DateTime _preAlertStartTime;

        private TimeSpan _totalIdleLimit = TimeSpan.FromSeconds(11111110);
        private TimeSpan _preAlertLimit = TimeSpan.FromSeconds(51);

        private bool _isPreAlertShown = false;
        private bool _isLogoutPopupShown = false;
        private bool _isUserInputDetected = false;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public IdleMonitorService(IDialogService dialogService, AngioManager angioManager)
        {
            StartIdleMonitorLoop();
            RegisterUserActivityEvents();
            _dialogService = dialogService;
            _angioManager = angioManager;
        }

        private async void StartIdleMonitorLoop()
        {
            _totalStartTime = DateTime.Now;
            _preAlertStartTime = DateTime.Now;

            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(500);

                // TODO: 특정 Page에서는 아래 실행 되지 않도록 기능 추가
                //Console.WriteLine(Constants.CurrentPage.ToString());

                if (IsUserActive())
                {
                    if (!_isPreAlertShown)
                    {
                        _totalStartTime = DateTime.Now;
                        _preAlertStartTime = DateTime.Now;
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
                        ShowPreAlertPopup();
                        _isPreAlertShown = true;
                    }
                }

                if (DateTime.Now - _totalStartTime > _totalIdleLimit)
                {
                    _cts.Cancel();

                    _totalStartTime = DateTime.Now;
                    _preAlertStartTime = DateTime.Now;

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

        private bool IsUserActive()
        {
            if (_isUserInputDetected)
            {
                _totalStartTime = DateTime.Now;
                _preAlertStartTime = DateTime.Now;

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

            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = "ShowPreAlertPopup";
                parameter["message"] = "";
                parameter["timer"] = _totalIdleLimit - _preAlertLimit;
                DialogResults result = _dialogService!.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                _isPreAlertShown = false;
            }));
        }
        private void ClosePreAlertPopup()
        {
            if (!_isPreAlertShown)
            {
                return;
            }

            _dialogService?.CloseOpenDialog();
            _isPreAlertShown = false;
        }
        private void ShowLoginScreen()
        {
            CommonUtil.Exit(DeviceStatus, _angioManager);
            //WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
        }
        private void ShowLogoutPopup()
        {
            _isLogoutPopupShown = true;

            System.Windows.Application.Current.Dispatcher.InvokeAsync(new Action(() =>
           {
               Dictionary<string, object> parameter = new Dictionary<string, object>();
               parameter["title"] = "ShowLogoutPopup";
               parameter["message"] = "ShowLogoutPopup";
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
