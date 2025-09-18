using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RayCoreWrapper;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;

namespace RaywattOCTFFR.ViewModels
{
    public partial class PatientNewDicomViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientNewDicomViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private IList<DicomServer> _dicomServers;

        [ObservableProperty]
        private DicomServer _selectedDicomServer;

        [ObservableProperty]
        private string _localHostAeTitle;

        [ObservableProperty]
        private bool _isChecking = false;

        private IntPtr dicomClient;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next, CanNext)); }
        }

        private ICommand _selectionChangedCommand;
        public ICommand SelectionChangedCommand
        {
            get { return this._selectionChangedCommand ?? (this._selectionChangedCommand = new RelayCommand<DicomServer>(SelectionChanged)); }
        }

        public PatientNewDicomViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientNewViewModel");

            Constants.CurrentPage = Constants.PatientNewDicomPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "LocalHost";
            IList<Configuration> localHost = _sqlManager.SelectConfiguration(sqlParameters);
            if (localHost != null && localHost.Count > 0)
            {
                LocalHostAeTitle = localHost.FirstOrDefault(x => x.Key == "AeTitle").Value;
            }

            DicomServers = _sqlManager.SelectDicomServer();
            dicomClient = RayExportWrapper.CreateDcmClient();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                PrevStatus = (PrevStatus)data["prevStatus"];
                if (data.TryGetValue("selectedDicomServer", out var serverObj) && serverObj is DicomServer temp)
                {
                    SelectedDicomServer = DicomServers.FirstOrDefault(x => x.Id == temp.Id);
                }

            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            RayExportWrapper.DestroyDcmClient(dicomClient);
            dicomClient = IntPtr.Zero;
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage) { Parameter = parameter });
        }

        private async void Next()
        {
            _log.Debug("Next");

            //Connection Test
            bool res = await ConnectionTest();
            if (!res)
                return;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            parameter["localHostAeTitle"] = LocalHostAeTitle;
            parameter["selectedDicomServer"] = SelectedDicomServer;

            if (SelectedDicomServer.ServerType == "PACS")
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientNewDicomPacsPage) { Parameter = parameter });

            if (SelectedDicomServer.ServerType == "MWL")
                WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientNewDicomMwlPage) { Parameter = parameter });
        }

        private async Task<bool> ConnectionTest()
        {
            IsChecking = true;
            bool usePeerVerification = CommonUtil.IsTestMode(DeviceStatus.TestMode, "CertIgnore") == true ? false : true;
            RayExportWrapper.DicomNetRWError res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Initialize(dicomClient, LocalHostAeTitle, SelectedDicomServer.IpAddress, int.Parse(SelectedDicomServer.Port), SelectedDicomServer.AeTitle, SelectedDicomServer.TlsYn, usePeerVerification, SelectedDicomServer.CaFilePath));
            _log.DebugFormat("Initialize : {0}", res);
            IsChecking = false;

            if (res != RayExportWrapper.DicomNetRWError.Normal)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = CommonUtil.GetDicomResultMessage(res);
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                return false;
            }

            return true;
        }

        private bool CanNext()
        {
            _log.Debug("CanNext");

            return SelectedDicomServer == null ? false : true;
        }

        private void SelectionChanged(DicomServer dicomServer)
        {
            _log.Debug("SelectionChanged");

            (NextCommand as RelayCommand).NotifyCanExecuteChanged();
        }
    }
}
