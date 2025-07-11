using System.Collections.Generic;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using RaywattApp.Views.Dialog;
using RaywattApp.Common.Bases;
using RayCoreWrapper;
using RaywattApp.Common.Util;
using System.Runtime.InteropServices;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class DicomServerDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DicomServerDialogViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService? _dialogService;

        [ObservableProperty]
        private string _localHostAeTitle;

        [ObservableProperty]
        private DicomServer _dicomServer = new DicomServer();

        [ObservableProperty]
        private IpAddress _ipAddress = new IpAddress();

        [ObservableProperty]
        private TextValidator _aeTitle = new TextValidator();

        [ObservableProperty]
        private TextValidator _port = new TextValidator();

        private string _hostname;
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                _hostname = value;
                HostnameMsg = "";
                OnPropertyChanged(nameof(Hostname));
            }
        }

        [ObservableProperty]
        private string _hostnameMsg;

        [ObservableProperty]
        private string _orgHostname;

        [ObservableProperty]
        private bool _isChecking = false;

        [ObservableProperty]
        private bool _isNew;

        private IntPtr dicomClient;

        private bool usePeerVerification = true;

        private string previousServerType = "";

        private ICommand _updateHostnameCommand;
        public ICommand UpdateHostnameCommand
        {
            get { return this._updateHostnameCommand ?? (this._updateHostnameCommand = new RelayCommand(async () => await SetIpAddressAsync())); }
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get { return this._deleteCommand ?? (this._deleteCommand = new RelayCommand<IDialogWindow>(Delete)); }
        }

        public DicomServerDialogViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Port.SetSpecialType(1);

            dicomClient = RayExportWrapper.CreateDcmClient();
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            LocalHostAeTitle = (string)data["localHostAeTitle"];
            DicomServer = (DicomServer)data["dicomServer"];
            this.usePeerVerification = (bool)data["usePeerVerification"];
            if (string.IsNullOrEmpty(DicomServer.AeTitle))
            {
                IsNew = true;
            }
            else
            {
                IpAddress.SetIpAddress(DicomServer.IpAddress);
                AeTitle.Text = DicomServer.AeTitle;
                Port.Text = DicomServer.Port;
                Hostname = DicomServer.Hostname;
                previousServerType = DicomServer.ServerType;

                IsNew = false;
            }
        }

        protected override async void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("AnswerYes");

            if (!Validate())
                return;

            //DICOM Server Connection Test
            bool res = await ConnectionTest();
            if (!res)
                return;

            if (IsNew && CanSaveNewDicomServer())
                SaveNew();

            if (!IsNew)
                SaveEdit();

            RayExportWrapper.DestroyDcmClient(dicomClient);

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            CloseDialogWithResult(dialog, dialogResults);
        }

        private bool Validate()
        {
            string message;

            string aeTitle = AeTitle.Text.Trim();
            if (string.IsNullOrEmpty(aeTitle))
            {
                message = "Enter AE Title";
                AeTitle.Msg = message;
                return false;
            }

            string ip = IpAddress.GetIpAddress();
            if (!IsValidIPAddress(ip))
            {
                message = "Invalid IP address.";
                IpAddress.Msg = message;
                return false;
            }

            string port = Port.Text.Trim();
            if (string.IsNullOrEmpty(port))
            {
                message = "Enter Port";
                Port.Msg = message;
                return false;
            }

            DicomServer.AeTitle = aeTitle;
            DicomServer.Hostname = Hostname;
            DicomServer.IpAddress = ip;
            DicomServer.Port = port;

            return true;
        }

        private async Task<bool> ConnectionTest()
        {
            bool tlsYn = false;
            RayExportWrapper.ServerType serverType = RayExportWrapper.ServerType.UNKNOWN;
            IntPtr caFilePathPtr = IntPtr.Zero;
            IsChecking = true;
            RayExportWrapper.DicomNetRWError res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.TestConnection(dicomClient, LocalHostAeTitle, DicomServer.IpAddress, int.Parse(DicomServer.Port), DicomServer.AeTitle, this.usePeerVerification, out tlsYn, out serverType, out caFilePathPtr));
            _log.DebugFormat("TestConnection : {0}", res);
            IsChecking = false;

            if (res != RayExportWrapper.DicomNetRWError.Normal)
            {
                ShowAlertDialog(CommonUtil.GetDicomResultMessage(res));
                return false;
            }

            if(serverType == RayExportWrapper.ServerType.UNKNOWN)
            {
                ShowAlertDialog("Unknown server type. Please verify the configuration for PACS or MWL.");
                return false;
            }

            DicomServer.TlsYn = tlsYn;
            DicomServer.ServerType = serverType.ToString();
            DicomServer.CaFilePath = IntPtr.Zero != caFilePathPtr ? Marshal.PtrToStringAnsi(caFilePathPtr) : string.Empty;
            if (caFilePathPtr != IntPtr.Zero)
                RayExportWrapper.FreeMemory(caFilePathPtr);

            return true;
        }

        private bool CanSaveNewDicomServer()
        {
            var existingServers = _sqlManager.SelectDicomServer().Where(x => x.AeTitle == DicomServer.AeTitle && x.IpAddress == DicomServer.IpAddress && x.Port == DicomServer.Port).ToList();

            if (existingServers.Count() == 0)
                return true;

            if (existingServers.Count() >= 2)
            {
                ShowAlertDialog("The DICOM server already exists.");
                return false;
            }

            else if (existingServers.First().ServerType == DicomServer.ServerType)
            {
                ShowAlertDialog("The DICOM server already exists.");
                return false;
            }

            else if (DicomServer.ServerType == "BOTH")
            {
                if (existingServers.First().ServerType == "PACS")
                    DicomServer.ServerType = "MWL";
                else if (existingServers.First().ServerType == "MWL")
                    DicomServer.ServerType = "PACS";
                else
                {
                    ShowAlertDialog("The existing server type is incorrect. Please remove the existing server information to avoid conflicts.");
                    return false;
                }
            }

            return true;
        }

        private void SaveNew()
        {
            bool isBoth = DicomServer.ServerType == "BOTH";
            string[] serverTypes = isBoth ? new[] { "PACS", "MWL" } : new[] { DicomServer.ServerType };

            foreach (var serverType in serverTypes)
            {
                Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
                sqlParameters["ae_title"] = AeTitle.Text.Trim();
                sqlParameters["hostname"] = DicomServer.SpecifyIpAddress ? "" : string.IsNullOrEmpty(Hostname) ? "" : Hostname.Trim();
                sqlParameters["specify_ip_address"] = DicomServer.SpecifyIpAddress;
                sqlParameters["ip_address"] = IpAddress.GetIpAddress();
                sqlParameters["port"] = Port.Text;
                sqlParameters["tls_yn"] = DicomServer.TlsYn;
                sqlParameters["server_type"] = serverType;
                sqlParameters["comment"] = DicomServer.Comment;
                sqlParameters["ca_file_path"] = DicomServer.CaFilePath;

                int res = _sqlManager.InsertDicomServer(sqlParameters);

                if (res != 1)
                {
                    _log.Error(IsNew ? "Insert Error" : "Update Error");
                    ShowAlertDialog("Failed to connect to the server.");
                }
            }
        }

        private void SaveEdit()
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["server_type"] = DicomServer.ServerType;

            if (DicomServer.ServerType != previousServerType)
            {
                if (DicomServer.ServerType == "BOTH")
                    sqlParameters["server_type"] = previousServerType;
                else
                {
                    ShowAlertDialog("The server type doesn't match your existing configuration. Please remove the existing server information to avoid conflicts.");
                    return;
                }
            }

            Dictionary<string, object> validateSqlParameters = new Dictionary<string, object>();
            validateSqlParameters["id"] = DicomServer.Id;
            IList<DicomServer> DicomServers = _sqlManager.SelectDicomServer(validateSqlParameters);

            if (DicomServers.Any(x => x.AeTitle == DicomServer.AeTitle && x.IpAddress == DicomServer.IpAddress && x.Port == DicomServer.Port && x.ServerType == DicomServer.ServerType))
            {
                ShowAlertDialog("The DICOM server already exists.");
                return;
            }

            sqlParameters["ae_title"] = AeTitle.Text.Trim();
            sqlParameters["hostname"] = DicomServer.SpecifyIpAddress ? "" : string.IsNullOrEmpty(Hostname) ? "" : Hostname.Trim();
            sqlParameters["specify_ip_address"] = DicomServer.SpecifyIpAddress;
            sqlParameters["ip_address"] = IpAddress.GetIpAddress();
            sqlParameters["port"] = Port.Text;
            sqlParameters["tls_yn"] = DicomServer.TlsYn;
            sqlParameters["comment"] = DicomServer.Comment;
            sqlParameters["ca_file_path"] = DicomServer.CaFilePath;
            sqlParameters["id"] = DicomServer.Id;

            int res = _sqlManager.UpdateDicomServer(sqlParameters);

            if (res != 1)
            {
                _log.Error("Update Error");
                ShowAlertDialog("Failed to connect to the server.");
                return;
            }
        }

        private async Task SetIpAddressAsync()
        {           
            try
            {
                if (string.IsNullOrEmpty(Hostname))
                {
                    IpAddress.SetIpAddress("...");
                    return;
                }

                Hostname = Hostname.Trim();
                
                if (Hostname.Equals(OrgHostname))
                {
                    return;
                }

                IsChecking = true;

                IPAddress[] addresses = await Dns.GetHostAddressesAsync(Hostname);
                var ipv4Addresses = addresses.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                IPAddress representativeIp = ipv4Addresses.FirstOrDefault();

                if (representativeIp != null)
                {
                    _log.Debug($"Hostname: {Hostname} - IPv4 addresses: {string.Join(", ", ipv4Addresses)}");
                    IpAddress.SetIpAddress(representativeIp.ToString());
                }
                else
                {
                    _log.Debug("IPv4 addresses not found");
                    HostnameMsg = "No valid IP address found";
                    IpAddress.SetIpAddress("...");
                }
            }
            catch (SocketException ex)
            {
                _log.Debug($"Hostname not found: {ex.Message}");
                HostnameMsg = "Hostname not found";
                IpAddress.SetIpAddress("...");
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
            finally
            {
                IsChecking = false;
                OrgHostname = Hostname;
            }
        }

        private bool IsValidIPAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress ip))
                return false;

            // IPv4만 필터링
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            // 특정 범위 제외
            byte firstOctet = ip.GetAddressBytes()[0];
            if (firstOctet == 0 || firstOctet > 223)
                return false;

            return true;
        }

        private void Delete(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Confirm deletion of selected DICOM server"];
            var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.DicomServerDialogWidth, Constants.DicomServerDialogHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
                sqlParameters["id"] = DicomServer.Id;

                int res = _sqlManager.DeleteDicomServer(sqlParameters);

                if (res != 1)
                {
                    _log.Error("Delete Error");
                }

                DialogResults dialogResults = new();
                dialogResults.DialogAnswer = DialogResults.Answer.Yes;
                CloseDialogWithResult(dialog, dialogResults);
            }
        }

        private void ShowAlertDialog(string message)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n[message];

            _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.DicomServerDialogWidth, Constants.DicomServerDialogHeight);
        }
    }
}
