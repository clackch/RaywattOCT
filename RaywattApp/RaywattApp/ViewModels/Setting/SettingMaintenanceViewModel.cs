using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using System.Windows.Input;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingMaintenanceViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingMaintenanceViewModel));

        [ObservableProperty]
        private string _btnName;

        private bool isRJCleanModeOnOff = false;

        private ICommand _rJCleanModeOnOffCommand;
        public ICommand RJCleanModeOnOffCommand
        {
            get { return this._rJCleanModeOnOffCommand ?? (this._rJCleanModeOnOffCommand = new RelayCommand(RJCleanModeOnOff)); }
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            BtnName = _l10n["Enable Cleaning"];
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");

            if (isRJCleanModeOnOff)
                RayRJCleanModeOnOff(false);
        }

        private void RJCleanModeOnOff()
        {
            _log.Debug("RJCleanModeOnOff");

            isRJCleanModeOnOff = !isRJCleanModeOnOff;

            if (isRJCleanModeOnOff)
                BtnName = _l10n["Disable Cleaning"]; 
            else
                BtnName = _l10n["Enable Cleaning"];

            RayRJCleanModeOnOff(isRJCleanModeOnOff);
        }
    }
}
