using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Setting;
using RaywattApp.Services;

namespace RaywattApp.ViewModels.Setting
{
    public class SettingServiceViewModel : SettingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingServiceViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        public SettingServiceViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingServiceViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

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
