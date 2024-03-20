using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class PhysicianDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PhysicianDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private IList<Physician> _physicianList;

        [ObservableProperty]
        private Physician _selectedPhysician;

        [ObservableProperty]
        private string _searchKeyword;

        private ICommand _searchCommand;
        public ICommand SearchCommand
        {
            get { return this._searchCommand ?? (this._searchCommand = new RelayCommand(Search)); }
        }

        public PhysicianDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            SearchKeyword = "";
            Search();
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            int selectedPhysicianId = (int)data["selectedPhysicianId"];

            SelectedPhysician = PhysicianList.FirstOrDefault(x => x.Id == selectedPhysicianId);
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedPhysician"] = SelectedPhysician;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private void Search()
        {
            _log.Debug("Search");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["SearchKeyword"] = SearchKeyword.Trim();

            PhysicianList = _sqlManager.SelectPhysicianList(sqlParameters);
        }
    }
}
