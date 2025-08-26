using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Localization;
using RaywattApp.Models;

namespace RaywattApp.Common.Bases
{
    /// <summary>
    /// ViewModelBase
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject, INavigationAware
    {
        protected readonly DynamicResource _l10n;

        private static DeviceStatus _deviceStatus = new DeviceStatus();

        public static DeviceStatus DeviceStatus
        {
            get => _deviceStatus;
            set => _deviceStatus = value;
        }

        public ViewModelBase()
        {
            _l10n = (DynamicResource)App.Current.Resources["L10N"];
        }

        /// <summary>
        /// Navigation 시작시 - 이동 시작하는 화면에서 발생
        /// </summary>
        public virtual void OnNavigating(object sender, object navigationEventArgs)
        {
        }

        /// <summary>
        /// Navigation 완료시 - 이동 완료된 화면에서 발생
        /// </summary>
        public virtual void OnNavigated(object sender, object navigatedEventArgs)
        {
        }
    }
}
