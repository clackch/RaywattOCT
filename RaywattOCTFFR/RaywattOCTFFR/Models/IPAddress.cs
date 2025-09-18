using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.Models
{
    public partial class IpAddress : ObservableObject
    {
        
        private string _octet1;
        public string Octet1
        {
            get { return _octet1; }
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    _octet1 = "";
                    OnPropertyChanged(nameof(Octet1));
                    return;
                }

                if (!CommonUtil.ValidateNumber(value))
                    return;

                _octet1 = ValidateOctet(value);
                Msg = "";
                OnPropertyChanged(nameof(Octet1));
            }
        }

        private string _octet2;
        public string Octet2
        {
            get { return _octet2; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _octet2 = "";
                    OnPropertyChanged(nameof(Octet2));
                    return;
                }

                if (!CommonUtil.ValidateNumber(value))
                    return;

                _octet2 = ValidateOctet(value);
                Msg = "";
                OnPropertyChanged(nameof(Octet2));
            }
        }

        private string _octet3;
        public string Octet3
        {
            get { return _octet3; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _octet3 = "";
                    OnPropertyChanged(nameof(Octet3));
                    return;
                }

                if (!CommonUtil.ValidateNumber(value))
                    return;

                _octet3 = ValidateOctet(value);
                Msg = "";
                OnPropertyChanged(nameof(Octet3));
            }
        }

        private string _octet4;
        public string Octet4
        {
            get { return _octet4; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _octet4 = "";
                    OnPropertyChanged(nameof(Octet4));
                    return;
                }

                if (!CommonUtil.ValidateNumber(value))
                    return;

                _octet4 = ValidateOctet(value);
                Msg = "";
                OnPropertyChanged(nameof(Octet4));
            }
        }

        public void SetIpAddress(string ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress)) 
            {
                string[] temp = ipAddress.Split(".");

                if (temp.Length == 4)
                {
                    Octet1 = temp[0];
                    Octet2 = temp[1];
                    Octet3 = temp[2];
                    Octet4 = temp[3];
                }
            }
        }

        public string GetIpAddress()
        {
            if(!string.IsNullOrEmpty(_octet1) && !string.IsNullOrEmpty(_octet2) && !string.IsNullOrEmpty(_octet3) && !string.IsNullOrEmpty(_octet4))
                return _octet1 + "." + _octet2 + "." + _octet3 + '.' + _octet4;
            return null;
        }

        private static string ValidateOctet(string value)
        {
            int octet = int.Parse(value);
            if (octet < 0 || octet > 255)
            {
                return "";
            }
            else
            {
                return value;
            }
        }

        [ObservableProperty]
        private string _msg;
    }
}
