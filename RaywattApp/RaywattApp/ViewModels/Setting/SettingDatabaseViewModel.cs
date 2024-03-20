using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System.IO;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingDatabaseViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingDatabaseViewModel));

        [ObservableProperty]
        private double _dbProgress;

        [ObservableProperty]
        public double _dbTotalSize;

        [ObservableProperty]
        public double _dbAvailableFreeSpace;

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            GetDbSize();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void GetDbSize()
        {
            string configDrive = Constants.SystemRootPath + "\\";

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDrives)
            {
                if (drive.Name.Equals(configDrive))
                {
                    DbTotalSize = CommonUtil.ByteToGB(drive.TotalSize);
                    DbAvailableFreeSpace = CommonUtil.ByteToGB(drive.AvailableFreeSpace);
                    break;
                }
            }

            DbProgress = (DbTotalSize - DbAvailableFreeSpace) / DbTotalSize * 100;
        }
    }
}
