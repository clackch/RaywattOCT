using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// PhysicianEditPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PhysicianEditPage : Page
    {
        public PhysicianEditPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PhysicianEditViewModel));
        }
    }
}
