using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class CathRoomDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CathRoomDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private IList<CathRoom> _cathRoomList;

        [ObservableProperty]
        private CathRoom _selectedCathRoom;

        public CathRoomDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            CathRoomList = _sqlManager.SelectCathRoomList();
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            int selectedCathRoomId = (int)data["selectedCathRoomId"];

            SelectedCathRoom = CathRoomList.FirstOrDefault(x => x.Id == selectedCathRoomId);
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedCathRoom"] = SelectedCathRoom;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
