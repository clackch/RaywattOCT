using RaywattApp.ViewModels.File;
using System.Windows.Controls;

namespace RaywattApp.Views.File
{
    /// <summary>
    /// FileExportStep1Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileExportStep1Page : Page
    {
        public FileExportStep1Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileExportStep1ViewModel));
        }
    }
}
