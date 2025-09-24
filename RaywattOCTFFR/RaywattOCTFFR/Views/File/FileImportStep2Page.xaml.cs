using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileImportStep2Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportStep2Page : Page
    {
        public FileImportStep2Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportStep2ViewModel));
        }
    }
}
