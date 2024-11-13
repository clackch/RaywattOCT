using RaywattApp.Common.Annotation;
using RaywattApp.ViewModels;
using System.Windows.Controls;

namespace RaywattApp.Views
{
    /// <summary>
    /// ReviewPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ReviewPage : Page
    {
        public ReviewPage()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(ReviewViewModel));
        }

        private void longitude_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.Source is DrawLmodeUtil)
            {
                // annotation:DrawLmodeUtil에서 발생한 이벤트이므로 부모 Grid의 이벤트 처리를 무시합니다.
                e.Handled = true;
            }
        }
    }
}
