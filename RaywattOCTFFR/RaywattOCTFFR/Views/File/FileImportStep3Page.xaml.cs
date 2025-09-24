using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileImportStep3Page.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileImportStep3Page : Page
    {
        public FileImportStep3Page()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileImportStep3ViewModel));
        }
    }
}
