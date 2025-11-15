using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Bases;

namespace RaywattOCTFFR.ViewModels.Setting
{
    public partial class SettingTermsConditionsViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingTermsConditionsViewModel));

        [ObservableProperty]
        private string _termsAndConditions;

        public SettingTermsConditionsViewModel()
        {
            _log.Debug("SettingTermsConditionsViewModel");

            TermsAndConditions = _l10n["$Terms and Conditions"];
        }

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
