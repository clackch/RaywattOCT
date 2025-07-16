using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class TermsConditionsDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TermsConditionsDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private User _termsConditions;

        [ObservableProperty]
        private string _termsAndConditions;

        private ICommand _yesCommand;
        public ICommand YesCommand
        {
            get { return this._yesCommand ?? (this._yesCommand = new RelayCommand<IDialogWindow>(AnswerYes)); }
        }

        public TermsConditionsDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;
            TermsAndConditions = _l10n["$Terms and Conditions"];
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            TermsConditions = (User)data["tnC"];
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            base.AnswerYes(dialog);
        }
    }
}
