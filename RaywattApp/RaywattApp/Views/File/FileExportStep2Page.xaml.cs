using RaywattApp.ViewModels.File;
using System.Windows.Controls;

namespace RaywattApp.Views.File
{
    /// <summary>
    /// FileExportStep2Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileExportStep2Page : Page
    {
        public FileExportStep2Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileExportStep2ViewModel));
        }
    }
}
