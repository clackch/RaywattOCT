using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.File;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2ViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2ViewModel));

        [ObservableProperty]
        FileExport _fileExportData;

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                FileExportData = (FileExport)extraData;
                SetCondition(FileExportData);
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Views/File/FileExportStep1Page.xaml") { Parameter = FileExportData });
        }

        //protected override void 

        private void SetCondition(FileExport fileExportData)
        {
            _log.Debug("SetCondition");

            //Step1 -> Step2 -> Step1 -> Step2 인 경우, 기존 Step2 일때 선택한 내용 화면에 표시
        }
    }
}
