using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileExportStep2DicomPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileExportStep2DicomPage : Page
    {
        public FileExportStep2DicomPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileExportStep2DicomViewModel));
        }
    }
}
