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
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Navigation;
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

            if (navigatedEventArgs is not NavigationEventArgs navArgs || navArgs.ExtraData is not Dictionary<string, object> data)
                return;

            string id = data["id"] as string;
            string password = data["password"] as string;

            if (!_passwordService.CheckLoginWithRetryCount(id, password)) return;

            var user = GetUserById(id);

            DeviceStatus.LoginID = id;
            _passwordService.ResetPasswordCount();

            if (HandleInitialPasswordReset(user)) return;
            if (CheckPasswordExpiry(user)) return;
            if (!EnsureTermsAgreement(user)) return;

            if(user.Admin)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.UserListPage));
                return;
            }

            InitializeSystem();
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

        private User? GetUserById(string id)
        {
            var sqlParams = new Dictionary<string, object>
            {
                ["id"] = id,
            };

            var users = _sqlManager.SelectUserById(sqlParams);
            return users?.Count > 0 ? users[0] : null;
        }

        private bool HandleInitialPasswordReset(User user)
        {
            if (user.PasswordReset)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["user"] = user;

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.InitialPasswordSetupPage) { Parameter = parameter });
                return true;
            }
            return false;
        }

        private bool CheckPasswordExpiry(User user)
        {
            if ((DateTime.Now - user.PasswordChangedAt).TotalDays > _passwordService.PasswordExpiryDays)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["user"] = user;

                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PasswordExpiryCheckPage) { Parameter = parameter });
                return true;
            }
            return false;
        }


        private bool EnsureTermsAgreement(User user)
        {
            if (user.TermsAgreedAt > DateTime.MinValue) return true;

            var parameter = new Dictionary<string, object> { ["tnC"] = user };
            var result = _dialogService.OpenDialog(
                new TermsConditionsControl(), parameter,
                Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result?.DialogAnswer == DialogResults.Answer.No)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
                return false;
            }

            _sqlManager.UpdateTermsAgreedDateUser(new Dictionary<string, object>
            {
                ["id"] = user.Id,
                ["password"] = user.Password,
                ["admin"] = user.Admin
            });

            return true;
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
