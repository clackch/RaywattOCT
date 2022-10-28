using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.ViewModels
{
    public partial class MainViewModel
    {
        private bool _showLayerPopup;
        /// <summary>
        /// 레이어 팝업 출력여부
        /// </summary>
        public bool ShowLayerPopup
        {
            get { return _showLayerPopup; }
            set { SetProperty(ref _showLayerPopup, value); }
        }

        private string _controlName;
        /// <summary>
        /// 레이어 팝업 내부 컨트롤 이름
        /// </summary>
        public string ControlName
        {
            get { return _controlName; }
            set { SetProperty(ref _controlName, value); }
        }

        private bool _showLayerExtraPopup;
        /// <summary>
        /// 레이어 팝업 출력여부
        /// </summary>
        public bool ShowLayerExtraPopup
        {
            get { return _showLayerExtraPopup; }
            set { SetProperty(ref _showLayerExtraPopup, value); }
        }

        private string _controlExtraName;
        /// <summary>
        /// 레이어 팝업 내부 컨트롤 이름
        /// </summary>
        public string ControlExtraName
        {
            get { return _controlExtraName; }
            set { SetProperty(ref _controlExtraName, value); }
        }

        private bool _showViewLayerPopup;
        /// <summary>
        /// 레이어 팝업 출력여부
        /// </summary>
        public bool ShowViewLayerPopup
        {
            get { return _showViewLayerPopup; }
            set { SetProperty(ref _showViewLayerPopup, value); }
        }

        private string _viewControlName;
        /// <summary>
        /// 레이어 팝업 내부 컨트롤 이름
        /// </summary>
        public string ViewControlName
        {
            get { return _viewControlName; }
            set { SetProperty(ref _viewControlName, value); }
        }

        [ObservableProperty]
        private string _messagePopupLevel;

        [ObservableProperty]
        private string _messagePopupContent;

        [ObservableProperty]
        private string _questionPopupContent;

        [ObservableProperty]
        private int _popupId;

        [ObservableProperty]
        private object _popupParent;

        private int callbackType;

        [ObservableProperty]
        private string _layerPopupTitle;

        [ObservableProperty]
        private string _viewLayerPopupTitle;

        [ObservableProperty]
        private string _layerExtraPopupTitle;

        [ObservableProperty]
        private string _editPopupText;

        [ObservableProperty]
        private Dictionary<string, string> _physicianComboBox;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private FileExport _fileExport;

        [ObservableProperty]
        private Visibility _isErrorMessage;

        [ObservableProperty]
        private IList<Patient> _patientList;

        private DirectoryProvider directoryProvider;

        [ObservableProperty]
        private DirectoryItem _selectedDir;

        private ObservableCollection<Item> _dirItems;
        public ObservableCollection<Item> DirItems
        {
            get { return _dirItems; }
            set
            {
                _dirItems = value;
                OnPropertyChanged(nameof(DirItems));
            }
        }

        private bool isRenameFolder;

        private string _createRenameFolderName;
        public string CreateRenameFolderName
        {
            get { return _createRenameFolderName; }
            set { _createRenameFolderName = value; IsErrorMessage = Visibility.Collapsed; OnPropertyChanged(nameof(CreateRenameFolderName)); }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set { _password = value; IsErrorMessage = Visibility.Collapsed; }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; IsErrorMessage = Visibility.Collapsed; }
        }


        private ICommand _settingCommand;
        public ICommand SettingCommand
        {
            get { return this._settingCommand ?? (this._settingCommand = new RelayCommand(Setting, CanButtonClick)); }
        }

        private ICommand _popupResponseCommand;
        public ICommand PopupResponseCommand
        {
            get { return this._popupResponseCommand ?? (this._popupResponseCommand = new RelayCommand<string>(ResponsePopup)); }
        }

        private ICommand _messagePopupCloseCommand;
        public ICommand MessagePopupCloseCommand
        {
            get { return this._messagePopupCloseCommand ?? (this._messagePopupCloseCommand = new RelayCommand(CloseMessagePopup)); }
        }

        private ICommand _resetCommand;
        public ICommand ResetCommand
        {
            get { return this._resetCommand ?? (this._resetCommand = new RelayCommand(ResetPatientId)); }
        }

        private ICommand _folderActionCommand;
        public ICommand FolderActionCommand
        {
            get { return this._folderActionCommand ?? (this._folderActionCommand = new RelayCommand<string>(FolderAction)); }
        }

        private ICommand _createRenameFolderCommand;
        public ICommand CreateRenameFolderCommand
        {
            get { return this._createRenameFolderCommand ?? (this._createRenameFolderCommand = new RelayCommand<string>(CreateRenameFolder)); }
        }

        private void OnLayerPopupMessage(object recipient, PopupMessage message)
        {
            _log.Debug("OnLayerPopupMessage : " + message.Type + "/" + message.Value + "/" + message.ControlName);

            if (message.Type == (int)CommonDefinition.PopupType.Question || message.Type == (int)CommonDefinition.PopupType.Edit || message.Type == (int)CommonDefinition.PopupType.Lookup)
                callbackType = message.Type;

            switch (message.Type)
            {
                case (int)CommonDefinition.PopupType.Message:

                    ShowLayerPopup = message.Value;
                    ControlName = message.ControlName;

                    //Popup Level
                    switch (message.Level)
                    {
                        case (int)CommonDefinition.PopupLevel.Info:
                            MessagePopupLevel = _l10n["Information"];
                            break;
                        case (int)CommonDefinition.PopupLevel.Warn:
                            MessagePopupLevel = _l10n["Warning"];
                            break;
                        case (int)CommonDefinition.PopupLevel.Error:
                            MessagePopupLevel = _l10n["Error"];
                            break;
                        default:
                            break;
                    }

                    //Popup Message
                    if (message.Parameter != null)
                        MessagePopupContent = message.Parameter.ToString();

                    break;
                case (int)CommonDefinition.PopupType.Question:

                    ShowLayerPopup = message.Value;
                    ControlName = message.ControlName;

                    PopupId = message.PopupId;

                    if (message.ParentObject != null)
                        PopupParent = message.ParentObject;

                    //Popup Message
                    if (message.Parameter != null)
                        QuestionPopupContent = message.Parameter.ToString();

                    break;
                case (int)CommonDefinition.PopupType.Edit:

                    ShowLayerPopup = message.Value;
                    ControlName = message.ControlName;

                    PopupId = message.PopupId;

                    if (message.ParentObject != null)
                        PopupParent = message.ParentObject;

                    if (PopupId == (int)CommonDefinition.CallbackEdit.Vessel)
                    {
                        LayerPopupTitle = _l10n["Vessel"];

                        if (message.Parameter != null)
                        {
                            EditPopupText = message.Parameter.ToString().Trim();
                        }
                        else
                        {
                            EditPopupText = "";
                        }

                    }
                    else if (PopupId == (int)CommonDefinition.CallbackEdit.Procedure)
                    {
                        LayerPopupTitle = _l10n["Procedure"];

                        if (message.Parameter != null)
                        {
                            EditPopupText = message.Parameter.ToString().Trim();
                        }
                        else
                        {
                            EditPopupText = "";
                        }
                    }
                    else if (PopupId == (int)CommonDefinition.CallbackEdit.Case)
                    {
                        LayerPopupTitle = _l10n["Case"];

                        if(PhysicianComboBox == null)
                            PhysicianComboBox = new Dictionary<string, string>();

                        IList<Physician> physicianList = _sqlManager.SelectPhysicianList();

                        if (PhysicianComboBox.Count != physicianList.Count)
                        {
                            Dictionary<string, string> dic = new Dictionary<string, string>();
                            foreach (Physician physician in physicianList)
                            {
                                dic[physician.Name] = physician.Name;
                            }
                            PhysicianComboBox = dic;
                        }

                        if (message.Parameter != null)
                        {
                            if(PatientCase == null)
                                PatientCase = new PatientCase();

                            Dictionary<string, Object> data = (Dictionary<string, Object>)message.Parameter;
                            PatientCase.PhysicianName = data["physicianName"].ToString();
                            PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                            PatientCase.Comment = data["comment"].ToString();
                        }
                    }
                    else if(PopupId == (int)CommonDefinition.CallbackEdit.Password)
                    {
                        LayerPopupTitle = _l10n["Enter Dataset Password"];

                        if (message.Parameter != null)
                        {
                            if(FileExport == null)
                                FileExport = new FileExport();

                            Dictionary<string, Object> data = (Dictionary<string, Object>)message.Parameter;
                            FileExport.PasswordProtected = (bool)data["passwordProtected"];
                            Password = data["password"].ToString();
                            ConfirmPassword = data["confirmPassword"].ToString();
                        }
                    }
                    else if(PopupId == (int)CommonDefinition.CallbackEdit.AlternateId)
                    {
                        LayerPopupTitle = _l10n["Define Alternate Patient ID"];

                        if (FileExport == null)
                            FileExport = new FileExport();
                        if(FileExport.AlternatePatientId == null)
                            FileExport.AlternatePatientId = new Dictionary<string, string>();

                        Dictionary<string, Object> data = (Dictionary<string, Object>)message.Parameter;

                        FileExport.AlternatePatientId.Clear();
                        foreach (KeyValuePair<string, string> item in (Dictionary<string, string>)data["alternatePatientId"])
                        {
                            FileExport.AlternatePatientId.Add(item.Key, item.Value);
                        }

                        Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                        sqlParameters["ids"] = (List<string>)data["patientList"];

                        PatientList = _sqlManager.SelectPatientByList(sqlParameters);

                        //기존에 입력한 정보가 있는 경우, 설정 (단, 변경이 있으면 입력 안함)
                        if(PatientList.Count == FileExport.AlternatePatientId.Count)
                        {
                            bool isExist = true;

                            //기존 patient 목록과 대체 ID 입력의 patient 목록 비교
                            foreach (Patient patient in PatientList)
                            {
                                isExist = false;
                                foreach (KeyValuePair<string, string> id in FileExport.AlternatePatientId)
                                {
                                    if (patient.Id == id.Key)
                                    {
                                        isExist = true;
                                        break;
                                    }
                                }
                                if (!isExist)
                                    break;
                            }

                            //전체 동일할 경우만, 기존 대체 ID 입력
                            if (isExist)
                            {
                                foreach (Patient patient in PatientList)
                                {
                                    foreach (KeyValuePair<string, string> id in FileExport.AlternatePatientId)
                                    {
                                        if (patient.Id == id.Key)
                                            patient.AlternateId = id.Value;
                                    }
                                }
                            }
                        }

                    }

                    break;
                case (int)CommonDefinition.PopupType.Setting:

                    ShowViewLayerPopup = message.Value;
                    ViewControlName = message.ControlName;

                    break;
                case (int)CommonDefinition.PopupType.File:

                    ShowViewLayerPopup = message.Value;
                    ViewControlName = message.ControlName;
                    PopupNavigationParameter = message.Parameter;

                    if (message.FileType == (int)CommonDefinition.FileType.Import)
                    {
                        ViewLayerPopupTitle = _l10n["Import"];
                        PopupNavigationSource = "Views/File/FileImportPage.xaml";
                    }
                    else
                    {
                        ViewLayerPopupTitle = _l10n["Export"];
                        PopupNavigationSource = "Views/File/FileExportStep1Page.xaml";
                    }

                    break;
                case (int)CommonDefinition.PopupType.Lookup:

                    ShowLayerPopup = message.Value;
                    ControlName = message.ControlName;

                    PopupId = message.PopupId;

                    if (message.ParentObject != null)
                        PopupParent = message.ParentObject;

                    if (PopupId == (int)CommonDefinition.CallbackLookup.FolderBrowser)
                    {
                        LayerPopupTitle = _l10n["Browser for Folder"];

                        if (message.Parameter != null)
                        {
                            if (FileExport == null)
                                FileExport = new FileExport();

                            Dictionary<string, Object> data = (Dictionary<string, Object>)message.Parameter;
                            FileExport.ExternalDrive = data["externalDrive"].ToString();

                            if (directoryProvider == null)
                                directoryProvider = new DirectoryProvider();

                            directoryProvider.GetDirectory(FileExport.ExternalDrive);
                            DirItems = directoryProvider.DirItems;
                        }
                    }

                    break;
                case (int)CommonDefinition.PopupType.Extra:

                    ShowLayerExtraPopup = message.Value;
                    ControlExtraName = message.ControlName;

                    if (message.Parameter != null)
                    {
                        if ("R".Equals(message.Parameter.ToString()))
                        {
                            LayerExtraPopupTitle = _l10n["Rename Folder"];
                            isRenameFolder = true;
                            CreateRenameFolderName = SelectedDir.Name;
                        }
                        else
                        {
                            LayerExtraPopupTitle = _l10n["Create New Folder"];
                            isRenameFolder = false;
                            CreateRenameFolderName = "";
                        }
                    }

                    break;
                default:
                    break;
            }
        }

        private bool CanButtonClick()
        {
            _log.Debug("CanButtonClick");

            //Layer Popup이 열려있는 경우, 다시 열리지 않도록 처리
            return !ShowViewLayerPopup && !ShowLayerPopup;
        }

        private void Setting()
        {
            _log.Debug("Setting");

            PopupNavigationSource = "Views/Setting/SettingAcquisitionPage.xaml";
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "SettingPopupControl", Type = (int)CommonDefinition.PopupType.Setting });
        }

        private void ResponsePopup(string response)
        {
            _log.Debug("ResponsePopup");

            Type? type = PopupParent.GetType();
            PropertyInfo popupCallback = type.GetProperty("PopupCallback");
            PopupResponse popupResponse = new PopupResponse();
            popupResponse.PopupId = PopupId;
            popupResponse.PopupAnswer = response == "Y" ? true : false;

            switch (callbackType)
            {
                case (int)CommonDefinition.PopupType.Question:

                    if (popupCallback != null)
                        popupCallback.SetValue(PopupParent, popupResponse);

                    WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Question });

                    break;
                case (int)CommonDefinition.PopupType.Edit:

                    if(PopupId == (int)CommonDefinition.CallbackEdit.Vessel || PopupId == (int)CommonDefinition.CallbackEdit.Procedure)
                    {
                        popupResponse.PopupParameter = EditPopupText;
                    }
                    else if(PopupId == (int)CommonDefinition.CallbackEdit.Case)
                    {
                        Dictionary<string, object> parameter = new Dictionary<string, object>();
                        parameter["physicianName"] = PatientCase.PhysicianName;
                        parameter["accessionNumber"] = PatientCase.AccessionNumber;
                        parameter["comment"] = PatientCase.Comment;
                        popupResponse.PopupParameter = parameter;
                    }
                    else if(PopupId == (int)CommonDefinition.CallbackEdit.Password)
                    {
                        if (popupResponse.PopupAnswer && !Password.Trim().Equals(ConfirmPassword.Trim()))
                        {
                            _log.Debug("Differ :" + Password.Trim() + " / " + ConfirmPassword.Trim());

                            IsErrorMessage = Visibility.Visible;

                            return;
                        }

                        Dictionary<string, object> parameter = new Dictionary<string, object>();
                        parameter["passwordProtected"] = FileExport.PasswordProtected;
                        parameter["password"] = Password.Trim();
                        parameter["confirmPassword"] = ConfirmPassword.Trim();
                        popupResponse.PopupParameter = parameter;
                    }
                    else if(PopupId == (int)CommonDefinition.CallbackEdit.AlternateId)
                    {
                        Dictionary<string, object> parameter = new Dictionary<string, object>();

                        FileExport.AlternatePatientId.Clear();
                        foreach (Patient patient in PatientList)
                        {
                            if (patient.AlternateId == null)
                                patient.AlternateId = "";

                            FileExport.AlternatePatientId.Add(patient.Id, patient.AlternateId);
                        }

                        parameter["alternatePatientId"] = FileExport.AlternatePatientId;
                        popupResponse.PopupParameter = parameter;
                    }

                    if (popupCallback != null)
                        popupCallback.SetValue(PopupParent, popupResponse);

                    WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Edit });

                    break;
                case (int)CommonDefinition.PopupType.Lookup:

                    if(PopupId == (int)CommonDefinition.CallbackLookup.FolderBrowser)
                    {
                        Dictionary<string, object> parameter = new Dictionary<string, object>();

                        if (SelectedDir != null)
                            parameter["externalDrivePath"] = SelectedDir.Path;
                        else
                            parameter["externalDrivePath"] = "";

                        popupResponse.PopupParameter = parameter;
                    }

                    if (popupCallback != null)
                        popupCallback.SetValue(PopupParent, popupResponse);

                    WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Lookup });

                    break;
                default:
                    break;
            }

        }

        private void CloseMessagePopup()
        {
            _log.Debug("CloseMessagePopup");

            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Message });
        }

        private void ResetPatientId()
        {
            _log.Debug("ResetPatientId");

            foreach (Patient patient in PatientList)
            {
                patient.AlternateId = "";
            }
        }

        private void FolderAction(string action)
        {
            _log.Debug("FolderAction");

            if (SelectedDir == null)
                return;

            if (action == "R")
            {
                if (DirItems[0].Path == SelectedDir.Path)
                    return;

                WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "CreateRenameFolderPopupControl", Type = (int)CommonDefinition.PopupType.Extra, Parameter = "R" });
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "CreateRenameFolderPopupControl", Type = (int)CommonDefinition.PopupType.Extra, Parameter = "C" });
            }
        }


        private void CreateRenameFolder(string response)
        {
            _log.Debug("CreateRenameFolder");

            if (response.Equals("Y"))
            {
                if (String.IsNullOrEmpty(CreateRenameFolderName.Trim()))
                {
                    return;
                }

                if (isRenameFolder)
                {
                    if (SelectedDir.Name.Equals(CreateRenameFolderName.Trim()))
                    {
                        WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Extra });
                        return;
                    }

                    string result = directoryProvider.RenameDirectory(SelectedDir.Path, SelectedDir.Name, CreateRenameFolderName.Trim());

                    if (result.Equals("D"))
                    {
                        IsErrorMessage = Visibility.Visible;
                        return;
                    }
                }
                else
                {
                    _log.Debug(CreateRenameFolderName);

                    string result = directoryProvider.AddDirectory(SelectedDir.Path, CreateRenameFolderName.Trim());

                    if (result.Equals("D"))
                    {
                        IsErrorMessage = Visibility.Visible;
                        return;
                    }
                }
            }

            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Extra });
        }
    }
}
