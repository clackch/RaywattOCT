using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Setting;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingLocalizationViewModel : SettingBase
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

        protected override void Okay()
        {
            _log.Debug("Okay");

            ApplyChange();

            //Close Popup
            CloseDialog();
        }

        protected override void Apply()
        {
            _log.Debug("Apply");

            ApplyChange();

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Refresh"));
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
