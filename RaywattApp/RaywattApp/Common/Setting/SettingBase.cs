using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using System.Windows.Input;

namespace RaywattApp.Common.Setting
{
    public class SettingBase : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingBase));

        private ICommand _refreshCommand;
        public ICommand RefreshCommand
        {
            get { return this._refreshCommand ?? (this._refreshCommand = new RelayCommand(Refresh)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _okayCommand;
        public ICommand OkayCommand
        {
            get { return this._okayCommand ?? (this._okayCommand = new RelayCommand(Okay)); }
        }

        private ICommand _applyCommand;
        public ICommand ApplyCommand
        {
            get { return this._applyCommand ?? (this._applyCommand = new RelayCommand(Apply)); }
        }

        private void Refresh()
        {
            _log.Debug("Refresh");
            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Refresh"));
        }

        private void Cancel()
        {
            _log.Debug("Cancel");
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Setting });
        }

        virtual protected void Okay() { }

        virtual protected void Apply() { }
    }
}
