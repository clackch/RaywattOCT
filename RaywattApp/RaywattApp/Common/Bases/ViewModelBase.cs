using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Localization;

namespace RaywattApp.Common.Bases
{
    /// <summary>
    /// ViewModelBase
    /// </summary>
    public abstract class ViewModelBase : ObservableObject, INavigationAware
    {
        private string _title;
        /// <summary>
        /// Title
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private string _message;
        /// <summary>
        /// Message
        /// </summary>
        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        protected readonly DynamicResource _l10n;

        public ViewModelBase()
        {
            _l10n = (DynamicResource)App.Current.Resources["L10N"];
        }

        /// <summary>
        /// Navigation 시작시 - 이동 시작하는 화면에서 발생
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="navigationEventArgs"></param>
        public virtual void OnNavigating(object sender, object navigationEventArgs)
        {
        }

        /// <summary>
        /// Navigation 완료시 - 이동 완료된 화면에서 발생
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="navigatedEventArgs"></param>
        public virtual void OnNavigated(object sender, object navigatedEventArgs)
        {
        }
    }
}
