using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.Models
{
    public partial class DicomServer : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _aeTitle;

        [ObservableProperty]
        private string _hostname;

        [ObservableProperty]
        private bool _specifyIpAddress;

        [ObservableProperty]
        private string _ipAddress;

        private string _port;
        public string Port
        {
            get { return _port; }
            set
            {
                if (!CommonUtil.ValidateNumber(value))
                    return;

                _port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        [ObservableProperty]
        private bool _tlsYn;

        [ObservableProperty]
        private string _serverType;

        [ObservableProperty]
        private string _comment;

        [ObservableProperty]
        private string _caFilePath;

        [ObservableProperty]
        private DateTime createDate;

        [ObservableProperty]
        private DateTime updateDate;
    }
}
