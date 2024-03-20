using log4net;
using RaywattApp.Common.Bases;

namespace RaywattApp.ViewModels.Setting
{
    public class SettingAcquisitionViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingAcquisitionViewModel));

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }
    }
}
