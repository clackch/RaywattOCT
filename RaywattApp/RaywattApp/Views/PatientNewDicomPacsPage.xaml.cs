using System.Windows.Controls;
using RaywattApp.ViewModels;

namespace RaywattApp.Views
{
    /// <summary>
    /// PatientNewDicomPacsPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientNewDicomPacsPage : Page
    {
        public PatientNewDicomPacsPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientNewDicomPacsViewModel));
        }
    }
}
