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

        private ICommand _questionPopupResponseCommand;
        public ICommand QuestionPopupResponseCommand
        {
            get { return this._questionPopupResponseCommand ?? (this._questionPopupResponseCommand = new RelayCommand<string>(ResponseQuestionPopup)); }
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

        private void ResponseQuestionPopup(string response)
        {
            QuestionPopupResponse res = new QuestionPopupResponse();
            res.QuestionId = QuestionPopupId;
            res.QuestionResponse = response == "Y" ? true : false;

            Type? type = QuestionPopupParent.GetType();
            PropertyInfo questionPopupResponse = type.GetProperty("QuestionPopupRes");
            if(questionPopupResponse != null)
                questionPopupResponse.SetValue(QuestionPopupParent, res);

            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Question });
        }

        private void CloseMessagePopup()
        {
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Message });
        }
    }
}
