using System.Collections.Generic;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class EditInstituteDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EditCaseInfoDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Institute _institute = new();

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return this._saveCommand ?? (this._saveCommand = new RelayCommand<IDialogWindow>(Save, CanSave)); }
        }

        public EditInstituteDialogViewModel(SqlManager sqlManager)
        {
            _log.Info("EditInstituteDialogViewModel");

            _sqlManager = sqlManager;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Institute.Name = data["name"].ToString();
            Institute.Comment = data["comment"].ToString();

            Institute.PropertyChanged += Institute_PropertyChanged;
        }

        private void Institute_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _log.Debug("User_PropertyChanged");

            (SaveCommand as RelayCommand<IDialogWindow>).NotifyCanExecuteChanged();
        }

        private bool CanSave(IDialogWindow dialog)
        {
            return !string.IsNullOrWhiteSpace(Institute.Name);
        }

        private void Save(IDialogWindow dialog)
        {
            if (string.IsNullOrWhiteSpace(Institute.Name))
            {
                return;
            }

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Institute";
            sqlParameters["key"] = "Info";
            sqlParameters["value"] = Institute.Name = Institute.Name.Trim();
            sqlParameters["buffer"] = Institute.Comment = Institute.Comment.Trim();

            int res = _sqlManager.UpdateConfiguration(sqlParameters);
            if (res != 1)
            {
                _log.Error("Update Error");
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["name"] = Institute.Name;
            parameter["comment"] = Institute.Comment;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
