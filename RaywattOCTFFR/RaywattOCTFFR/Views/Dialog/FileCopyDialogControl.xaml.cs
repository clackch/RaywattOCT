using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.ViewModels.Dialog;
using System.Windows;
using System.Windows.Controls;

namespace RaywattOCTFFR.Views.Dialog
{
    /// <summary>
    /// FileCopyDialogControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FileCopyDialogControl : UserControl
    {
        public FileCopyDialogControl()
        {
            InitializeComponent();
            DataContext = App.Current.Services.GetService(typeof(FileCopyDialogViewModel));

            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as FileCopyDialogViewModel;

            if (vm?.LoadedCommand != null)
            {
                var window = Window.GetWindow(this) as IDialogWindow;

                if (vm.LoadedCommand.CanExecute(window))
                {
                    vm.LoadedCommand.Execute(window);
                }
            }
        }
    }
}
