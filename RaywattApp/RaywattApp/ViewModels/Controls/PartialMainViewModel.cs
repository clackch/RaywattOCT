using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
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

        private bool CanButtonClick()
        {
            _log.Debug("CanButtonClick");

            //View Layer Popup이 열려있는 경우, 다시 열리지 않도록 처리
            return !ShowViewLayerPopup;
        }

        private void Setting()
        {
            _log.Debug("Setting");

            PopupNavigationSource = "Views/Setting/SettingAcquisitionPage.xaml";
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "SettingPopupControl", Type = (int)CommonDefinition.PopupType.Setting });
        }

        private void ShowPatientEdit()
        {
            _log.Debug("ShowPatientEdit");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientEditPage.xaml") { Parameter = Patient });
        }
    }
}
