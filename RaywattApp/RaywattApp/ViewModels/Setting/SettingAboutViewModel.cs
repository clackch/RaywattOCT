using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Enums;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Input;
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

        private ICommand _rjUpdateCommand;
        public ICommand RJUpdateCommand
        {
            get { return this._rjUpdateCommand ?? (this._rjUpdateCommand = new RelayCommand(RJUpdate)); }
        }

        private ICommand _cmUpdateCommand;
        public ICommand CMUpdateCommand
        {
            get { return this._cmUpdateCommand ?? (this._cmUpdateCommand = new RelayCommand(CMUpdate)); }
        }

        [ObservableProperty]
        private string _softwareName;

        [ObservableProperty]
        private string _softwareVersion;

        [ObservableProperty]
        private string _rjVersion;

        [ObservableProperty]
        private string _cmVersion;

        public SettingAboutViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingAboutViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            SoftwareName = ConfigurationManager.AppSettings.Get("SoftwareName");
            SoftwareVersion = ConfigurationManager.AppSettings.Get("SoftwareVersion");

            // 펌웨어 버전 읽기
            LoadRJVersion();
            LoadCMVersion();
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
                // TODO: Software 업데이트 로직 구현
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Software Update"];
                parameter["message"] = "Software update is not implemented yet.";

                _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            }
            catch (Exception ex)
            {
                _log.Error($"Error occurred during software update: {ex.Message}", ex);

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Error"];
                parameter["message"] = $"An error occurred during software update: {ex.Message}";
                parameter["error"] = true;

                _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            }
        }

        public void RJUpdate()
        {
            _log.Debug("RJUpdate button clicked");
            ExecuteFirmwareUpdate(UpdateType.RJ, RjVersion);
        }

        public void CMUpdate()
        {
            _log.Debug("CMUpdate button clicked");
            ExecuteFirmwareUpdate(UpdateType.CM, CmVersion);
        }

        private void ExecuteFirmwareUpdate(UpdateType updateType, string currentVersion)
        {
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

                parameter["title"] = $"{updateType} Update";
                parameter["currentVersion"] = currentVersion;
                parameter["updateType"] = updateType;

                var result = _dialogService.OpenDialog(new SoftwareUpdateDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                {
                    _log.Debug($"User selected {updateType} update");

                    Dictionary<string, object> returnData = (Dictionary<string, object>)result.DialogReturn;
                    bool shouldUpdate = (bool)returnData["shouldUpdate"];

                    if (shouldUpdate)
                    {
                        var selectedUpdate = (UpdateItem)returnData["selectedUpdate"];
                        var updateFilePath = selectedUpdate.FilePath;

                        _log.Debug($"Starting {updateType} update - File: {updateFilePath}");

                        // 펌웨어 업데이트 프로그레스 다이얼로그 표시
                        Dictionary<string, object> progressParameter = new Dictionary<string, object>();
                        progressParameter["title"] = $"{updateType} Update";
                        progressParameter["firmwareFilePath"] = updateFilePath;
                        progressParameter["updateType"] = updateType;

                        var progressDialog = _dialogService.OpenDialog(new FirmwareUpdateProgressDialogControl(), progressParameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                        // 업데이트 완료 후 결과 처리
                        if (progressDialog != null && progressDialog.DialogReturn is Dictionary<string, object> progressResult)
                        {
                            bool updateCompleted = (bool)progressResult["updateCompleted"];
                            var finalState = (FirmwareUpdateState)progressResult["finalState"];

                            _log.Debug($"{updateType} update finished - Completed: {updateCompleted}, State: {finalState}");

                            if (updateCompleted && finalState == FirmwareUpdateState.Success)
                            {
                                // 펌웨어 버전 다시 읽기
                                if (updateType == UpdateType.RJ)
                                {
                                    LoadRJVersion();
                                }
                                else if (updateType == UpdateType.CM)
                                {
                                    LoadCMVersion();
                                }
                            }
                        }
                    }
                }
                else
                {
                    _log.Debug($"User canceled {updateType} update");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error occurred during {updateType} update: {ex.Message}", ex);

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Error"];
                parameter["message"] = $"An error occurred during {updateType} update: {ex.Message}";
                parameter["error"] = true;

                _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
            }
        }

        private void LoadRJVersion()
        {
            try
            {
                RayGetRJVersion(out int major, out int minor, out int patch, out bool isBootMode);

                if (isBootMode)
                {
                    RjVersion = $"(Boot_){major}.{minor}.{patch}";
                }
                else
                {
                    RjVersion = $"{major}.{minor}.{patch}";
                }

                _log.Debug($"RJ version loaded: {RjVersion}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading RJ version: {ex.Message}", ex);
                RjVersion = "N/A";
            }
        }

        private void LoadCMVersion()
        {
            try
            {
                RayGetCMVersion(out int major, out int minor, out int patch, out bool isBootMode);

                if (isBootMode)
                {
                    CmVersion = $"(Boot_){major}.{minor}.{patch}";
                }
                else
                {
                    CmVersion = $"{major}.{minor}.{patch}";
                }

                _log.Debug($"CM version loaded: {CmVersion}");
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading CM version: {ex.Message}", ex);
                CmVersion = "N/A";
            }
        }
    }
}