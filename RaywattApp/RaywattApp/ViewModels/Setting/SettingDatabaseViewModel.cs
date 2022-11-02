using log4net;
using RaywattApp.Common.Setting;

namespace RaywattApp.ViewModels.Setting
{
    public class SettingDatabaseViewModel : SettingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingDatabaseViewModel));

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Okay()
        {
            _log.Debug("Okay");
        }

        protected override void Apply()
        {
            _log.Debug("Apply");
        }
    }
}
