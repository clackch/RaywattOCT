using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;

namespace RaywattApp.Common.File
{
    public partial class FileExportStep2Base : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2Base));

        protected IDialogService _dialogService;

        [ObservableProperty]
        protected FileExport _fileExport;

        [ObservableProperty]
        protected IList<PatientCase> _patientCases;

        [ObservableProperty]
        private double _exportSize;

        [ObservableProperty]
        private string _exportSizeUnit;

        protected DispatcherTimer timer = new DispatcherTimer();

        [ObservableProperty]
        private Dictionary<string, string> _externalDriveComboBox;

        [ObservableProperty]
        private bool isEnableExternalDrive;

        protected bool isExternalDriveInit;

        private string _selectedExternalDrive;
        public string SelectedExternalDrive
        {
            get { return _selectedExternalDrive; }
            set
            {
                if (_selectedExternalDrive != value)
                {
                    _selectedExternalDrive = value;
                    FileExport.ExternalDrive = _selectedExternalDrive;
                    if (!isExternalDriveInit)
                        FileExport.ExternalDrivePath = "";

                    if (value == null)
                    {
                        ExternalDriveTotalSize = 0;
                        ExternalDriveAvailableFreeSpace = 0;
                        _externalDriveAvailableFreeSpaceByte = 0;
                    }
                    else
                    {
                        foreach (var item in ExternalDriveList)
                        {
                            if (value.Equals(item.Key))
                            {
                                ExternalDriveTotalSize = CommonUtil.ByteToGB(((long[])item.Value)[0]);
                                ExternalDriveAvailableFreeSpace = CommonUtil.ByteToGB(((long[])item.Value)[1]);
                                _externalDriveAvailableFreeSpaceByte = ((long[])item.Value)[1];
                                break;
                            }
                        }
                    }

                    OnPropertyChanged(nameof(SelectedExternalDrive));
                }
            }
        }

        [ObservableProperty]
        private Dictionary<string, object> _externalDriveList;

        [ObservableProperty]
        private double _externalDriveTotalSize;

        [ObservableProperty]
        private double _externalDriveAvailableFreeSpace;

        private double _exportSizeByte;
        private double _externalDriveAvailableFreeSpaceByte;

        private ICommand _externalDrivePathCommand;
        public ICommand ExternalDrivePathCommand
        {
            get { return this._externalDrivePathCommand ?? (this._externalDrivePathCommand = new RelayCommand(ExternalDrivePath)); }
        }

        public FileExportStep2Base(IDialogService dialogService)
        {
            _log.Debug("FileExportBase");

            _dialogService = dialogService;

            ExternalDriveComboBox = new Dictionary<string, string>();
            ExternalDriveList = new Dictionary<string, object>();

            isExternalDriveInit = true;
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(CheckDrive);
            timer.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            if (timer.IsEnabled)
                timer.Stop();
        }

        protected override void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage(Constants.FileExportStep1Page) { Parameter = FileExport });
        }

        protected override void Cancel()
        {
            _log.Debug("Cancel");

            Close();
        }

        protected void Close()
        {
            if (timer.IsEnabled)
                timer.Stop();

            CloseDialog();
        }

        private void CheckDrive(object sender, EventArgs e)
        {
            GetDrive();
        }

        protected void GetDrive()
        {
            Dictionary<string, string> currExternalDrive = new Dictionary<string, string>();

            string firstExternalDrive = "";
            bool isFirstExternalDrive = true;

            ExternalDriveList.Clear();

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    if (d.DriveType == DriveType.Removable)
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

            if (ExternalDriveComboBox.Count != currExternalDrive.Count)
            {
                ExternalDriveComboBox = currExternalDrive;

                if (ExternalDriveComboBox.Count > 0)
                {
                    IsEnableExternalDrive = true;

                    if (String.IsNullOrEmpty(FileExport.ExternalDrive))
                    {
                        SelectedExternalDrive = firstExternalDrive;
                    }
                    else
                    {
                        SelectedExternalDrive = FileExport.ExternalDrive;
                    }
                    isExternalDriveInit = false;
                }
                else
                {
                    IsEnableExternalDrive = false;
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

        private void ExternalDrivePath()
        {
            _log.Debug("ExternalDrivePath");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["externalDrive"] = FileExport.ExternalDrive;
            parameter["externalDrivePath"] = FileExport.ExternalDrivePath;
            var result = _dialogService.OpenDialog(new FileFolderBrowseDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                FileExport.ExternalDrivePath = data["externalDrivePath"].ToString();
            }
            else if (result != null && result.DialogAnswer == DialogResults.Answer.No)
            {
                //기존에 선택되어 있는 폴더의 경로(or 폴더명)이 변경이 되었는데, Folder 선택 Dialog에서 Cancel을 클릭했을 경우
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                if ((bool)data["isSelectedPathChanged"])
                {
                    FileExport.ExternalDrivePath = "";
                }
            }
        }

        protected override void Export()
        {
            _log.Debug("Export");

            if (String.IsNullOrEmpty(FileExport.ExternalDrive))
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Enter an External Device"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);
            }
            else if (String.IsNullOrEmpty(FileExport.ExternalDrivePath))
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Select Destination path"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);
            }
            else if (_externalDriveAvailableFreeSpaceByte <= _exportSizeByte)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Insufficient storage space available"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);
            }
            else
            {
                if (timer.IsEnabled)
                    timer.Stop();
                FileSave();
            }            
        }

        protected void UpdateFileSize(double bytes)
        {
            double unit = 1024;

            if (bytes >= unit * unit * unit)
            {
                ExportSize = CommonUtil.ByteToGB(bytes);
                ExportSizeUnit = _l10n["GB"];
            }
            else if (bytes >= unit * unit)
            {
                ExportSize = CommonUtil.ByteToMB(bytes);
                ExportSizeUnit = _l10n["MB"];
            }
            else if (bytes >= unit)
            {
                ExportSize = CommonUtil.ByteToKB(bytes);
                ExportSizeUnit = _l10n["KB"];
            }
            else 
            {
                ExportSize = bytes;
                ExportSizeUnit = _l10n["Byte"];
            }

            _exportSizeByte = bytes;
        }

        protected virtual void FileSave() { }
    }
}
