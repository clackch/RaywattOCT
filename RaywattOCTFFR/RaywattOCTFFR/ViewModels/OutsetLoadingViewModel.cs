using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Angio;
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
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Threading;
using static RaywattOCT.Ray3DWrapper;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattOCTFFR.ViewModels
{
    public partial class OutsetLoadingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoadingViewModel));

        private readonly SqlManager _sqlManager;

        private readonly AngioManager _angioManager;

        private readonly IPasswordService _passwordService;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        private bool isError;

        private ConnectionStatus connState = ConnectionStatus.Default;

        private string errorMsg;

        [ObservableProperty]
        private double _progress;

        public OutsetLoadingViewModel(SqlManager sqlManager, IDialogService dialogService, AngioManager angioManager, IPasswordService passwordService)
        {
            _log.Debug("OutsetLoadingViewModel");

            Constants.CurrentPage = Constants.OutsetLoadingPage;

            _sqlManager = sqlManager;
            _angioManager = angioManager;
            _dialogService = dialogService;
            _passwordService = passwordService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            if (navigatedEventArgs is not NavigationEventArgs navArgs)
                return;

            if (CommonUtil.IsRV200())
            {
                if(TermsAndConditionCheck())
                    InitializeSystem();
            }
            else
            {
                InitializeSystem();
            }
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
                int ray3DResult = ODSOCT_CreateDll(hWnd);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_CreateDll Error");
                }
                ray3DResult = ODSOCT_CreateOCTWindowByPos(Ray3DViewID.CutView, (int)Constants.CutView3dX, (int)Constants.CutView3dY,
                    (int)Constants.CutView3dWidth, (int)Constants.CutView3dHeight);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_CreateOCTWindowByPos Error");
                }
                ray3DResult = ODSOCT_CreateOCTWindowByPos(Ray3DViewID.FlyThrough, (int)Constants.FlyThroughView3dX, (int)Constants.FlyThroughView3dY,
                    (int)Constants.FlyThroughView3dWidth, (int)Constants.FlyThroughView3dHeight);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_CreateOCTWindowByPos Error");
                }
                ray3DResult = ODSOCT_StartRendering();
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_StartRendering Error");
                }
                ray3DResult = ODSOCT_EnableInteractor(Ray3DViewID.CutView, false);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_EnableInteractor Error");
                }
                ray3DResult = ODSOCT_EnableInteractor(Ray3DViewID.FlyThrough, false);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_EnableInteractor Error");
                }
                ray3DResult = ODSOCT_EnableWheelEvent(false);
                if (ray3DResult == 0)
                {
                    _log.Error("ODSOCT_EnableWheelEvent Error");
                }

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

            RayError result = (RayError)RayInitSystem();

            result |= (RayError)RaySetConfigPath(Constants.ConfigPath);
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

        private void InitializeSystem()
        {
            Task.Run(() => ThreadCoreAndDeviceInit());

            timer.Tick -= ProgressTest; // 중복 방지
            timer.Tick += ProgressTest;
            timer.Interval = TimeSpan.FromMilliseconds(25);
            timer.Start();
        }

        private bool TermsAndConditionCheck()
        {
            _log.Debug("TermsAndConditionCheck");

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
                    var result = _dialogService.OpenDialog(new TermsConditionsControl_RV200(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                    if (result != null && result.DialogAnswer == DialogResults.Answer.No)
                    {
                        DeviceStatus.PowerOffMsg = _l10n["Switching user"];
                        CommonUtil.Exit(DeviceStatus, _angioManager, false, true);

                        return false;
                    }
                }
            }

            return true;
        }

    }
}
