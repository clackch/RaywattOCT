using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RaywattApp.Services
{
    public class IdleMonitorService
    {
        // TODO: Log 기능 추가

        private IDialogService? _dialogService;

        private DateTime _totalStartTime;
        private DateTime _preAlertStartTime;

        private TimeSpan _totalIdleLimit = TimeSpan.FromSeconds(30);
        private TimeSpan _preAlertLimit = TimeSpan.FromSeconds(20);

        private bool _isPreAlertShown = false;
        private bool _isLogoutPopupShown = false;
        private bool _isUserInputDetected = false;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public IdleMonitorService(IDialogService dialogService)
        {
            StartIdleMonitorLoop();
            RegisterUserActivityEvents();
            _dialogService = dialogService;
        }

        private async void StartIdleMonitorLoop()
        {
            _preAlertLimit = _totalIdleLimit - _preAlertLimit;

            _totalStartTime = DateTime.Now;
            _preAlertStartTime = DateTime.Now;

            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(1000);

                if (IsUserActive())
                {
                    if (_isPreAlertShown)
                    {
                        ClosePreAlertPopup();
                    }

                    _totalStartTime = DateTime.Now;
                    _preAlertStartTime = DateTime.Now;
                    continue;
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
                    ShowLoginScreen();
                    ShowLogoutPopup();

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
            Console.WriteLine("Checking user activity...");

            if (_isUserInputDetected)
            {
                _totalStartTime = DateTime.Now;
                _preAlertStartTime = DateTime.Now;

                _isUserInputDetected = false;

                Console.WriteLine("User activity detected.");
                return true;
            }

            return false;
        }
        private void ShowPreAlertPopup()
        {
            Console.WriteLine("Showing pre-alert popup...");
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _isPreAlertShown = true;

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = "ShowPreAlertPopup";
                parameter["message"] = "Pre-Alert: You have been idle for 8 minutes. Please take action to avoid logout";
                parameter["timer"] = _preAlertLimit;
                DialogResults result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                _isPreAlertShown = false;
            }));
            Console.WriteLine("Pre-alert popup shown.");
        }
        private void ClosePreAlertPopup()
        {
            Console.WriteLine("Closing pre-alert popup.");
            _dialogService?.CloseOpenDialog();
            _isPreAlertShown = false;
        }
        private void ShowLoginScreen()
        {
            //WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPresetPage) { });
            Console.WriteLine("Redirecting to login screen...");
        }
        private void ShowLogoutPopup()
        {
            Console.WriteLine("Logout popup shown. Please confirm to logout.");
            _isLogoutPopupShown = true;
        }

        private async Task WaitForLogoutPopupToClose()
        {
            Console.WriteLine("Waiting for logout popup to close...");
            while (_isLogoutPopupShown)
            {
                await Task.Delay(1000);
                _isLogoutPopupShown = false; // Simulate user closing the popup
            }
        }
        private void RestartIdleLoop()
        {
            Console.WriteLine("Restarting idle monitor loop...");
            _cts = new CancellationTokenSource();
            StartIdleMonitorLoop();
        }
    }
}
