using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.ViewModels.Setting
{
    public partial class SettingDatabaseViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingDatabaseViewModel));

        [ObservableProperty]
        private double _storageProgress;

        [ObservableProperty]
        public double _storageTotalSize;

        [ObservableProperty]
        public double _storageFreeSize;

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            double storageTotalSize, storageFreeSize;
            CommonUtil.GetStorageSize(out storageTotalSize, out storageFreeSize);
            StorageTotalSize = storageTotalSize;
            StorageFreeSize = storageFreeSize;

            StorageProgress = (StorageTotalSize - StorageFreeSize) / StorageTotalSize * 100;
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }
    }
}
