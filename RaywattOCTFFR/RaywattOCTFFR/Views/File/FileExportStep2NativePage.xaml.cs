using RaywattOCTFFR.ViewModels.File;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.File
{
    /// <summary>
    /// FileExportStep2NativePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileExportStep2NativePage : Page
    {
        public FileExportStep2NativePage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileExportStep2NativeViewModel));
        }
    }
}
