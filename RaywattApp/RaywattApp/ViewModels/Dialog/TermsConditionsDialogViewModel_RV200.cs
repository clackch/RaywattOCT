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
    public partial class TermsConditionsDialogViewModel_RV200 : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TermsConditionsDialogViewModel_RV200));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Configuration _termsConditions;

        [ObservableProperty]
        private string _termsAndConditions;

        private ICommand _yesCommand;
        public ICommand YesCommand
        {
            get { return this._yesCommand ?? (this._yesCommand = new RelayCommand<IDialogWindow>(AnswerYes, CanAgree)); }
        }

        public TermsConditionsDialogViewModel_RV200(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            TermsAndConditions = _l10n["$Terms and Conditions"];
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            TermsConditions = (Configuration)data["tnC"];
            TermsConditions.Buffer = "";
            TermsConditions.PropertyChanged += TermsConditions_PropertyChanged;
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Terms&Cond";
            sqlParameters["key"] = "AgreeYN";
            sqlParameters["value"] = "Y(" + DateTime.Now.ToString("yyyyMMddHHmmss") + ")";
            sqlParameters["buffer"] = TermsConditions.Buffer.Trim();

            int res = _sqlManager.UpdateConfiguration(sqlParameters);
            if (res != 1)
            {
                _log.Error("Update Error");
            }

            base.AnswerYes(dialog);
        }

        private bool CanAgree(IDialogWindow dialog)
        {
            return !string.IsNullOrEmpty(TermsConditions.Buffer);
        }

        private void TermsConditions_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            (YesCommand as RelayCommand<IDialogWindow>).NotifyCanExecuteChanged();
        }
    }
}
