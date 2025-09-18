using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattOCTFFR.ViewModels
{
    public partial class OutsetLoadingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoadingViewModel));

        private readonly SqlManager _sqlManager;

        private readonly IPasswordService _passwordService;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        private bool isError;

        private string errorMsg;

        [ObservableProperty]
        private double _progress;

        public OutsetLoadingViewModel(SqlManager sqlManager, IDialogService dialogService, IPasswordService passwordService)
        {
            _log.Debug("OutsetLoadingViewModel");

            Constants.CurrentPage = Constants.OutsetLoadingPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _passwordService = passwordService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is not NavigationEventArgs navArgs)
                return;

            InitializeSystem();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void ProgressTest(object sender, EventArgs e)
        {
            if (Progress >= 100 && DeviceStatus.IsServiceStarted)
            {
                timer.Stop();
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
            }
            else if (DeviceStatus.IsServiceStarted)
            {
                Progress = 100;
            }
            else if (isError)
            {
                _log.Error("RayStartSystem Error or RayConnectDevices Error");

                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Error"];
                parameter["message"] = _l10n[errorMsg];
                parameter["error"] = true;
                var resultDialog = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                if (resultDialog != null && resultDialog.DialogAnswer == DialogResults.Answer.Undefined)
                {
                    CommonUtil.Exit(DeviceStatus, true);
                }
            }

            Progress += 0.25;
        }

        private void ThreadCoreAndDeviceInit()
        {
            _log.Debug("ThreadCoreAndDeviceInit");

            RayError result = (RayError)RayInitSystem();

            result |= (RayError)RaySetConfigPath(Constants.ConfigPath);
            result |= (RayError)RayStartSystem();

            if (result != RayError.OK)
            {
                errorMsg = "$MSG011";
                isError = true;
            }

            _log.Debug("ThreadCoreAndDeviceInit - Done");
        }

        private void InitializeSystem()
        {
            Task.Run(() => ThreadCoreAndDeviceInit());

            timer.Tick -= ProgressTest; // 중복 방지
            timer.Tick += ProgressTest;
            timer.Interval = TimeSpan.FromMilliseconds(25);
            timer.Start();
        }
    }
}
