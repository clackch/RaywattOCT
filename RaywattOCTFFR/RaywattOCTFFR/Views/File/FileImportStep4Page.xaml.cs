using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileImportStep4Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportStep4Page : Page
    {
        public FileImportStep4Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportStep4ViewModel));
        }
    }
}
