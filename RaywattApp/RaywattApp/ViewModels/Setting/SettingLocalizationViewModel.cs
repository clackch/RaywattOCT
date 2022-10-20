using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Setting;
using System.Collections.Generic;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingLocalizationViewModel : SettingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingLocalizationViewModel));

        [ObservableProperty]
        private Dictionary<string, string> _languageComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private string _currentLanguage;

        private string OriginLanguage;

        public SettingLocalizationViewModel()
        {
            _log.Debug("SettingLocalizationViewModel");

            string l10n_language_list = _l10n.ReadSetting("l10n_language_list"); 
            string[] languageList = l10n_language_list.Split(";");

            for(int i = 0; i < languageList.Length; i++)
            {
                LanguageComboBox[languageList[i]] = _l10n[languageList[i]];
            }

            CurrentLanguage = _l10n.ReadSetting("l10n_current_language");
            OriginLanguage = CurrentLanguage;
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

            ApplyChange();

            //Close Popup
            WeakReferenceMessenger.Default.Send(new PopupMessage(false) { Type = (int)CommonDefinition.PopupType.Setting });
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
                if (CommonDefinition.CurrentPage == (int)CommonDefinition.PageList.PatientListPage)
                {
                    WeakReferenceMessenger.Default.Send(new NavigationMessage("Refresh"));
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml"));
                }
            }
        }
    }
}
