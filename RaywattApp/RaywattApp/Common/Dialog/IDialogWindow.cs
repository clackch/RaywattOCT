using System.Windows;

namespace RaywattApp.Common.Dialog
{
    public interface IDialogWindow
    {
        bool? DialogResult { get; set; }

        object DataContext { get; set; }

        object Content { get; set; }

        double Left { get; set; }

        double Top { get; set; }

        Window Owner { get; set; }

        bool? ShowDialog();

        void Show();

        void Hide();

        void Close();
    }
}
