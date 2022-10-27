using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
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
            get { return this._importCommand ?? (this._importCommand = new RelayCommand(Export)); }
        }

        virtual protected void Cancel()
        {
            _log.Debug("Cancel");
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.File });
        }

        virtual protected void Next() { }

        virtual protected void Export() { }

        virtual protected void Import() { }

        virtual protected void Back() { }
    }
}
