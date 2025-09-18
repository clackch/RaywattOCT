using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// PhysicianListPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PhysicianListPage : Page
    {
        public PhysicianListPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(PhysicianListViewModel));
        }
    }
}
