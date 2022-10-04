using RaywattApp.ViewModels.File;
using System.Windows.Controls;

namespace RaywattApp.Views.File
{
    /// <summary>
    /// FileImportPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportPage : Page
    {
        public FileImportPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportViewModel));
        }
    }
}
