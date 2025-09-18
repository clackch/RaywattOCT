using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Common.Util;
using System.Management;
using RaywattOCTFFR.Views.Dialog;
using RaywattOCTFFR.Common.Bases;
using System.Linq;
using System.Net;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class LocalHostDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LocalHostDialogViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService? _dialogService;

        [ObservableProperty]
        private LocalHost _localHost;

        [ObservableProperty]
        private TextValidator _aeTitle = new TextValidator();

        [ObservableProperty]
        private IpAddress _ipAddress = new IpAddress();

        [ObservableProperty]
        private SubnetMask _subnetMask = new SubnetMask();

        [ObservableProperty]
        private IpAddress _defaultGateway = new IpAddress();

        [ObservableProperty]
        private IpAddress _preferredDnsServer = new IpAddress();

        [ObservableProperty]
        private IpAddress _alternateDnsServer = new IpAddress();

        public LocalHostDialogViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            LocalHost = (LocalHost)data["localHost"];
            AeTitle.Text = LocalHost.AeTitle;
            IpAddress.SetIpAddress(LocalHost.IpAddress);
            SubnetMask.SetSubnetMask(LocalHost.SubnetMask);
            DefaultGateway.SetIpAddress(LocalHost.DefaultGateway);
            PreferredDnsServer.SetIpAddress(LocalHost.PreferredDnsServer);
            AlternateDnsServer.SetIpAddress(LocalHost.AlternateDnsServer);
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("AnswerYes");

            if (!Validate())
                return;

            if (LocalHost.IsManual)
            {
                _log.Debug("Manual");

                if (!SetStaticIP(LocalHost.AdapterName))
                    return;
                if (!SetDNS(LocalHost.AdapterName))
                    return;
            }
            else
            {
                _log.Debug("Auto");
                if (!EnableDHCP(LocalHost.AdapterName))
                    return;
            }

            LocalHost = CommonUtil.GetNetworkInfo();
            LocalHost.AeTitle = AeTitle.Text.Trim();

            Save();

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["localHost"] = LocalHost;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private bool Validate()
        {
            string message;

            if (string.IsNullOrEmpty(AeTitle.Text.Trim()))
            {
                message = "Enter AE Title";
                AeTitle.Msg = message;
                return false;
            }

            if (!LocalHost.IsManual)
                return true;

            string ip = IpAddress.GetIpAddress();
            if (!IsValidIPAddress(ip))
            {
                message = "Invalid IP address.";
                IpAddress.Msg = message;
                return false;
            }

            string subnet = SubnetMask.GetSubnetMask();
            if (!IsValidSubnetMask(subnet))
            {
                message = "Invalid subnet mask.";
                SubnetMask.Msg = message;
                return false;
            }

            string gateway = DefaultGateway.GetIpAddress();
            if (!IsValidIPAddress(gateway))
            {
                message = "Invalid default gateway.";
                DefaultGateway.Msg = message;
                return false;
            }

            string dns1 = PreferredDnsServer.GetIpAddress();
            if (!IsValidIPAddress(dns1))
            {
                message = "Invalid Preferred DNS server.";
                PreferredDnsServer.Msg = message;
                return false;
            }

            string dns2 = AlternateDnsServer.GetIpAddress();
            if (!string.IsNullOrEmpty(dns2) && !IsValidIPAddress(dns2))
            {
                message = "Invalid Alternate DNS server.";
                AlternateDnsServer.Msg = message;
                return false;
            }

            return true;
        }

        private void Save()
        {
            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "LocalHost";
            sqlParameters["key"] = "AeTitle";
            sqlParameters["value"] = AeTitle.Text.Trim();
            sqlParameters["buffer"] = "";

            int res = _sqlManager.UpdateConfiguration(sqlParameters);
            if (res != 1)
            {
                _log.Error("Update Error");
            }
        }

        private bool SetStaticIP(string adapterName)
        {
            string ip = IpAddress.GetIpAddress();
            string subnet = SubnetMask.GetSubnetMask();
            string gateway = DefaultGateway.GetIpAddress();

            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] && mo["Description"].ToString() == adapterName)
                    {
                        // ✅ 1. 정적 IP 설정
                        ManagementBaseObject inParams = mo.GetMethodParameters("EnableStatic");
                        inParams["IPAddress"] = new string[] { ip };
                        inParams["SubnetMask"] = new string[] { subnet };

                        ManagementBaseObject outParams = mo.InvokeMethod("EnableStatic", inParams, null);
                        long returnCode = Convert.ToInt64(outParams["ReturnValue"]);
                        PrintResult(returnCode);

                        if (returnCode != 0)
                            return false;

                        _log.Debug($"✅ 정적 IP 설정 완료: {ip}");

                        // ✅ 2. 게이트웨이 설정
                        inParams = mo.GetMethodParameters("SetGateways");
                        inParams["DefaultIPGateway"] = new string[] { gateway };
                        inParams["GatewayCostMetric"] = new int[] { 1 };

                        outParams = mo.InvokeMethod("SetGateways", inParams, null);
                        returnCode = Convert.ToInt64(outParams["ReturnValue"]);
                        PrintResult(returnCode);

                        if (returnCode != 0)
                            return false;

                        _log.Debug($"✅ 기본 게이트웨이 설정 완료: {gateway}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug("❌ 정적 IP 설정 중 오류 발생: " + ex.Message);
            }

            return true;
        }

        private bool SetDNS(string adapterName)
        {
            string dns1 = PreferredDnsServer.GetIpAddress();
            string dns2 = AlternateDnsServer.GetIpAddress();

            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] && mo["Description"].ToString() == adapterName)
                    {
                        // ✅ 1. 기본 DNS 설정
                        ManagementBaseObject inParams = mo.GetMethodParameters("SetDNSServerSearchOrder");
                        inParams["DNSServerSearchOrder"] = string.IsNullOrEmpty(dns2) ? new string[] { dns1 } : new string[] { dns1, dns2 };

                        ManagementBaseObject outParams = mo.InvokeMethod("SetDNSServerSearchOrder", inParams, null);
                        long returnCode = Convert.ToInt64(outParams["ReturnValue"]);
                        PrintResult(returnCode);

                        if (returnCode != 0)
                            return false;

                        _log.Debug($"✅ DNS 서버 설정 완료: {dns1}, {dns2}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug("❌ DNS 설정 중 오류 발생: " + ex.Message);
            }

            return true;
        }

        private bool EnableDHCP(string adapterName)
        {
            try
            {
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    // ✅ 해당 네트워크 어댑터가 사용 가능한지 확인
                    if ((bool)mo["IPEnabled"] && mo["Description"].ToString() == adapterName)
                    {
                        // ✅ 1. DHCP 활성화
                        ManagementBaseObject outParams = mo.InvokeMethod("EnableDHCP", null, null);
                        long returnCode = Convert.ToInt64(outParams["ReturnValue"]);
                        PrintResult(returnCode);

                        if (returnCode != 0)
                            return false;

                        // ✅ 2. DNS 자동 설정 (DHCP 기반 변경)
                        ManagementBaseObject dnsParams = mo.GetMethodParameters("SetDNSServerSearchOrder");
                        dnsParams["DNSServerSearchOrder"] = null;

                        outParams = mo.InvokeMethod("SetDNSServerSearchOrder", dnsParams, null);
                        returnCode = Convert.ToInt64(outParams["ReturnValue"]);
                        PrintResult(returnCode);

                        if (returnCode != 0)
                            return false;

                        _log.Debug("✅ DHCP 및 DNS 자동 설정이 완료되었습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug("❌ DHCP 설정 중 오류 발생: " + ex.Message);
            }

            return true;
        }

        private void PrintResult(long returnCode)
        {
            _log.Debug("ReturnValue : " + returnCode);
            string description = GetReturnValueDescription(returnCode);
            _log.Debug("Description : " + description);

            if (returnCode != 0)
            {
                ShowMessage(description);
            }
        }

        private void ShowMessage(string message)
        {
            _log.Debug(message);

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = message;
            var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.SettingDialogWidth, Constants.SettingDialogHeight);
        }

        private static string GetReturnValueDescription(long returnCode)
        {
            switch (returnCode)
            {
                case 0: return "Success";//성공
                case 1: return "Restart Required";//재부팅이 필요합니다.
                case 64: return "Method Not Supported";//네트워크 어댑터에서 해당 메서드를 지원하지 않습니다.
                case 65: return "Unknown Failure";//알 수 없는 실패
                case 66: return "Invalid Subnet Mask";//잘못된 서브넷 마스크
                case 67: return "Gateway Not Reachable";//게이트웨이에 도달할 수 없습니다.
                case 68: return "Invalid IP Address";//잘못된 IP 주소
                case 69: return "Invalid Gateway IP Address";//잘못된 게이트웨이 주소
                case 70: return "IP Address Already Exists";//동일한 IP 주소가 이미 존재합니다.
                case 71: return "IP Address Is In Use";//IP 주소가 이미 사용 중입니다.
                case 72: return "Invalid Input Parameter";//잘못된 입력 매개변수
                case 73: return "Invalid DHCP Lease Time";//유효하지 않은 DHCP 임대 시간
                case 74: return "Command Not Applicable";//현재 환경에서 명령을 적용할 수 없습니다.
                case 75: return "DNS Server Not Reachable";//지정된 DNS 서버에 연결할 수 없습니다.
                case 2147942405: return "Access Denied / Run as Administrator";//관리자 권한이 필요합니다.
                case 2147950655: return "WMI Access Denied";//WMI 권한 부족
                case 2147749890: return "Adapter Not Found";//네트워크 어댑터가 존재하지 않음
                default: return $"Unknown Error: {returnCode}";//알 수 없는 오류
            }
        }

        private static bool IsValidIPAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress ip))
                return false;

            // IPv4만 필터링
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;


            // 특정 범위 제외 (127.x.x.x)            
            byte firstOctet = ip.GetAddressBytes()[0];
            if (firstOctet == 127 || firstOctet == 0 || firstOctet > 223)
                return false;

            return true;
        }

        private static bool IsValidSubnetMask(string subnetMask)
        {
            if (string.IsNullOrWhiteSpace(subnetMask)) return false;

            string[] parts = subnetMask.Split('.');
            if (parts.Length != 4) return false;

            int[] validMasks = { 0, 128, 192, 224, 240, 248, 252, 254, 255 };
            int[] maskParts = new int[4];

            for (int i = 0; i < 4; i++)
            {
                if (!int.TryParse(parts[i], out maskParts[i])) return false;
                if (!validMasks.Contains(maskParts[i])) return false;
            }

            // 모든 옥텟을 이진수로 변환하고 합쳐서 검증
            string binaryMask = string.Join("", maskParts.Select(p => Convert.ToString(p, 2).PadLeft(8, '0')));

            // 유효한 서브넷 마스크인지 확인 (1이 연속된 후에만 0이 나와야 함)
            if (binaryMask.Contains("01")) return false;

            // 서브넷 마스크가 `0.0.0.0`이면 유효하지 않음
            if (binaryMask == "00000000000000000000000000000000") return false;

            return true;
        }
    }
}
