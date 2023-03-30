using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Views.Dialog;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.Common.Setting
{
    public class SettingBase : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingBase));

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

        protected void CloseDialog()
        {
            DialogWindow? dialog = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive) as DialogWindow;
            if (dialog != null)
            {
                dialog.DialogResult = true;
            }
        }

        protected virtual void Okay() { }

        protected virtual void Apply() { }
    }
}
