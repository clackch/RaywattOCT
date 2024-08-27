using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Angio;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using static RaywattOCT.Ray3DWrapper;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels
{
    public partial class OutsetLoadingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoadingViewModel));

        private readonly SqlManager _sqlManager;

        private readonly AngioManager _angioManager;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        private bool isError = false;

        private ConnectionStatus connState = ConnectionStatus.Default;

        private string errorMsg;

        [ObservableProperty]
        private double _progress;

        public OutsetLoadingViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager)
        {
            _log.Debug("OutsetLoadingViewModel");

            Constants.CurrentPage = Constants.OutsetLoadingPage;

            _sqlManager = sqlManager;
            _angioManager = angioManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            // Terms and Contidions 확인
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Terms&Cond";
            IList<Configuration> tnCs = _sqlManager.SelectConfiguration(sqlParameters);
            if (tnCs != null || tnCs.Count == 1)
            {
                if ("N".Equals(tnCs[0].Value))
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["tnC"] = tnCs[0];
                    var result = _dialogService.OpenDialog(new TermsConditionsControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                    if (result != null && result.DialogAnswer == DialogResults.Answer.No)
                    {
                        DeviceStatus.PowerOffMsg = _l10n["Switching user"];
                        CommonUtil.Exit(DeviceStatus);
                    }
                }
            }

            Thread threadCoreAndDeviceInit = new Thread(() => ThreadCoreAndDeviceInit());
                threadCoreAndDeviceInit.Start();

            timer.Interval = TimeSpan.FromMilliseconds(25);
            timer.Tick += new EventHandler(ProgressTest);
            timer.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void ProgressTest(object sender, EventArgs e)
        {
            if (Progress >= 100 && DeviceStatus.IsServiceStarted && DeviceStatus.IsDeviceConnected)
            {
                IntPtr hWnd = new WindowInteropHelper(Constants.mainWindow).Handle;
                ODSOCT_CreateDll(hWnd);
                ODSOCT_CreateOCTWindowByPos(Ray3DViewID.CutView, (int)Constants.CutView3dX, (int)Constants.CutView3dY,
                    (int)Constants.CutView3dWidth, (int)Constants.CutView3dHeight);
                ODSOCT_CreateOCTWindowByPos(Ray3DViewID.FlyThrough, (int)Constants.FlyThroughView3dX, (int)Constants.FlyThroughView3dY,
                    (int)Constants.FlyThroughView3dWidth, (int)Constants.FlyThroughView3dHeight);
                ODSOCT_StartRendering();
                ODSOCT_EnableInteractor(Ray3DViewID.CutView, false);
                ODSOCT_EnableInteractor(Ray3DViewID.FlyThrough, false);
                ODSOCT_EnableWheelEvent(false);

                timer.Stop();
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
            }
            else if (DeviceStatus.IsServiceStarted && DeviceStatus.IsDeviceConnected)
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
                    CommonUtil.Exit(DeviceStatus, _angioManager, true);
                }
            }

            Progress += 0.25;
        }

        private void ThreadCoreAndDeviceInit()
        {
            _log.Debug("ThreadCoreAndDeviceInit");

            RayError result = RayError.OK;

            result |= (RayError)RayStartSystem();
            if (result == RayError.OK)
            {
                result |= (RayError)RayConnectDevices();
                if (result == RayError.OK)
                {
                    connState = _angioManager.ConnectToServer();
                    switch (connState)
                    {
                        case ConnectionStatus.Success:
                            DeviceStatus.IsDeviceConnected = true;
                            break;
                        case ConnectionStatus.BoardFailure:
                            DeviceStatus.IsDeviceConnected = false;
                            errorMsg = "$MSG014";
                            isError = true;
                            break;
                    }
                }
                else
                {
                    errorMsg = "$MSG011";
                    isError = true;
                }
            }
            else
            {
                errorMsg = "$MSG011";
                isError = true;
            }

            _log.Debug("ThreadCoreAndDeviceInit - Done");
        }
    }
}
