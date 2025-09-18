using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileExportStep2StandardPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileExportStep2StandardPage : Page
    {
        public FileExportStep2StandardPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileExportStep2StandardViewModel));
        }
    }
}
