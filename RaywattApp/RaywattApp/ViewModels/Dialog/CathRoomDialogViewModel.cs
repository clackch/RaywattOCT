using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class CathRoomDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CathRoomDialogViewModel));

        private readonly SqlManager _sqlManager;
        private IDialogService? _dialogService;
        private readonly AngioManager _angioManager;

        [ObservableProperty]
        private IList<CathRoom> _cathRoomList;

        [ObservableProperty]
        private CathRoom _selectedCathRoom;

        public CathRoomDialogViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _angioManager = angioManager;

            CathRoomList = _sqlManager.SelectCathRoomList();

            var notSelectedItem = new RaywattApp.Models.CathRoom
            {
                Id = -1,
                Name = "Not Selected"
            };
            CathRoomList.Insert(0, notSelectedItem);
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

            if (SelectedCathRoom.Id != -1)
            {

                _angioManager.SendChpFilePacket(SelectedCathRoom.AppChp);

                while (_angioManager.IsChpFileChangeSuccess == 0)
                {
                    Thread.Sleep(500);
                }
                if (_angioManager.IsChpFileChangeSuccess == 1)
                {
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["$MSG012"];

                    if (_angioManager.ReadyToRecv)
                    {
                        _angioManager.SendCommandPacket(CommandType.FGStarted);
                    }

                    _dialogService.OpenDialog(new AlertDialogControl(), parameter, Common.Bases.Constants.ApplicationWidth, Common.Bases.Constants.ApplicationHeight);
                }
                else if (_angioManager.IsChpFileChangeSuccess == -1)
                {
                    parameter["title"] = _l10n["Error"];
                    parameter["message"] = _l10n["$MSG013"];
                    parameter["error"] = true;

                    _dialogService.OpenDialog(new AlertDialogControl(), parameter, Common.Bases.Constants.ApplicationWidth, Common.Bases.Constants.ApplicationHeight);
                }
                _angioManager.IsChpFileChangeSuccess = 0;
            }

            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
