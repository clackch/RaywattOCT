using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Views.Dialog;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.Common.File
{
    public class FileBase : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileBase));

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next)); }
        }

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get { return this._exportCommand ?? (this._exportCommand = new RelayCommand(Export)); }
        }

        private ICommand _importCommand;
        public ICommand ImportCommand
        {
            get { return this._importCommand ?? (this._importCommand = new RelayCommand(Import)); }
        }

        protected virtual void Cancel()
        {
            _log.Debug("Cancel");

            CloseDialog();
        }

        protected void CloseDialog()
        {
            DialogWindow? dialog = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive) as DialogWindow;
            if (dialog != null)
            {
                dialog.DialogResult = true;
            }
        }

        protected virtual void Next() { }

        protected virtual void Export() { }

        protected virtual void Import() { }

        protected virtual void Back() { }
    }
}
