using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System.Configuration;
using System.Windows.Input;
using RaywattApp.Models;
using System;
using System.Windows;
using System.Windows.Interop;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingAboutViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingAboutViewModel));

        private readonly SqlManager _sqlManager;
        private readonly UsbDetectionService _usbDetectionService;
        private IDialogService _dialogService;
        private IntPtr _windowHandle;
        private HwndSource _hwndSource;

        private ICommand _softwareUpdateCommand;
        public ICommand SoftwareUpdateCommand
        {
            get { return this._softwareUpdateCommand ?? (this._softwareUpdateCommand = new RelayCommand(SoftwareUpdate)); }
        }

        [ObservableProperty]
        private string _softwareName;

        [ObservableProperty]
        private string _softwareVersion;

        [ObservableProperty]
        private string _firmwareVersion;

        public SettingAboutViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingAboutViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _usbDetectionService = new UsbDetectionService();

            SoftwareName = ConfigurationManager.AppSettings.Get("SoftwareName");
            SoftwareVersion = ConfigurationManager.AppSettings.Get("SoftwareVersion");
            
            // 펌웨어 버전 읽기
            LoadFirmwareVersion();

            // USB 이벤트 등록
            _usbDetectionService.DeviceArrived += OnUsbDeviceArrived;
            _usbDetectionService.DeviceRemoved += OnUsbDeviceRemoved;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
            RegisterUsbDetection();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            UnregisterUsbDetection();
        }

        private void RegisterUsbDetection()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    var windowHelper = new WindowInteropHelper(mainWindow);
                    _windowHandle = windowHelper.Handle;
                    
                    if (_windowHandle != IntPtr.Zero)
                    {
                        _usbDetectionService.RegisterForDeviceNotification(_windowHandle);
                        
                        // Windows 메시지 후킹 추가
                        _hwndSource = HwndSource.FromHwnd(_windowHandle);
                        if (_hwndSource != null)
                        {
                            _hwndSource.AddHook(WndProc);
                            _log.Debug("USB device notification registered successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error registering USB detection: {ex.Message}", ex);
            }
        }

        private void UnregisterUsbDetection()
        {
            try
            {
                if (_hwndSource != null)
                {
                    _hwndSource.RemoveHook(WndProc);
                    _hwndSource = null;
                    _log.Debug("USB device notification unregistered");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error unregistering USB detection: {ex.Message}", ex);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // USB 장치 변경 메시지 처리
            _usbDetectionService.ProcessWindowMessage(msg, wParam, lParam);
            return IntPtr.Zero;
        }

        private void OnUsbDeviceArrived()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _log.Debug("USB device arrived event received in SettingAboutViewModel");
                // 필요시 UI 업데이트나 추가 로직 처리
            });
        }

        private void OnUsbDeviceRemoved()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _log.Debug("USB device removed event received in SettingAboutViewModel");
                // 필요시 UI 업데이트나 추가 로직 처리
            });
        }

        public void SoftwareUpdate()
        {
            _log.Debug("SoftwareUpdate button clicked");

            try
            {
                bool hasUsbDrive = _usbDetectionService.HasUsbDrive();

                if (hasUsbDrive)
                {
                    _log.Debug("USB drive detected");

                    // Show modal dialog if USB drive is available
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Software Update"];

                    var result = _dialogService.OpenDialog(new SoftwareUpdateDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                    if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                    {
                        _log.Debug("User selected software update");
                        
                        Dictionary<string, object> returnData = (Dictionary<string, object>)result.DialogReturn;
                        bool shouldUpdate = (bool)returnData["shouldUpdate"];
                        
                        if (shouldUpdate)
                        {
                            //var updateItems = returnData["updateItems"];
                            var usbDriveName = returnData["usbDriveName"].ToString();
                            
                            _log.Debug($"Starting update - USB Drive: {usbDriveName}");
                            
                            // TODO[haeun]: 여기서 펌웨어 업데이트 또는 소프트웨어 업데이트 작업을 수행
                            
                            Dictionary<string, object> successParameter = new Dictionary<string, object>();
                            successParameter["title"] = _l10n["Information"];
                            successParameter["message"] = _l10n["Software update initiated successfully"];
                            _dialogService.OpenDialog(new AlertDialogControl(), successParameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                        }
                    }
                    else
                    {
                        _log.Debug("User canceled software update");
                    }
                }
                else
                {
                    _log.Debug("USB drive not found");

                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = "USB drive not found. Please connect a USB drive and try again.";
                    
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error occurred during software update: {ex.Message}", ex);
                
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Error"];
                parameter["message"] = $"An error occurred during software update: {ex.Message}";
                parameter["error"] = true;
                
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            }
        }

        private void LoadFirmwareVersion()
        {
            try
            {
                RayGetRJFirmwareVersion(out int major, out int minor, out int patch, out bool isBootMode);
                
                if (isBootMode)
                {
                    FirmwareVersion = $"(Boot Mode) {major}.{minor}.{patch}";
                }
                else
                {
                    FirmwareVersion = $"{major}.{minor}.{patch}";
                }
                
                _log.Debug($"Firmware version loaded: {FirmwareVersion}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading firmware version: {ex.Message}", ex);
                FirmwareVersion = "N/A";
            }
        }

        // IDisposable 패턴 구현하여 리소스 정리
        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnregisterUsbDetection();
                    
                    if (_usbDetectionService != null)
                    {
                        _usbDetectionService.DeviceArrived -= OnUsbDeviceArrived;
                        _usbDetectionService.DeviceRemoved -= OnUsbDeviceRemoved;
                    }
                }
                _disposed = true;
            }
        }

        ~SettingAboutViewModel()
        {
            Dispose(false);
        }
    }
}
