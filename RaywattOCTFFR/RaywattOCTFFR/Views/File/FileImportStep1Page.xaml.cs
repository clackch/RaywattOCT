using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileImportStep1Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportStep1Page : Page
    {
        public FileImportStep1Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportStep1ViewModel));
        }
    }
}
