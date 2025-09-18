using System.Windows.Controls;
using RaywattOCTFFR.ViewModels;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// PatientNewDicomPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientNewDicomPage : Page
    {
        public PatientNewDicomPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientNewDicomViewModel));
        }
    }
}
