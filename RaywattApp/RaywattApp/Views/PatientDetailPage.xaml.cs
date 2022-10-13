using RaywattApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// PatientDetailPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PatientDetailPage : Page
    {
        public PatientDetailPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PatientDetailViewModel));
        }
    }
}
