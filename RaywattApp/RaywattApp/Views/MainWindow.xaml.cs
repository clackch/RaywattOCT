using RaywattApp.ViewModels;
using System.Windows;

namespace RaywattApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(MainViewModel));
        }
    }
}
