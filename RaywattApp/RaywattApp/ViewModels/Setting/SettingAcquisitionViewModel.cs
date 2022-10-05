using log4net;
using RaywattApp.Common.Setting;

namespace RaywattApp.ViewModels.Setting
{
    public class SettingAcquisitionViewModel : SettingBase
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

        override protected void Okay()
        {
            _log.Debug("Okay");
        }

        override protected void Apply()
        {
            _log.Debug("Apply");
        }
    }
}
