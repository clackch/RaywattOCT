using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System.Collections.Generic;

namespace RaywattOCTFFR.ViewModels.Setting
{
    public partial class SettingLocalizationViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingLocalizationViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Dictionary<string, string> _languageComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private string _currentLanguage;

        private string OriginLanguage;

        public SettingLocalizationViewModel(SqlManager sqlManager)
        {
            _log.Debug("SettingLocalizationViewModel");

            _sqlManager = sqlManager;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "L10N";
            IList<Configuration> l10Ns = _sqlManager.SelectConfiguration(sqlParameters);

            foreach(Configuration l10n in l10Ns)
            {
                LanguageComboBox[l10n.Key] = _l10n[l10n.Key];
                if ("Y".Equals(l10n.Value))
                    CurrentLanguage = l10n.Key;
            }
            
            OriginLanguage = CurrentLanguage;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            ApplyChange();
        }

        private void ApplyChange()
        {
            _log.Debug("ApplyChange");

            if (OriginLanguage.Equals(CurrentLanguage))
            {
                _log.Debug(OriginLanguage + " = " + CurrentLanguage);
            }
            else
            {
                _l10n.ChangeLanguage(CurrentLanguage);

                //Home 이동 or Refresh
                if (Constants.CurrentPage == Constants.PatientListPage)
                {
                    WeakReferenceMessenger.Default.Send(new NavigationMessage("Refresh"));
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage));
                }
            }
        }
    }
}
