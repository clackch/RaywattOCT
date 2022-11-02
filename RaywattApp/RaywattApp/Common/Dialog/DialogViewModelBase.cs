using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Localization;
using RaywattApp.Models;
using System.Windows.Input;

namespace RaywattApp.Common.Dialog
{
    public abstract class DialogViewModelBase : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DialogViewModelBase));

        protected readonly DynamicResource _l10n;

        public string? Title { get; set; }

        public string? Message { get; set; }

        public object? Parameter { get; set; }

        public DialogResults? DialogResult { get; set; }

        private ICommand _yesCommand;
        public ICommand YesCommand
        {
            get { return this._yesCommand ?? (this._yesCommand = new RelayCommand<IDialogWindow>(AnswerYes)); }
        }

        private ICommand _noCommand;
        public ICommand NoCommand
        {
            get { return this._noCommand ?? (this._noCommand = new RelayCommand<IDialogWindow>(AnswerNo)); }
        }

        private ICommand _okCommand;
        public ICommand OKCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand<IDialogWindow>(AnswerOK)); }
        }

        public DialogViewModelBase()
        {
            _l10n = (DynamicResource)App.Current.Resources["L10N"];
            Title = "";
        }

        public virtual void SetParameter(object parameter) { }

        protected virtual void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("AnswerYes");

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;

            CloseDialogWithResult(dialog, dialogResults);
        }

        protected virtual void AnswerNo(IDialogWindow dialog)
        {
            _log.Debug("AnswerNo");

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.No;

            CloseDialogWithResult(dialog, dialogResults);
        }

        protected void AnswerOK(IDialogWindow dialog)
        {
            _log.Debug("AnswerOK");

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Undefined;

            CloseDialogWithResult(dialog, dialogResults);
        }

        protected void CloseDialogWithResult(IDialogWindow dialog, DialogResults result)
        {
            DialogResult = result;
            if (dialog != null)
            {
                dialog.DialogResult = true;
            }
        }
    }
}
