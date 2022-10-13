using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.File;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep1ViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep1ViewModel));

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

                FileExportData.Type = "Test";
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Next()
        {
            _log.Debug("Next");

            WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Views/File/FileExportStep2Page.xaml") { Parameter = FileExportData });
        }

        private void SetCondition(FileExport fileExportData)
        {
            _log.Debug("SetCondition");

            foreach(var item in _fileExportData.SelectedItem)
            {
                _log.Debug(item.ToString());
            }
            //Step1 -> Step2 -> Step1 인 경우, 기존 Step1 일때 선택한 내용 화면에 표시
        }
    }
}
