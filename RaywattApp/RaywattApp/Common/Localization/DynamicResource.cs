using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows.Markup;
using System.Windows;
using RaywattApp.Common.Localization.Resources;
using System.Configuration;
using log4net;

namespace RaywattApp.Common.Localization
{
    /// <summary>
    /// 다이나믹 리소스 - 모든 택스트 리소스는 여기를 통해서 출력됨
    /// </summary>
    public class DynamicResource : DynamicObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DynamicResource));

        /// <summary>
        /// 윈도우 리소스로더
        /// </summary>
        private readonly ResourceManager _resourceManager;
        private CultureInfo _clutureInfo;

        /// <summary>
        /// 생성자
        /// </summary>
        public DynamicResource()
        {
            _log.Debug("DynamicResource");

            _resourceManager = new ResourceManager(typeof(Resource));

            //App.config에서 l10n_current_language 가져오기
            string languageCode = ReadSetting("l10n_current_language");

            //l10n_current_language 없을 경우, Default로 en-US 사용
            if (languageCode == null)
                languageCode = "en-US";

            SetLanguage(languageCode);
        }

        #region 기본 기능

        /// <summary>
        /// 프로퍼티로 호출
        /// </summary>
        public string this[string id]
        {
            get
            {
                //1. 리소스에서 값 조회
                if (string.IsNullOrEmpty(id)) return null;
                string str = _resourceManager.GetString(id, _clutureInfo);
                if (string.IsNullOrEmpty(str))
                //2. 없으면 키 반환
                {
                    str = id;
                }
                return str;
            }
        }

        /// <summary>
        /// 이름으로 호출
        /// </summary>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string id = binder.Name;
            string str = _resourceManager.GetString(id, _clutureInfo);
            if (string.IsNullOrEmpty(str))
            {
                str = id;
            }
            result = str;
            return true;
        }

        #endregion
        /// <summary>
        /// 클래스 네임
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// 리소스 딕셔너리
        /// </summary>
        private IDictionary<KeyValuePair<string, string>, string> ResourceDictionaryUsedByClass { get; } = new Dictionary<KeyValuePair<string, string>, string>();

        /// <summary>
        /// 클래스에서 사용하는 리소스 사전에 추가
        /// </summary>
        private void AddResourceDictionary(string className, string resourceKey)
        {
            KeyValuePair<string, string> key = new KeyValuePair<string, string>(className ?? "public", resourceKey);
            if (ResourceDictionaryUsedByClass.ContainsKey(key))
            {
                return;
            }

            ResourceDictionaryUsedByClass.Add(new KeyValuePair<KeyValuePair<string, string>, string>(key, resourceKey));
        }

        /// <summary>
        /// 클래스에서 사용하는 리소스 사전 조회
        /// </summary>
        public IList<string> GetResourceDictionary(string className)
        {
            List<string> returnValues = (from item in ResourceDictionaryUsedByClass
                                         where item.Key.Key == className
                                         select item.Value).ToList();
            return returnValues;
        }

        /// <summary>
        /// 런타임 언어 변경
        /// </summary>
        public void ChangeLanguage(string languageCode)
        {
            if (languageCode.Equals(Thread.CurrentThread.CurrentCulture.ToString()))
                return;

            _log.Debug("ChangeLanguage : " + Thread.CurrentThread.CurrentCulture.ToString() + " -> " + languageCode);

            AddUpdateAppSettings("l10n_current_language", languageCode);          

            SetLanguage(languageCode);
        }

        private void SetLanguage(string languageCode)
        {
            _log.Debug("SetLanguage : " + languageCode);

            //언어 설정
            _clutureInfo = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = _clutureInfo;
            Thread.CurrentThread.CurrentUICulture = _clutureInfo;

            //윈도우의 언어코드 변경
            foreach (Window window in Application.Current.Windows.Cast<Window>())
            {
                if (!window.AllowsTransparency)
                {
                    window.Language = XmlLanguage.GetLanguage(_clutureInfo.Name);
                }
            }
        }

        public string ReadSetting(string key)
        {
            _log.Debug("ReadSetting : " + key);

            string value = null;

            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                value = appSettings[key];
            }
            catch (ConfigurationErrorsException e)
            {
                _log.Error("Error reading app settings : " + e.ToString());
            }

            return value;
        }

        public void AddUpdateAppSettings(string key, string value)
        {
            _log.Debug("AddUpdateAppSettings : " + key + "/" + value);

            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException e)
            {
                _log.Error("Error writing app settings : " + e.ToString());
            }
        }
    }
}
