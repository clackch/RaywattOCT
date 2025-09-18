using log4net;
using RaywattOCTFFR.Common.Bases;

namespace RaywattOCTFFR.ViewModels.Setting
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
