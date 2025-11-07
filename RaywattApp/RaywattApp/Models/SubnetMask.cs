using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Util;

namespace RaywattApp.Models
{
    public partial class SubnetMask : ObservableObject
    {
        private string _octet1;
        public string Octet1
        {
            get { return _octet1; }
            set
            {
                if (string.IsNullOrEmpty(value))
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

        public void SetSubnetMask(string subnetMask)
        {
            if (!string.IsNullOrEmpty(subnetMask))
            {
                string[] temp = subnetMask.Split(".");

                if (temp.Length == 4)
                {
                    _octet1 = temp[0];
                    _octet2 = temp[1];
                    _octet3 = temp[2];
                    _octet4 = temp[3];
                }
            }
        }

        public string GetSubnetMask()
        {
            if (!string.IsNullOrEmpty(_octet1) && !string.IsNullOrEmpty(_octet2) && !string.IsNullOrEmpty(_octet3) && !string.IsNullOrEmpty(_octet4))
                return _octet1 + "." + _octet2 + "." + _octet3 + '.' + _octet4;
            return null;
        }

        public bool IsValidSubnetMask()
        {
            if(string.IsNullOrEmpty(_octet1) || string.IsNullOrEmpty(_octet2) || string.IsNullOrEmpty(_octet3) || string.IsNullOrEmpty(_octet4))
                return false;

            int[] maskParts = new int[4];
            maskParts[0] = int.Parse(_octet1);
            maskParts[1] = int.Parse(_octet2);
            maskParts[2] = int.Parse(_octet3);
            maskParts[3] = int.Parse(_octet4);

            string binaryMask = string.Join("", maskParts.Select(p => Convert.ToString(p, 2).PadLeft(8, '0')));
            if (binaryMask.Contains("01")) 
                return false; // 중간에 0과 1이 섞이면 잘못된 서브넷 마스크

            return true;
        }

        private static string ValidateOctet(string value)
        {
            int[] validMasks = { 0, 128, 192, 224, 240, 248, 252, 254, 255 };
            
            if (!validMasks.Contains(int.Parse(value)))
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
