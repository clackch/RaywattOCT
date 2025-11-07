using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingDicomViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingDicomViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private LocalHost _localHost;

        [ObservableProperty]
        private IList<DicomServer> _dicomServers;

        [ObservableProperty]
        private bool _isEditLocalHost = false;

        [ObservableProperty]
        private bool _isEditDicomServer = false;

        private ICommand _localHostEditCommand;
        public ICommand LocalHostEditCommand
        {
            get { return this._localHostEditCommand ?? (this._localHostEditCommand = new RelayCommand(LocalHostEdit)); }
        }

        private ICommand _dicomServerAddCommand;
        public ICommand DicomServerAddCommand
        {
            get { return this._dicomServerAddCommand ?? (this._dicomServerAddCommand = new RelayCommand(DicomServerAdd)); }
        }

        private ICommand _dicomServerEditCommand;
        public ICommand DicomServerEditCommand
        {
            get { return this._dicomServerEditCommand ?? (this._dicomServerEditCommand = new RelayCommand<DicomServer>(DicomServerEdit)); }
        }

        public SettingDicomViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingDicomViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            LocalHost = CommonUtil.GetNetworkInfo();
            LocalHost.AeTitle = "";

            if(!string.IsNullOrEmpty(LocalHost.AdapterName))
            {
                IsEditLocalHost = true;

                Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
                sqlParameters["classification"] = "LocalHost";
                IList<Configuration> localHost = _sqlManager.SelectConfiguration(sqlParameters);
                if (localHost != null && localHost.Count > 0)
                {
                    string aeTitle = localHost.FirstOrDefault(x => x.Key == "AeTitle").Value;

                    if (!string.IsNullOrEmpty(aeTitle))
                    {
                        LocalHost.AeTitle = aeTitle;
                        IsEditDicomServer = true;
                    }
                }
            }

            DicomServers = _sqlManager.SelectDicomServer();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void LocalHostEdit()
        {
            _log.Debug("LocalHostEdit");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            LocalHost param = new LocalHost();
            param.AeTitle = LocalHost.AeTitle;
            param.AdapterName = LocalHost.AdapterName;
            param.IsManual = LocalHost.IsManual;
            param.Hostname = LocalHost.Hostname;
            param.IpAddress = LocalHost.IpAddress;
            param.SubnetMask = LocalHost.SubnetMask;
            param.DefaultGateway = LocalHost.DefaultGateway;
            param.PreferredDnsServer = LocalHost.PreferredDnsServer;
            param.AlternateDnsServer = LocalHost.AlternateDnsServer;
            parameter["localHost"] = param;
            var result = _dialogService.OpenDialog(new LocalHostDialogControl(), parameter, Constants.SettingDialogWidth, Constants.SettingDialogHeight);
            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                LocalHost = (LocalHost)data["localHost"];
                IsEditDicomServer = true;
            }
        }

        private void DicomServerAdd()
        {
            _log.Debug("DicomServerAdd");

            DicomServerEdit(null);
        }

        private void DicomServerEdit(DicomServer dicomServer)
        {
            _log.Debug("DicomServerEdit");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            DicomServer param = new DicomServer();
            if(dicomServer != null)
            {
                param.Id = dicomServer.Id;
                param.AeTitle = dicomServer.AeTitle;
                param.Hostname = dicomServer.Hostname;
                param.SpecifyIpAddress = dicomServer.SpecifyIpAddress;
                param.IpAddress = dicomServer.IpAddress;
                param.Port = dicomServer.Port;
                param.TlsYn = dicomServer.TlsYn;
                param.ServerType = dicomServer.ServerType;
                param.Comment = dicomServer.Comment;
            }
            parameter["localHostAeTitle"] = LocalHost.AeTitle;
            parameter["dicomServer"] = param;
            parameter["usePeerVerification"] = CommonUtil.IsTestMode(DeviceStatus.TestMode, "CertIgnore") == true ? false : true;
            var result = _dialogService.OpenDialog(new DicomServerDialogControl(), parameter, Constants.SettingDialogWidth, Constants.SettingDialogHeight);
            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                DicomServers = _sqlManager.SelectDicomServer();
            }
        }
    }
}
