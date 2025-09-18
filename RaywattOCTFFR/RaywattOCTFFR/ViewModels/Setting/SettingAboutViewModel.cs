using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Services;
using System.Configuration;

namespace RaywattOCTFFR.ViewModels.Setting
{
    public partial class SettingAboutViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingAboutViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private string _softwareName;

        [ObservableProperty]
        private string _softwareVersion;

        public SettingAboutViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingAboutViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            SoftwareName = ConfigurationManager.AppSettings.Get("SoftwareName");
            SoftwareVersion = ConfigurationManager.AppSettings.Get("SoftwareVersion");
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
