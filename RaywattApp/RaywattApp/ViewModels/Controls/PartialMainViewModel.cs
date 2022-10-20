using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        private int _messagePopupType;

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

        [ObservableProperty]
        private string _editPopupType;

        [ObservableProperty]
        private string _editPopupText;

        [ObservableProperty]
        private Dictionary<string, string> _physicianComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private string _filePopupType;


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

        private void OnLayerPopupMessage(object recipient, PopupMessage message)
        {
            _log.Debug("OnLayerPopupMessage : " + message.Type + "/" + message.Value + "/" + message.ControlName);

            MessagePopupType = message.Type;

            switch (MessagePopupType)
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

                    if (PopupId == (int)CommonDefinition.EditList.Vessel)
                    {
                        EditPopupType = _l10n["Vessel"];

                        if (message.Parameter != null)
                        {
                            EditPopupText = message.Parameter.ToString().Trim();
                        }
                        else
                        {
                            EditPopupText = "";
                        }

                    }
                    else if (PopupId == (int)CommonDefinition.EditList.Procedure)
                    {
                        EditPopupType = _l10n["Procedure"];

                        if (message.Parameter != null)
                        {
                            EditPopupText = message.Parameter.ToString().Trim();
                        }
                        else
                        {
                            EditPopupText = "";
                        }
                    }
                    else if (PopupId == (int)CommonDefinition.EditList.Case)
                    {
                        EditPopupType = _l10n["Case"];

                        IList<Physician> physicianList = _sqlManager.SelectPhysicianList();
                        foreach(Physician physician in physicianList)
                        {
                            PhysicianComboBox[physician.Name] = physician.Name;
                        }

                        if (message.Parameter != null)
                        {
                            Dictionary<string, Object> data = (Dictionary<string, Object>)message.Parameter;
                            PatientCase.PhysicianName = data["physicianName"].ToString();
                            PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                            PatientCase.Comment = data["comment"].ToString();
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
                        FilePopupType = _l10n["Import"];
                        PopupNavigationSource = "Views/File/FileImportPage.xaml";
                    }
                    else
                    {
                        FilePopupType = _l10n["Export"];
                        PopupNavigationSource = "Views/File/FileExportStep1Page.xaml";
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
            Type? type = PopupParent.GetType();
            PropertyInfo popupCallback = type.GetProperty("PopupCallback");
            PopupResponse popupResponse = new PopupResponse();
            popupResponse.PopupId = PopupId;
            popupResponse.PopupAnswer = response == "Y" ? true : false;


            switch (MessagePopupType)
            {
                case (int)CommonDefinition.PopupType.Question:


                    if (popupCallback != null)
                        popupCallback.SetValue(PopupParent, popupResponse);

                    WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Question });

                    break;
                case (int)CommonDefinition.PopupType.Edit:

                    if(PopupId == (int)CommonDefinition.EditList.Vessel || PopupId == (int)CommonDefinition.EditList.Procedure)
                    {
                        popupResponse.PopupParameter = EditPopupText;
                    }
                    else if(PopupId == (int)CommonDefinition.EditList.Case)
                    {
                        Dictionary<string, object> parameter = new Dictionary<string, object>();
                        parameter["physicianName"] = PatientCase.PhysicianName;
                        parameter["accessionNumber"] = PatientCase.AccessionNumber;
                        parameter["comment"] = PatientCase.Comment;
                        popupResponse.PopupParameter = parameter;
                    }                    

                    if (popupCallback != null)
                        popupCallback.SetValue(PopupParent, popupResponse);

                    WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Edit });

                    break;
                default:
                    break;
            }

        }

        private void CloseMessagePopup()
        {
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Message });
        }
    }
}
