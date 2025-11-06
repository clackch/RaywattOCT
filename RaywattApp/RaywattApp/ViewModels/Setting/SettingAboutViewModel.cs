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
using System.IO;
using System.Linq;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingAboutViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingAboutViewModel));

        private readonly SqlManager _sqlManager;
        private readonly IDialogService _dialogService;

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

            SoftwareName = ConfigurationManager.AppSettings.Get("SoftwareName");
            SoftwareVersion = ConfigurationManager.AppSettings.Get("SoftwareVersion");

            // 펌웨어 버전 읽기
            LoadFirmwareVersion();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private bool HasUsbDrive()
        {
            try
            {
                var usbDrives = DriveInfo.GetDrives()
                    .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                    .ToList();

                bool hasUsb = usbDrives.Any();
                _log.Debug($"Has USB drive: {hasUsb}");
                return hasUsb;
            }
            catch (Exception ex)
            {
                _log.Error($"Error checking for USB drive: {ex.Message}", ex);
                return false;
            }
        }

        public void SoftwareUpdate()
        {
            _log.Debug("SoftwareUpdate button clicked");

            try
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();

                if (!HasUsbDrive())
                {
                    _log.Debug("USB drive not found");

                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = "USB drive not found. Please connect a USB drive and try again.";

                    _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    return;
                }

                _log.Debug("USB drive detected");

                parameter["title"] = _l10n["Firmware Update"];
                parameter["currentVersion"] = FirmwareVersion;

                var result = _dialogService.OpenDialog(new SoftwareUpdateDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                {
                    _log.Debug("User selected firmware update");

                    Dictionary<string, object> returnData = (Dictionary<string, object>)result.DialogReturn;
                    bool shouldUpdate = (bool)returnData["shouldUpdate"];

                    if (shouldUpdate)
                    {
                        var selectedFirmware = (UpdateItem)returnData["selectedFirmware"];
                        var firmwareFilePath = selectedFirmware.FilePath;

                        _log.Debug($"Starting firmware update - File: {firmwareFilePath}");

                        // 펌웨어 업데이트 프로그레스 다이얼로그 표시
                        Dictionary<string, object> progressParameter = new Dictionary<string, object>();
                        progressParameter["title"] = _l10n["Firmware Update"];
                        progressParameter["firmwareFilePath"] = firmwareFilePath;

                        var progressDialog = _dialogService.OpenDialog(new FirmwareUpdateProgressDialogControl(), progressParameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                        // 업데이트 완료 후 결과 처리
                        if (progressDialog != null && progressDialog.DialogReturn is Dictionary<string, object> progressResult)
                        {
                            bool updateCompleted = (bool)progressResult["updateCompleted"];
                            var finalState = (FirmwareUpdateState)progressResult["finalState"];

                            _log.Debug($"Firmware update finished - Completed: {updateCompleted}, State: {finalState}");

                            if (updateCompleted && finalState == FirmwareUpdateState.Success)
                            {
                                // 펌웨어 버전 다시 읽기
                                LoadFirmwareVersion();
                            }
                        }
                    }
                }
                else
                {
                    _log.Debug("User canceled firmware update");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error occurred during firmware update: {ex.Message}", ex);

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Error"];
                parameter["message"] = $"An error occurred during firmware update: {ex.Message}";
                parameter["error"] = true;

                _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            }
        }

        private void LoadFirmwareVersion()
        {
            try
            {
                RayGetRJFirmwareVersion(out int major, out int minor, out int patch, out bool isBootMode);

                if (isBootMode)
                {
                    FirmwareVersion = $"(Boot_){major}.{minor}.{patch}";
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
    }
}