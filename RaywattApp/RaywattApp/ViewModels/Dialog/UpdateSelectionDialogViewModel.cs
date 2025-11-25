using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Enums;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Threading;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class UpdateSelectionDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UpdateSelectionDialogViewModel));

        private DispatcherTimer _timer;

        [ObservableProperty]
        private string _currentVersion = "N/A";

        [ObservableProperty]
        private string _statusMessage = "Checking USB device...";

        [ObservableProperty]
        private ObservableCollection<UpdateItem> _updateItems = new ObservableCollection<UpdateItem>();

        [ObservableProperty]
        private UpdateItem _selectedUpdate;

        [ObservableProperty]
        private UpdateType _updateType = UpdateType.Software;

        [ObservableProperty]
        private string _usbDriveName = "";

        [ObservableProperty]
        private bool _hasAvailableUpdates = true;

        [ObservableProperty]
        private bool _canUpdate = true;

        private Dictionary<string, string> _externalDriveComboBox;
        public Dictionary<string, string> ExternalDriveComboBox
        {
            get { return _externalDriveComboBox; }
            set
            {
                _externalDriveComboBox = value;
                OnPropertyChanged(nameof(ExternalDriveComboBox));
            }
        }

        private bool _isEnableExternalDrive;
        public bool IsEnableExternalDrive
        {
            get { return _isEnableExternalDrive; }
            set
            {
                _isEnableExternalDrive = value;
                OnPropertyChanged(nameof(IsEnableExternalDrive));
            }
        }

        private Dictionary<string, object> _externalDriveList;
        public Dictionary<string, object> ExternalDriveList
        {
            get { return _externalDriveList; }
            set
            {
                _externalDriveList = value;
                OnPropertyChanged(nameof(ExternalDriveList));
            }
        }

        private string _selectedExternalDrive;
        public string SelectedExternalDrive
        {
            get { return _selectedExternalDrive; }
            set
            {
                if (_selectedExternalDrive != value)
                {
                    _selectedExternalDrive = value;
                    OnPropertyChanged(nameof(SelectedExternalDrive));

                    if (!string.IsNullOrEmpty(_selectedExternalDrive))
                    {
                        LoadAndFilterUpdateItems();
                    }
                }
            }
        }

        public UpdateSelectionDialogViewModel()
        {
            _log.Debug("UpdateSelectionDialogViewModel");

            ExternalDriveComboBox = new Dictionary<string, string>();
            ExternalDriveList = new Dictionary<string, object>();

            // USB 드라이브 감지 타이머 시작
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += CheckDrive;
            _timer.Start();
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

                    if (data.ContainsKey("updateType"))
                    {
                        UpdateType = (UpdateType)data["updateType"];
                    }

                    GetDrive();
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

        private void CheckDrive(object sender, EventArgs e)
        {
            GetDrive();
        }

        private void GetDrive()
        {
            Dictionary<string, string> currExternalDrive = new Dictionary<string, string>();

            string firstExternalDrive = "";
            bool isFirstExternalDrive = true;

            ExternalDriveList.Clear();

            var searcher = new ManagementObjectSearcher(@"Select * From Win32_DiskDrive");

            foreach (var drive in searcher.Get())
            {
                var mediaType = drive["MediaType"]?.ToString();
                var interfaceType = drive["InterfaceType"]?.ToString();

                if (interfaceType == "USB" || mediaType == "Removable Media" || mediaType == "External hard disk media")
                {
                    // 디스크 드라이브에 있는 모든 파티션 반환
                    var partitionsQuery = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{drive["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                    foreach (var partition in partitionsQuery.Get())
                    {
                        // 각 파티션에 부여된 드라이브 이름 반환 (C, D, E)
                        var logicalDisksQuery = new ManagementObjectSearcher($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                        foreach (var logicalDisk in logicalDisksQuery.Get())
                        {
                            var d = new DriveInfo(logicalDisk["Name"].ToString());
                            if (d.IsReady)
                            {
                                string driveName = d.Name.Replace("\\", "");

                                currExternalDrive[driveName] = driveName;
                                long[] data = { d.TotalSize, d.AvailableFreeSpace };
                                ExternalDriveList.Add(driveName, data);

                                if (isFirstExternalDrive)
                                {
                                    firstExternalDrive = driveName;
                                    isFirstExternalDrive = false;
                                }
                            }
                        }
                    }
                }
            }

            if (ExternalDriveComboBox.Count != currExternalDrive.Count)
            {
                ExternalDriveComboBox = currExternalDrive;

                if (ExternalDriveComboBox.Count > 0)
                {
                    IsEnableExternalDrive = true;

                    if (string.IsNullOrEmpty(SelectedExternalDrive))
                    {
                        SelectedExternalDrive = firstExternalDrive;
                    }
                    else if (!ExternalDriveComboBox.ContainsKey(SelectedExternalDrive))
                    {
                        SelectedExternalDrive = firstExternalDrive;
                    }
                }
                else
                {
                    IsEnableExternalDrive = false;
                    SelectedExternalDrive = null;
                    UpdateItems.Clear();
                    HasAvailableUpdates = false;
                    StatusMessage = "No USB drive detected";
                }
            }

            if (ExternalDriveComboBox.Count == 1)
            {
                if (!ExternalDriveComboBox.ContainsKey(SelectedExternalDrive))
                {
                    SelectedExternalDrive = firstExternalDrive;
                }
            }
        }

        private void LoadAndFilterUpdateItems()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedExternalDrive))
                {
                    _log.Debug("No USB drive selected");
                    StatusMessage = "Please select a USB drive";
                    HasAvailableUpdates = false;
                    CanUpdate = false;
                    UpdateItems.Clear();
                    return;
                }

                // 선택된 드라이브에서 업데이트 목록 가져오기
                var allUpdateItems = GetUpdateItemsFromDrive(SelectedExternalDrive);
                _log.Debug($"Found {allUpdateItems.Count} {UpdateType} update items on USB drive {SelectedExternalDrive}");

                if (!allUpdateItems.Any())
                {
                    StatusMessage = $"No {UpdateType} firmware found on selected drive";
                    HasAvailableUpdates = false;
                    CanUpdate = false;
                    UpdateItems.Clear();
                    return;
                }

                // 현재 버전 객체 생성
                var currentVersion = new UpdateItem(CurrentVersion, "", DateTime.Now);

                // 현재보다 새로운 버전만 필터링
                var newerVersions = allUpdateItems
                    .Where(item => item.IsNewerThan(currentVersion))
                    .OrderByDescending(item => item.Version)
                    .ToList();

                _log.Debug($"Available updates (newer than current): {newerVersions.Count}");

                UpdateItems.Clear();

                if (newerVersions.Any())
                {
                    foreach (var item in newerVersions)
                    {
                        UpdateItems.Add(item);
                    }

                    // 가장 최신 버전을 자동 선택
                    SelectedUpdate = UpdateItems.First();
                    HasAvailableUpdates = true;
                    CanUpdate = true;
                    StatusMessage = string.Empty;

                    _log.Debug($"Loaded {newerVersions.Count} newer {UpdateType} firmware versions");
                }
                else
                {
                    HasAvailableUpdates = false;
                    CanUpdate = false;
                    StatusMessage = "Latest Version";
                    _log.Debug($"No newer {UpdateType} firmware versions available");
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error loading {UpdateType} update items: {ex.Message}", ex);
                StatusMessage = "An error occurred";
                HasAvailableUpdates = false;
                CanUpdate = false;
            }
        }

        private List<UpdateItem> GetUpdateItemsFromDrive(string driveName)
        {
            try
            {
                // UpdateType에 따라 폴더 이름 결정
                string folderName = UpdateType switch
                {
                    UpdateType.Software => "Software",
                    UpdateType.RJ => "Firmware\\RJ",
                    UpdateType.CM => "Firmware\\CM",
                    _ => "Firmware" // 기본값
                };

                string updatePath = Path.Combine(driveName, folderName);

                if (!Directory.Exists(updatePath))
                {
                    _log.Warn($"{folderName} folder not found at: {updatePath}");
                    return new List<UpdateItem>();
                }

                _log.Debug($"{folderName} folder found at: {updatePath}");

                var updateItems = new List<UpdateItem>();
                var directories = Directory.GetDirectories(updatePath);

                foreach (var dir in directories)
                {
                    try
                    {
                        string dirName = Path.GetFileName(dir);
                        var binFiles = Directory.GetFiles(dir, "*.bin");

                        if (binFiles.Length == 0)
                        {
                            _log.Warn($"No .bin files found in {dirName}, skipping");
                            continue;
                        }

                        // 첫 번째 .bin 파일의 전체 경로 사용
                        string binFilePath = binFiles[0];
                        _log.Debug($"Found {UpdateType} firmware binary: {binFilePath}");

                        updateItems.Add(new UpdateItem(
                            dirName,
                            binFilePath,
                            System.IO.File.GetLastWriteTime(binFilePath)
                        ));
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Error processing directory {dir}: {ex.Message}");
                    }
                }

                _log.Debug($"Total {UpdateType} update items found: {updateItems.Count}");
                return updateItems;
            }
            catch (Exception ex)
            {
                _log.Error($"Error getting {UpdateType} update items from drive: {ex.Message}", ex);
                return new List<UpdateItem>();
            }
        }

        partial void OnSelectedUpdateChanged(UpdateItem value)
        {
            if (value != null)
            {
                CanUpdate = true;
                _log.Debug($"Selected {UpdateType} update changed: v{value.Version}");
            }
            else
            {
                CanUpdate = false;
            }
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            if (SelectedUpdate == null)
            {
                _log.Warn($"No {UpdateType} update selected");
                return;
            }

            _log.Debug($"User confirmed update to {UpdateType} version {SelectedUpdate.Version}");

            // 타이머 정지
            if (_timer != null && _timer.IsEnabled)
            {
                _timer.Stop();
            }

            DialogResults dialogResults = new DialogResults
            {
                DialogAnswer = DialogResults.Answer.Yes,
                DialogReturn = new Dictionary<string, object>
                {
                    { "shouldUpdate", true },
                    { "selectedUpdate", SelectedUpdate },
                    { "updateType", UpdateType },
                    { "usbDriveName", SelectedExternalDrive }
                }
            };

            CloseDialogWithResult(dialog, dialogResults);
        }

        protected override void AnswerNo(IDialogWindow dialog)
        {
            _log.Debug($"User canceled {UpdateType} update");

            // 타이머 정지
            if (_timer != null && _timer.IsEnabled)
            {
                _timer.Stop();
            }

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