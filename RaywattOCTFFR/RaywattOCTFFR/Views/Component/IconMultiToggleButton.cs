using System.Windows;
using System.Windows.Controls.Primitives;

namespace RaywattOCTFFR.Views.Component
{
    public class IconMultiToggleButton : ToggleButton, SvgComponentBase
    {
        private const string resPath = "/res/icon/";

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(IconMultiToggleButton), new PropertyMetadata(null, SvgComponentBase.OnIconPropertyChanged));
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public static readonly DependencyProperty CheckedIconProperty = DependencyProperty.Register("CheckedIcon", typeof(string), typeof(IconMultiToggleButton), new PropertyMetadata(null, OnCheckedIconPropertyChanged));
        public string CheckedIcon { get => (string)GetValue(CheckedIconProperty); set => SetValue(CheckedIconProperty, value); }


        public void InitializeIconPath(string iconName)
        {
            IconDefault = SvgComponentBase.GetPath(resPath, iconName);
            IconOver = SvgComponentBase.GetPath(resPath, iconName, "_hover");
            IconPressed = SvgComponentBase.GetPath(resPath, iconName, "_active");
            IconDisabled = SvgComponentBase.GetPath(resPath, iconName, "_disabled");
        }

        public void InitializeCheckedIconPath(string iconName)
        {
            CheckedIconDefault = SvgComponentBase.GetPath(resPath, iconName);
            CheckedIconOver = SvgComponentBase.GetPath(resPath, iconName, "_hover");
            CheckedIconPressed = SvgComponentBase.GetPath(resPath, iconName, "_active");
            CheckedIconDisabled = SvgComponentBase.GetPath(resPath, iconName, "_disabled");
        }

        protected static void OnCheckedIconPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var iconComponent = obj as IconMultiToggleButton;
            if (iconComponent == null) return;

            string value = e.NewValue as string;
            if (string.IsNullOrEmpty(value)) return;

            iconComponent.InitializeCheckedIconPath(value);
        }

        public static readonly DependencyProperty IconDefaultProperty = DependencyProperty.Register(nameof(IconDefault), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty IconOverProperty = DependencyProperty.Register(nameof(IconOver), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty IconPressedProperty = DependencyProperty.Register(nameof(IconPressed), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty IconDisabledProperty = DependencyProperty.Register(nameof(IconDisabled), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty CheckedIconDefaultProperty = DependencyProperty.Register(nameof(CheckedIconDefault), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty CheckedIconOverProperty = DependencyProperty.Register(nameof(CheckedIconOver), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty CheckedIconPressedProperty = DependencyProperty.Register(nameof(CheckedIconPressed), typeof(string), typeof(IconMultiToggleButton));
        public static readonly DependencyProperty CheckedIconDisabledProperty = DependencyProperty.Register(nameof(CheckedIconDisabled), typeof(string), typeof(IconMultiToggleButton));

        public string IconDefault { get => (string)GetValue(IconDefaultProperty); set => SetValue(IconDefaultProperty, value); }
        public string IconOver { get => (string)GetValue(IconOverProperty); set => SetValue(IconOverProperty, value); }
        public string IconPressed { get => (string)GetValue(IconPressedProperty); set => SetValue(IconPressedProperty, value); }
        public string IconDisabled { get => (string)GetValue(IconDisabledProperty); set => SetValue(IconDisabledProperty, value); }

        public string CheckedIconDefault { get => (string)GetValue(CheckedIconDefaultProperty); set => SetValue(CheckedIconDefaultProperty, value); }
        public string CheckedIconOver { get => (string)GetValue(CheckedIconOverProperty); set => SetValue(CheckedIconOverProperty, value); }
        public string CheckedIconPressed { get => (string)GetValue(CheckedIconPressedProperty); set => SetValue(CheckedIconPressedProperty, value); }
        public string CheckedIconDisabled { get => (string)GetValue(CheckedIconDisabledProperty); set => SetValue(CheckedIconDisabledProperty, value); }
    }
}
