using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RayCoreWrapper;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

        protected DispatcherTimer timer = new DispatcherTimer();

        private string _diskType; //CD/DVD, External Drive
        public string DiskType
        {
            get { return _diskType; }
            set
            {
                _diskType = value;
                FileExport.DiskType = _diskType;
                OnPropertyChanged(nameof(DiskType));
            }
        }

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
                    }
                    else
                    {
                        foreach (var item in ExternalDriveList)
                        {
                            if (value.Equals(item.Key))
                            {
                                ExternalDriveTotalSize = CommonUtil.ByteToGB(((long[])item.Value)[0]);
                                ExternalDriveAvailableFreeSpace = CommonUtil.ByteToGB(((long[])item.Value)[1]);
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

        [ObservableProperty]
        private string _cdDiskType;

        private bool cdInit = false;

        [ObservableProperty]
        private double _cdTotalSize;

        [ObservableProperty]
        private double _cdAvailableFreeSpace;

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

            RayExportWrapper.CDBurnError cDBurnError;
            cDBurnError = RayExportWrapper.initDevice();
            _log.Debug("initDevice : " + cDBurnError);

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
            if (FileExport.DiskType.Equals(Constants.FileDiskCd))
            {
                RayExportWrapper.CDBurnError cDBurnError;
                cDBurnError = RayExportWrapper.checkDiskOnDrive();
                _log.Debug("checkDiskOnDrive : " + cDBurnError);

                if (cDBurnError == RayExportWrapper.CDBurnError.OK)
                {
                    if (!cdInit)
                    {
                        GetCdInfo();

                        cdInit = true;
                    }
                }
                else
                {
                    FileExport.MediaType = Constants.MediaTypeNoDisc;
                    FileExport.IsDiskFormat = false;
                    CdTotalSize = 0;
                    CdAvailableFreeSpace = 0;

                    cdInit = false;
                }
            }
            else
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
        }

        private async void GetCdInfo()
        {
            await Task.Run(() =>
            {
                RayExportWrapper.CDBurnError cDBurnError;
                cDBurnError = RayExportWrapper.initDevice();
                _log.Debug("initDevice : " + cDBurnError);

                RayExportWrapper.MediaType mediaType;
                mediaType = RayExportWrapper.getDiskType();
                _log.Debug("getDiskType : " + mediaType);
                SetMediaType(mediaType);

                CdTotalSize = CommonUtil.ByteToGB(RayExportWrapper.checkTotalBlock());
                CdAvailableFreeSpace = CommonUtil.ByteToGB(RayExportWrapper.checkFreeBlock());
            });
        }

        private void SetMediaType(RayExportWrapper.MediaType mediaType)
        {
            FileExport.VolumeLabel = DateTime.Now.ToString("yyyy.MM.dd");

            switch (mediaType)
            {
                case RayExportWrapper.MediaType.NotSupportDisc:
                    FileExport.MediaType = Constants.MediaTypeNotSupportDisc;
                    FileExport.IsDiskFormat = false;
                    FileExport.VolumeLabel = "";
                    break;
                case RayExportWrapper.MediaType.TYPE_CDR:
                    FileExport.MediaType = Constants.MediaTypeCDR;
                    FileExport.IsDiskFormat = false;
                    break;
                case RayExportWrapper.MediaType.TYPE_CDRW:
                    FileExport.MediaType = Constants.MediaTypeCDRW;
                    FileExport.IsDiskFormat = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDDASHR:
                    FileExport.MediaType = Constants.MediaTypeDVDDASHR;
                    FileExport.IsDiskFormat = false;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDDASHRW:
                    FileExport.MediaType = Constants.MediaTypeDVDDASHRW;
                    FileExport.IsDiskFormat = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDPLUSR:
                    FileExport.MediaType = Constants.MediaTypeDVDPLUSR;
                    FileExport.IsDiskFormat = false;
                    break;
                case RayExportWrapper.MediaType.TYPE_DVDPLUSRW:
                    FileExport.MediaType = Constants.MediaTypeDVDPLUSRW;
                    FileExport.IsDiskFormat = true;
                    break;
                case RayExportWrapper.MediaType.TYPE_BDR:
                    FileExport.MediaType = Constants.MediaTypeBDR;
                    FileExport.IsDiskFormat = false;
                    break;
                case RayExportWrapper.MediaType.TYPE_BDRE:
                    FileExport.MediaType = Constants.MediaTypeBDRE;
                    FileExport.IsDiskFormat = true;
                    break;
                default:
                    FileExport.MediaType = Constants.MediaTypeNotSupportDisc;
                    FileExport.IsDiskFormat = false;
                    FileExport.VolumeLabel = "";
                    break;
            }
        }

        private void ExternalDrivePath()
        {
            _log.Debug("ExternalDrivePath");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["externalDrive"] = FileExport.ExternalDrive;
            parameter["externalDrivePath"] = FileExport.ExternalDrivePath;
            var result = _dialogService.OpenDialog(new FileFolderBrowseDialogControl(), parameter);

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

            if (FileExport.DiskType.Equals(Constants.FileDiskExternal))
            {
                if (String.IsNullOrEmpty(FileExport.ExternalDrivePath))
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Path is required"];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                }
                else if (ExternalDriveAvailableFreeSpace <= ExportSize)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["There is not enough space on the storage device to store."];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                }
                else
                {
                    if (timer.IsEnabled)
                        timer.Stop();
                    FileSave();
                }
            }
            else if (FileExport.DiskType.Equals(Constants.FileDiskCd))
            {
                if (FileExport.MediaType == Constants.MediaTypeNoDisc)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["CD/DVD is required"];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                }
                else if (FileExport.MediaType == Constants.MediaTypeNotSupportDisc)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Media Type is not supported"];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                }
                else if (FileExport.VolumeLabel == null || String.IsNullOrEmpty(FileExport.VolumeLabel.Trim()))
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Volume Label is required"];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                }
                else if (CdTotalSize <= ExportSize)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["There is not enough space on the storage device to store."];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                }
                else
                {
                    if (timer.IsEnabled)
                        timer.Stop();
                    FileSave();
                }
            }
        }

        protected virtual void FileSave() { }
    }
}
