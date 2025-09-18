using System.Windows;
using System.Windows.Controls.Primitives;

namespace RaywattOCTFFR.Views.Component
{
    public class IconRepeatButton : RepeatButton, SvgComponentBase
    {
        private const string resPath = "/res/icon/";

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(IconRepeatButton), new PropertyMetadata(null, SvgComponentBase.OnIconPropertyChanged));
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        public void InitializeIconPath(string iconName)
        {
            IconDefault = SvgComponentBase.GetPath(resPath, iconName);
            IconOver = SvgComponentBase.GetPath(resPath, iconName, "_hover");
            IconPressed = SvgComponentBase.GetPath(resPath, iconName, "_active");
            IconDisabled = SvgComponentBase.GetPath(resPath, iconName, "_disabled");
        }

        public static readonly DependencyProperty IconDefaultProperty = DependencyProperty.Register(nameof(IconDefault), typeof(string), typeof(IconRepeatButton));
        public static readonly DependencyProperty IconOverProperty = DependencyProperty.Register(nameof(IconOver), typeof(string), typeof(IconRepeatButton));
        public static readonly DependencyProperty IconPressedProperty = DependencyProperty.Register(nameof(IconPressed), typeof(string), typeof(IconRepeatButton));
        public static readonly DependencyProperty IconDisabledProperty = DependencyProperty.Register(nameof(IconDisabled), typeof(string), typeof(IconRepeatButton));

        public string IconDefault { get => (string)GetValue(IconDefaultProperty); set => SetValue(IconDefaultProperty, value); }
        public string IconOver { get => (string)GetValue(IconOverProperty); set => SetValue(IconOverProperty, value); }
        public string IconPressed { get => (string)GetValue(IconPressedProperty); set => SetValue(IconPressedProperty, value); }
        public string IconDisabled { get => (string)GetValue(IconDisabledProperty); set => SetValue(IconDisabledProperty, value); }
    }
}
