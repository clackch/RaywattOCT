using log4net;
using RaywattApp.Common.File;
using System.Windows.Navigation;
using RaywattApp.Models;

namespace RaywattApp.ViewModels.File
{
    public class FileImportViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportViewModel));

        public FileImportViewModel()
        {

        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }
    }
}
