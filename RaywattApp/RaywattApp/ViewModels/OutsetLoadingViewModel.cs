using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Interop;
using System.Windows.Threading;
using static RaywattOCT.Ray3DWrapper;

namespace RaywattApp.ViewModels
{
    public partial class OutsetLoadingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OutsetLoadingViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private DispatcherTimer timer = new DispatcherTimer();

        [ObservableProperty]
        private double _progress;
        public OutsetLoadingViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("OutsetLoadingViewModel");

            Constants.CurrentPage = Constants.OutsetLoadingPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            // Terms and Contidions 확인
            IList<Configuration> tnCs = _sqlManager.SelectConfigurationTnC();
            if (tnCs != null || tnCs.Count == 1)
            {
                if ("N".Equals(tnCs[0].Value))
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["tnC"] = tnCs[0];
                    var result = _dialogService.OpenDialog(new TermsConditionsControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

                    if (result != null && result.DialogAnswer == DialogResults.Answer.No)
                    {
                        CommonUtil.Exit(DeviceStatus);
                    }
                }
            }

            timer.Interval = TimeSpan.FromMilliseconds(1);
            timer.Tick += new EventHandler(ProgressTest);
            timer.Start();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void ProgressTest(object sender, EventArgs e)
        {
            if (Progress >= 100)
            {
                IntPtr hWnd = new WindowInteropHelper(Constants.mainWindow).Handle;
                ODSOCT_CreateDll(hWnd);
                ODSOCT_CreateOCTWindowByPos(Ray3DViewID.CutView, (int) Constants.CutView3dX, (int) Constants.CutView3dY, 
                    (int) Constants.CutView3dWidth, (int) Constants.CutView3dHeight);
                ODSOCT_CreateOCTWindowByPos(Ray3DViewID.FlyThrough, (int)Constants.FlyThroughView3dX, (int)Constants.FlyThroughView3dY,
                    (int)Constants.FlyThroughView3dWidth, (int)Constants.FlyThroughView3dHeight);
                ODSOCT_StartRendering();

                timer.Stop();
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
            }

            Progress += 0.5;
        }
    }
}
