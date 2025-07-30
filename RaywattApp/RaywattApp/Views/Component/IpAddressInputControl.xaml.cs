using RaywattApp.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;

namespace RaywattApp.Views.Component
{
    /// <summary>
    /// IpAddressInputControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class IpAddressInputControl : UserControl
    {
        public static readonly DependencyProperty IpAddressProperty = DependencyProperty.Register("IpAddress", typeof(IpAddress), typeof(IpAddressInputControl), new PropertyMetadata(null, OnIpAddressChanged));

        public static readonly DependencyProperty SubnetMaskProperty = DependencyProperty.Register("SubnetMask", typeof(SubnetMask), typeof(IpAddressInputControl), new PropertyMetadata(null, OnSubnetMaskChanged));

        public static readonly DependencyProperty ToolTipProperty = DependencyProperty.Register("ToolTip", typeof(string), typeof(IpAddressInputControl), new PropertyMetadata(string.Empty, OnTooltipChanged));

        public IpAddress IpAddress
        {
            get { return (IpAddress)GetValue(IpAddressProperty); }
            set { SetValue(IpAddressProperty, value); }
        }

        public SubnetMask SubnetMask
        {
            get { return (SubnetMask)GetValue(SubnetMaskProperty); }
            set { SetValue(SubnetMaskProperty, value); }
        }

        public string ToolTip
        {
            get { return (string)GetValue(ToolTipProperty); }
            set { SetValue(ToolTipProperty, value); }
        }

        private bool _isInitialized;
        private bool _lockTextChanged;
        private bool _lockPropertyChanged;

        private bool _hasError => !string.IsNullOrEmpty(ToolTip);

        public IpAddressInputControl()
        {
            InitializeComponent();
        }

        private static void OnIpAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IpAddressInputControl)d;
            
            if (e.OldValue is IpAddress oldIpAddress)
            {
                oldIpAddress.PropertyChanged -= control.OnIpAddressPropertyChanged;
            }

            if (e.NewValue is IpAddress newIpAddress)
            {
                newIpAddress.PropertyChanged += control.OnIpAddressPropertyChanged;
                control.SetTextBoxValues();
                control._isInitialized = true;
            }
        }

        private void OnIpAddressPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith("Octet"))
            {
                if (_lockPropertyChanged)
                    return;

                _lockTextChanged = true;
                SetTextBoxValues();
                _lockTextChanged = false;
            }
        }

        private static void OnSubnetMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IpAddressInputControl)d;

            if (e.NewValue != null && e.OldValue == null)
            {
                control.SetTextBoxValues();
                control._isInitialized = true;
            }
        }

        private void SetTextBoxValues()
        {
            if (IpAddress != null)
            {
                Octet1TextBox.Text = IpAddress.Octet1 ?? string.Empty;
                Octet2TextBox.Text = IpAddress.Octet2 ?? string.Empty;
                Octet3TextBox.Text = IpAddress.Octet3 ?? string.Empty;
                Octet4TextBox.Text = IpAddress.Octet4 ?? string.Empty;
            }
            else if (SubnetMask != null)
            {
                Octet1TextBox.Text = SubnetMask.Octet1 ?? string.Empty;
                Octet2TextBox.Text = SubnetMask.Octet2 ?? string.Empty;
                Octet3TextBox.Text = SubnetMask.Octet3 ?? string.Empty;
                Octet4TextBox.Text = SubnetMask.Octet4 ?? string.Empty;
            }
        }

        private static void OnTooltipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IpAddressInputControl)d;
            control.UpdateToolTipVisibility();
        }

        private void UpdateToolTipVisibility()
        {
            if (_hasError)
            {
                Validator.Visibility = Visibility.Visible;
                var errorBrush = TryFindResource("AdditionalColor3Brush") as Brush;
                IpAddressBorder.BorderBrush = errorBrush;
            }
            else
            {
                Validator.Visibility = Visibility.Collapsed;
                UpdateBorderBrush();
            }
        }

        private void UpdateBorderBrush()
        {
            if (_hasError)
            {
                var errorBrush = TryFindResource("AdditionalColor3Brush") as Brush;
                IpAddressBorder.BorderBrush = errorBrush;
            }
            else if (IsAnyIpTextBoxFocused())
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

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized || _lockTextChanged)
                return;

            _lockPropertyChanged = true;
            if (IpAddress != null)
            {
                IpAddress.Octet1 = Octet1TextBox.Text;
                IpAddress.Octet2 = Octet2TextBox.Text;
                IpAddress.Octet3 = Octet3TextBox.Text;
                IpAddress.Octet4 = Octet4TextBox.Text;
            }
            else if (SubnetMask != null)
            {
                SubnetMask.Octet1 = Octet1TextBox.Text;
                SubnetMask.Octet2 = Octet2TextBox.Text;
                SubnetMask.Octet3 = Octet3TextBox.Text;
                SubnetMask.Octet4 = Octet4TextBox.Text;
            }
            _lockPropertyChanged = false;

            UpdateBorderBrush();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateBorderBrush();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsAnyIpTextBoxFocused())
            {
                UpdateBorderBrush();
            }
        }

        private bool IsAnyIpTextBoxFocused()
        {
            return Octet1TextBox.IsFocused || Octet2TextBox.IsFocused || Octet3TextBox.IsFocused || Octet4TextBox.IsFocused;
        }
    }
}
