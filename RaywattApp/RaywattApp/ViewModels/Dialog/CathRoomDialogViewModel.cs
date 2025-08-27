using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;

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

        private System.Timers.Timer _connectionCheckTimer;
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
            _connectionCheckTimer = new System.Timers.Timer(1000); // 1초마다 실행
            _connectionCheckTimer.Elapsed += OnConnectionCheck;
            _connectionCheckTimer.Start();
        }

        private void OnConnectionCheck(object sender, ElapsedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (!ViewModelBase.DeviceStatus.IsAngioConnected && !Application.Current.Windows.OfType<Window>().Any(w => w.Content is AlertDialogControl))//ViewModelBase._deviceStatus.IsErrorDialogClosed)
                {
                    var targetWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(w => w.DataContext is CathRoomDialogViewModel) as IDialogWindow;
                    AnswerNo(targetWindow);
                    _connectionCheckTimer.Stop();
                }
                else if (this._selectedCathRoom == null && this.DialogResult != null) _connectionCheckTimer.Stop();
            });
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

            if (SelectedCathRoom.Id == -1) // CHP not Selected
            {
                _log.Debug("CathRoom not Selected");
                ViewModelBase.DeviceStatus.IsAngioInitialized = false;
                _angioManager.IsChpFileChangeSuccess = 0;
                _angioManager.isChpFileConnected = 0;
                _angioManager.SendCommandPacket(CommandType.FGStopped);
                _angioManager.ToggleLive(false);
            }
            else
            {
                _angioManager.SendChpFilePacket(SelectedCathRoom.SetupChp);

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
                        _angioManager.ToggleLive(true);
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
            _connectionCheckTimer.Stop();
            CloseDialogWithResult(dialog, dialogResults);
        }
    }
}
