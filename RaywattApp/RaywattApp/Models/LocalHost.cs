using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class LocalHost : ObservableObject
    {
        [ObservableProperty]
        private string _aeTitle;

        [ObservableProperty]
        private string _adapterName;

        [ObservableProperty]
        private bool _isManual;

        [ObservableProperty]
        private string _hostname;

        [ObservableProperty]
        private string _ipAddress;

        [ObservableProperty]
        private string _subnetMask;

        [ObservableProperty]
        private string _defaultGateway;

        [ObservableProperty]
        private string preferredDnsServer;

        [ObservableProperty]
        private string alternateDnsServer;

        [ObservableProperty]
        private DateTime createDate;

        [ObservableProperty]
        private DateTime updateDate;
    }
}
