using RaywattOCTFFR.ViewModels;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views
{
    /// <summary>
    /// ReviewComparePage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewComparePage : Page
    {
        public ReviewComparePage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewCompareViewModel));
        }
    }
}
