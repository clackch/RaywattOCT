using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.ViewModels
{
    public partial class MainViewModel
    {
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

                    popupResponse.PopupParameter = EditPopupText;

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
