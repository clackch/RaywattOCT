using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileImportStep5Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportStep5Page : Page
    {
        public FileImportStep5Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportStep5ViewModel));
        }
    }
}
