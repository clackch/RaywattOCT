using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattOCTFFR.Common.Bases;
using System.Windows.Input;
using static RaywattOCT.RayCoreWrapper;

namespace RaywattOCTFFR.ViewModels.Setting
{
    public partial class SettingMaintenanceViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingMaintenanceViewModel));

        [ObservableProperty]
        private string _btnName;

        private bool isRJCleanModeOnOff;

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
            {
                RayError result = (RayError)RayRJCleanModeOnOff(false);
                if (result != RayError.OK)
                {
                    _log.Error("RayRJCleanModeOnOff Error");
                }
            }
        }

        private void RJCleanModeOnOff()
        {
            _log.Debug("RJCleanModeOnOff");

            isRJCleanModeOnOff = !isRJCleanModeOnOff;
            DeviceStatus.IsCleaningDone = isRJCleanModeOnOff;

            RayError ret = (RayError) RayRJCleanModeOnOff(isRJCleanModeOnOff);

            if (ret == RayError.OK)
            {
                if (isRJCleanModeOnOff)
                {
                    BtnName = _l10n["Disable Cleaning"];
                }
                else
                {
                    BtnName = _l10n["Enable Cleaning"];
                }
            }
            else {
                isRJCleanModeOnOff = !isRJCleanModeOnOff;
                DeviceStatus.IsCleaningDone = isRJCleanModeOnOff;
            }
        }
    }
}
