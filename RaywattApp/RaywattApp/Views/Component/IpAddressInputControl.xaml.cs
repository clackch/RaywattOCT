using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RaywattApp.Models;

namespace RaywattApp.Views.Component
{
    /// <summary>
    /// IpAddressInputControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IpAddressInputControl : UserControl
    {
        public static readonly DependencyProperty IpAddressProperty = DependencyProperty.Register("IpAddress", typeof(IpAddress), typeof(IpAddressInputControl));

        public static readonly DependencyProperty ToolTipProperty = DependencyProperty.Register("ToolTip", typeof(string), typeof(IpAddressInputControl), new PropertyMetadata(string.Empty, OnTooltipChanged));

        public IpAddress IpAddress
        {
            get { return (IpAddress)GetValue(IpAddressProperty); }
            set { SetValue(IpAddressProperty, value); }
        }

        public string ToolTip
        {
            get { return (string)GetValue(ToolTipProperty); }
            set { SetValue(ToolTipProperty, value); }
        }

        public IpAddressInputControl()
        {
            InitializeComponent();
        }

        private static void OnTooltipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IpAddressInputControl)d;
            control.UpdateToolTipVisibility();
        }

        private void UpdateToolTipVisibility()
        {
            if (!string.IsNullOrEmpty(ToolTip))
            {
                Validator.Visibility = Visibility.Visible;
                var errorBrush = TryFindResource("AdditionalColor3Brush") as Brush;
                IpAddressBorder.BorderBrush = errorBrush;
            }
            else
            {
                Validator.Visibility = Visibility.Collapsed;
                if (IsAnyIpTextBoxFocused())
                {
                    var focusBrush = TryFindResource("MainPrimaryColorBrush") as Brush;
                    IpAddressBorder.BorderBrush = focusBrush;
                }
                else
                {
                    var normalBrush = TryFindResource("GrayscaleGray5Brush") as Brush;
                    IpAddressBorder.BorderBrush = normalBrush;
                }
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            IpAddress.Octet1 = Octet1TextBox.Text;
            IpAddress.Octet2 = Octet2TextBox.Text;
            IpAddress.Octet3 = Octet3TextBox.Text;
            IpAddress.Octet4 = Octet4TextBox.Text;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var focusBrush = TryFindResource("MainPrimaryColorBrush") as Brush;
            IpAddressBorder.BorderBrush = focusBrush;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsAnyIpTextBoxFocused())
            {
                var normalBrush = TryFindResource("GrayscaleGray5Brush") as Brush;
                IpAddressBorder.BorderBrush = normalBrush;
            }
        }

        private bool IsAnyIpTextBoxFocused()
        {
            return Octet1TextBox.IsFocused || Octet2TextBox.IsFocused || Octet3TextBox.IsFocused || Octet4TextBox.IsFocused;
        }
    }
}
