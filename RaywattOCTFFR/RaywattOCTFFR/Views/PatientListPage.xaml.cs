using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// PatientListPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientListPage : Page
    {
        public PatientListPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientListViewModel));
        }
    }
}
