using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FirmwareUpdateProgressDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FirmwareUpdateProgressDialogViewModel));

        // Callback delegates - stored as member variables to prevent GC
        private FWProgressCallback _progressCallback;
        private FWStatusCallback _statusCallback;

        private IDialogWindow _currentDialog;
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

            // Register callbacks with DLL
            RayFWSetProgressCallback(_progressCallback);
            RayFWSetStatusCallback(_statusCallback);
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

                    if (data.ContainsKey("firmwareFilePath"))
                    {
                        string firmwareFilePath = data["firmwareFilePath"].ToString();
                        _log.Debug($"Firmware file path received: {firmwareFilePath}");

                        // Start firmware update immediately
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
                        StatusMessage = "Firmware update completed successfully!\nPlease restart the device.";
                        CanCancel = false;
                        ShowCloseButton = true;
                        _updateCompleted = true;
                        break;

                    case FirmwareUpdateState.Failed:
                        StatusMessage = "Firmware update failed.\nPlease try again or contact support.";
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
            _log.Debug($"Starting firmware update: {firmwareFilePath}");

            try
            {
                StatusMessage = "Starting firmware download...";
                Progress = 0;
                CurrentState = FirmwareUpdateState.Idle;

                bool success = RayFWStartDownload(firmwareFilePath);

                if (!success)
                {
                    _log.Error("Failed to start firmware download");
                    StatusMessage = "Failed to start firmware update.\nPlease check device connection.";
                    CanCancel = false;
                    ShowCloseButton = true;
                    CurrentState = FirmwareUpdateState.Failed;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Exception during firmware update: {ex.Message}", ex);
                StatusMessage = $"Error: {ex.Message}";
                CanCancel = false;
                ShowCloseButton = true;
                CurrentState = FirmwareUpdateState.Failed;
            }
        }

        private void CancelUpdate()
        {
            _log.Debug("User requested firmware update cancellation");

            if (CanCancel)
            {
                try
                {
                    bool cancelled = RayFWCancelDownload();
                    if (cancelled)
                    {
                        _log.Debug("Firmware update cancelled successfully");
                        StatusMessage = "Cancelling firmware update...";
                        CanCancel = false;
                    }
                    else
                    {
                        _log.Warn("Failed to cancel firmware update");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error cancelling firmware update: {ex.Message}", ex);
                }
            }
        }

        protected override void AnswerNo(IDialogWindow dialog)
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
