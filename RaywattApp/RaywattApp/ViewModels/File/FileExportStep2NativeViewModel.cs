using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.File;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Threading;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2NativeViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2NativeViewModel));

        [ObservableProperty]
        private FileExport _fileExport;

        private DispatcherTimer timer = new DispatcherTimer();

        [ObservableProperty]
        private Dictionary<string, string> _externalDriveComboBox;

        [ObservableProperty]
        private bool isEnableExternalDrive;

        private string _selectedExternalDrive;

        private bool isExternalDriveInit;

        public string SelectedExternalDrive
        {
            get { return _selectedExternalDrive; }
            set 
            {
                if (_selectedExternalDrive != value)
                {
                    _selectedExternalDrive = value;
                    FileExport.ExternalDrive = _selectedExternalDrive;
                    if(!isExternalDriveInit)
                        FileExport.ExternalDrivePath = "";

                    if(value == null)
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
                                ExternalDriveTotalSize = ((long[])item.Value)[0];
                                ExternalDriveAvailableFreeSpace = ((long[])item.Value)[1];
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
        private long _externalDriveTotalSize;

        [ObservableProperty]
        private long _externalDriveAvailableFreeSpace;

        private ICommand _passwordCommand;
        public ICommand PasswordCommand
        {
            get { return this._passwordCommand ?? (this._passwordCommand = new RelayCommand(PasswordProtected)); }
        }

        private ICommand _alternateCommand;
        public ICommand AlternateCommand
        {
            get { return this._alternateCommand ?? (this._alternateCommand = new RelayCommand(AlternatePatientId)); }
        }

        private ICommand _externalDrivePathCommand;
        public ICommand ExternalDrivePathCommand
        {
            get { return this._externalDrivePathCommand ?? (this._externalDrivePathCommand = new RelayCommand(ExternalDrivePath)); }
        }

        public FileExportStep2NativeViewModel()
        {
            ExternalDriveComboBox = new Dictionary<string, string>();
            ExternalDriveList = new Dictionary<string, object>();

            isExternalDriveInit = true;
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(CheckDrive);
            timer.Start();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                FileExport = (FileExport)extraData;
                SetCondition();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            timer.Stop();
        }

        protected override void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Views/File/FileExportStep1Page.xaml") { Parameter = FileExport });
        }

        protected override void Cancel()
        {
            _log.Debug("Cancel");

            timer.Stop();
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.File });
        }

        public override void CallbackPopup()
        {
            _log.Debug("CallbackPopup : " + PopupCallback.PopupId + "/" + PopupCallback.PopupAnswer);

            if (PopupCallback != null)
            {
                if (PopupCallback.PopupId == (int)CommonDefinition.CallbackEdit.Password)
                {
                    if (PopupCallback.PopupAnswer)
                    {
                        Dictionary<string, Object> data = (Dictionary<string, Object>)PopupCallback.PopupParameter;
                        FileExport.PasswordProtected = (bool)data["passwordProtected"];
                        FileExport.Password = data["password"].ToString();
                        FileExport.ConfirmPassword = data["confirmPassword"].ToString();
                    }
                }
                else if (PopupCallback.PopupId == (int)CommonDefinition.CallbackEdit.AlternateId)
                {
                    if (PopupCallback.PopupAnswer)
                    {
                        Dictionary<string, Object> data = (Dictionary<string, Object>)PopupCallback.PopupParameter;
                        FileExport.AlternatePatientId.Clear();
                        foreach (KeyValuePair<string, string> item in (Dictionary<string, string>)data["alternatePatientId"])
                        {
                            FileExport.AlternatePatientId.Add(item.Key, item.Value);
                        }
                    }
                }
                else if (PopupCallback.PopupId == (int)CommonDefinition.CallbackLookup.FolderBrowser)
                {
                    if (PopupCallback.PopupAnswer)
                    {
                        Dictionary<string, Object> data = (Dictionary<string, Object>)PopupCallback.PopupParameter;
                        FileExport.ExternalDrivePath = data["externalDrivePath"].ToString();
                    }
                }
            }
        }

        private void CheckDrive(object sender, EventArgs e)
        {
            GetDrive();
        }

        private void GetDrive()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            Dictionary<string, string> currExternalDrive = new Dictionary<string, string>();

            string firstExternalDrive = "";
            bool isFirstExternalDrive = true;

            ExternalDriveList.Clear();

            foreach (DriveInfo d in allDrives)
            {
                if (d.IsReady == true)
                {
                    if (d.DriveType == DriveType.CDRom)
                    {

                    }
                    else if (d.DriveType == DriveType.Removable)
                    {
                        currExternalDrive[d.Name] = d.Name;
                        long[] data = { d.TotalSize, d.AvailableFreeSpace };
                        ExternalDriveList.Add(d.Name, data);

                        if (isFirstExternalDrive)
                        {
                            firstExternalDrive = d.Name;
                            isFirstExternalDrive = false;
                        }
                    }
                }
            }

            if (ExternalDriveComboBox.Count != currExternalDrive.Count)
            {
                ExternalDriveComboBox = currExternalDrive;

                if(ExternalDriveComboBox.Count > 0)
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
        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            if (FileExport.Purpose == null)
                FileExport.Purpose = "Archive";//Archive

            if (FileExport.FileOption == null)
                FileExport.FileOption = "Leave";//Leave Unchanged

            if (FileExport.DiskType == null)
                FileExport.DiskType = "CD";//CD/DVD

            if (FileExport.Password == null)
                FileExport.Password = "";

            if (FileExport.ConfirmPassword == null)
                FileExport.ConfirmPassword = "";

            GetDrive();
        }

        private void PasswordProtected()
        {
            _log.Debug("PasswordProtected");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["passwordProtected"] = FileExport.PasswordProtected;
            parameter["password"] = FileExport.Password;
            parameter["confirmPassword"] = FileExport.ConfirmPassword;

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "PasswordProtectedPopupControl", Type = (int)CommonDefinition.PopupType.Edit, PopupId = (int)CommonDefinition.CallbackEdit.Password, ParentObject = this, Parameter = parameter });
        }

        private void AlternatePatientId()
        {
            _log.Debug("AlternatePatientId");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            if (FileExport.AlternatePatientId == null)
                FileExport.AlternatePatientId = new Dictionary<string, string>();
            parameter["patientList"] = FileExport.PatientList;
            parameter["alternatePatientId"] = FileExport.AlternatePatientId;

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "AlternatedPatientIdPopupControl", Type = (int)CommonDefinition.PopupType.Edit, PopupId = (int)CommonDefinition.CallbackEdit.AlternateId, ParentObject = this, Parameter = parameter });
        }

        private void ExternalDrivePath()
        {
            _log.Debug("ExternalDrivePath");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["externalDrive"] = FileExport.ExternalDrive;
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "FolderBrowserPopupControl", Type = (int)CommonDefinition.PopupType.Lookup, PopupId = (int)CommonDefinition.CallbackLookup.FolderBrowser, ParentObject = this, Parameter = parameter });
        }
    }
}
