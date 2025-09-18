using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// ReviewAngioCoRegPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewAngioCoRegPage : Page
    {
        public ReviewAngioCoRegPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewAngioCoRegViewModel));
        }
    }
}
