using log4net;
using RaywattApp.Common.File;

namespace RaywattApp.ViewModels.File
{
    public class FileImportViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportViewModel));

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
