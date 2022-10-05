using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// PatientNewPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientNewPage : Page
    {
        public PatientNewPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientNewViewModel));
        }
    }
}
