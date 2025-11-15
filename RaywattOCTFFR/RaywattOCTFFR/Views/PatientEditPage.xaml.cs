using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// PatientEditPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientEditPage : Page
    {
        public PatientEditPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientEditViewModel));
        }
    }
}
