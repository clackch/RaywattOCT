using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class SoftwareUpdateDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SoftwareUpdateDialogViewModel));

        private readonly UsbDetectionService _usbDetectionService;

        [ObservableProperty]
        private string _currentVersion = "N/A";

        [ObservableProperty]
        private string _statusMessage = "Checking USB device...";

        [ObservableProperty]
        private ObservableCollection<UpdateItem> _firmwareUpdateItems = new ObservableCollection<UpdateItem>();

        [ObservableProperty]
        private UpdateItem _selectedFirmware;

        [ObservableProperty]
        private string _usbDriveName = "";

        [ObservableProperty]
        private bool _hasAvailableUpdates = true;

        [ObservableProperty]
        private bool _canUpdate = true;

        public SoftwareUpdateDialogViewModel()
        {
            _log.Debug("SoftwareUpdateDialogViewModel");
            _usbDetectionService = new UsbDetectionService();
        }

        public override void SetParameter(object parameter)
        {
            if (parameter is Dictionary<string, object> data)
            {
                try
                {
                    if (data.ContainsKey("title"))
                    {
                        Title = data["title"].ToString();
                    }

                    if (data.ContainsKey("currentVersion"))
                    {
                        CurrentVersion = data["currentVersion"].ToString();
                    }

                    LoadAndFilterFirmwareItems();
                }
                catch (Exception ex)
                {
                    _log.Error($"Error setting parameters: {ex.Message}", ex);
                    StatusMessage = "An error occurred";
                    HasAvailableUpdates = false;
                    CanUpdate = false;
                }
            }
        }

        private void LoadAndFilterFirmwareItems()
        {
            try
            {
                // USB에서 펌웨어 목록 가져오기
                var allFirmwareItems = _usbDetectionService.GetUsbFirmwareItems();
                _log.Debug($"Found {allFirmwareItems.Count} firmware items on USB");

                if (!allFirmwareItems.Any())
                {
                    StatusMessage = "Latest Version";
                    HasAvailableUpdates = false;
                    CanUpdate = false;
                    return;
                }

                // 현재 버전 객체 생성
                var currentFirmware = new UpdateItem(CurrentVersion, "", DateTime.Now);

                // 현재보다 새로운 버전만 필터링
                var newerVersions = allFirmwareItems
                    .Where(item => item.IsNewerThan(currentFirmware))
                    .OrderByDescending(item => item.Version)
                    .ToList();

                _log.Debug($"Available updates (newer than current): {newerVersions.Count}");

                FirmwareUpdateItems.Clear();

                if (newerVersions.Any())
                {
                    foreach (var item in newerVersions)
                    {
                        FirmwareUpdateItems.Add(item);
                    }

                    // 가장 최신 버전을 자동 선택
                    SelectedFirmware = FirmwareUpdateItems.First();
                    HasAvailableUpdates = true;
                    CanUpdate = true;
                    StatusMessage = string.Empty;

                    _log.Debug($"Loaded {newerVersions.Count} newer firmware versions");
                }
                else
                {
                    HasAvailableUpdates = false;
                    CanUpdate = false;
                    StatusMessage = "Latest Version";
                    _log.Debug("No newer firmware versions available");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading firmware items: {ex.Message}", ex);
                StatusMessage = "An error occurred";
                HasAvailableUpdates = false;
                CanUpdate = false;
            }
        }

        partial void OnSelectedFirmwareChanged(UpdateItem value)
        {
            if (value != null)
            {
                CanUpdate = true;
                _log.Debug($"Selected firmware changed: v{value.Version}");
            }
            else
            {
                CanUpdate = false;
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            if (SelectedFirmware == null)
            {
                _log.Warn("No firmware selected for update");
                return;
            }

            _log.Debug($"User confirmed update to firmware version {SelectedFirmware.Version}");

            DialogResults dialogResults = new DialogResults
            {
                DialogAnswer = DialogResults.Answer.Yes,
                DialogReturn = new Dictionary<string, object>
                {
                    { "shouldUpdate", true },
                    { "selectedFirmware", SelectedFirmware },
                    { "usbDriveName", UsbDriveName }
                }
            };

            CloseDialogWithResult(dialog, dialogResults);
        }

        protected override void AnswerNo(IDialogWindow dialog)
        {
            _log.Debug("User canceled firmware update");

            DialogResults dialogResults = new DialogResults
            {
                DialogAnswer = DialogResults.Answer.No,
                DialogReturn = new Dictionary<string, object>
                {
                    { "shouldUpdate", false }
                }
            };

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}