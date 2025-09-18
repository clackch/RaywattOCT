using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// PatientNewDicomMwlPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientNewDicomMwlPage : Page
    {
        public PatientNewDicomMwlPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientNewDicomMwlViewModel));
        }
    }
}
