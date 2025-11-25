using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Enums;
using RaywattApp.Models;
using System;
using System.Windows;
using System.Windows.Input;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FirmwareUpdateProgressDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FirmwareUpdateProgressDialogViewModel));

        private FWProgressCallback _progressCallback;
        private FWStatusCallback _statusCallback;

        private bool _updateCompleted = false;

        [ObservableProperty]
        private int _progress = 0;

        [ObservableProperty]
        private string _statusMessage = "Preparing firmware update...";

        [ObservableProperty]
        private bool _canCancel = true;

        [ObservableProperty]
        private bool _showCloseButton = false;

        [ObservableProperty]
        private FirmwareUpdateState _currentState = FirmwareUpdateState.Idle;

        [ObservableProperty]
        private UpdateType _updateType = UpdateType.RJ;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(CancelUpdate)); }
        }

        public FirmwareUpdateProgressDialogViewModel()
        {
            _log.Debug("FirmwareUpdateProgressDialogViewModel");

            // Initialize callbacks
            _progressCallback = OnProgressCallback;
            _statusCallback = OnStatusCallback;
        }

        public override void SetParameter(object parameter)
        {
            if (parameter is System.Collections.Generic.Dictionary<string, object> data)
            {
                try
                {
                    if (data.ContainsKey("title"))
                    {
                        Title = data["title"].ToString();
                    }

                    if (data.ContainsKey("updateType"))
                    {
                        UpdateType = (UpdateType)data["updateType"];
                        _log.Debug($"Update type set to: {UpdateType}");
                    }

                    RegisterCallbacks();

                    if (data.ContainsKey("firmwareFilePath"))
                    {
                        string firmwareFilePath = data["firmwareFilePath"].ToString();
                        _log.Debug($"{UpdateType} firmware file path received: {firmwareFilePath}");

                        StartFirmwareUpdate(firmwareFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error setting parameters: {ex.Message}", ex);
                    StatusMessage = $"Error: {ex.Message}";
                    CanCancel = false;
                    ShowCloseButton = true;
                    CurrentState = FirmwareUpdateState.Failed;
                }
            }
        }

        private void RegisterCallbacks()
        {
            switch (UpdateType)
            {
                case UpdateType.RJ:
                    RayRJSetProgressCallback(_progressCallback);
                    RayRJSetStatusCallback(_statusCallback);
                    _log.Debug("RJ callbacks registered");
                    break;

                case UpdateType.CM:
                    RayCMSetProgressCallback(_progressCallback);
                    RayCMSetStatusCallback(_statusCallback);
                    _log.Debug("CM callbacks registered");
                    break;

                default:
                    _log.Warn($"No callbacks to register for UpdateType: {UpdateType}");
                    break;
            }
        }

        private void OnProgressCallback(int progress)
        {
            _log.Debug($"Progress callback received: {progress}%");

            // Marshal to UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Progress = progress;
            });
        }

        private void OnStatusCallback(int state)
        {
            var updateState = (FirmwareUpdateState)state;
            _log.Debug($"Status callback received: {updateState}");

            // Marshal to UI thread
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentState = updateState;

                switch (updateState)
                {
                    case FirmwareUpdateState.Idle:
                        StatusMessage = "Preparing firmware update...";
                        CanCancel = true;
                        break;

                    case FirmwareUpdateState.Downloading:
                        StatusMessage = "Downloading firmware to device...";
                        CanCancel = true;
                        break;

                    case FirmwareUpdateState.Success:
                        StatusMessage = "Firmware update completed successfully.";
                        CanCancel = false;
                        ShowCloseButton = true;
                        _updateCompleted = true;
                        break;

                    case FirmwareUpdateState.Failed:
                        StatusMessage = "Firmware update failed.";
                        CanCancel = false;
                        ShowCloseButton = true;
                        break;

                    case FirmwareUpdateState.Cancelled:
                        StatusMessage = "Firmware update cancelled by user.";
                        CanCancel = false;
                        ShowCloseButton = true;
                        break;
                }
            });
        }

        public void StartFirmwareUpdate(string firmwareFilePath)
        {
            _log.Debug($"Starting {UpdateType} update: {firmwareFilePath}");

            try
            {
                StatusMessage = $"Starting {UpdateType} download...";
                Progress = 0;
                CurrentState = FirmwareUpdateState.Idle;

                bool success = false;

                switch (UpdateType)
                {
                    case UpdateType.RJ:
                        success = RayRJStartDownload(firmwareFilePath);
                        break;

                    case UpdateType.CM:
                        success = RayCMStartDownload(firmwareFilePath);
                        break;

                    default:
                        _log.Error($"Unsupported UpdateType for firmware download: {UpdateType}");
                        break;
                }

                if (!success)
                {
                    _log.Error($"Failed to start {UpdateType} download");
                    StatusMessage = $"Failed to start {UpdateType} update.\nPlease check device connection.";
                    CanCancel = false;
                    ShowCloseButton = true;
                    CurrentState = FirmwareUpdateState.Failed;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception during {UpdateType} update: {ex.Message}", ex);
                StatusMessage = $"Error: {ex.Message}";
                CanCancel = false;
                ShowCloseButton = true;
                CurrentState = FirmwareUpdateState.Failed;
            }
        }

        private void CancelUpdate()
        {
            _log.Debug($"User requested {UpdateType} update cancellation");

            if (CanCancel)
            {
                try
                {
                    bool cancelled = false;

                    switch (UpdateType)
                    {
                        case UpdateType.RJ:
                            cancelled = RayRJCancelDownload();
                            break;

                        case UpdateType.CM:
                            cancelled = RayCMCancelDownload();
                            break;

                        default:
                            _log.Warn($"No cancel method for UpdateType: {UpdateType}");
                            break;
                    }

                    if (cancelled)
                    {
                        _log.Debug($"{UpdateType} update cancelled successfully");
                        StatusMessage = $"Cancelling {UpdateType} update...";
                        CanCancel = false;
                    }
                    else
                    {
                        _log.Warn($"Failed to cancel {UpdateType} update");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error cancelling {UpdateType} update: {ex.Message}", ex);
                }
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("Close button clicked");

            DialogResults dialogResults = new DialogResults
            {
                DialogAnswer = _updateCompleted ? DialogResults.Answer.Yes : DialogResults.Answer.No,
                DialogReturn = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "updateCompleted", _updateCompleted },
                    { "finalState", CurrentState }
                }
            };

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
