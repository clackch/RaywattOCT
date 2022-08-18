using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RaywattOCT
{
    class PlayButton : System.Windows.Controls.Button
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(PlayButton));
        public static readonly DependencyProperty IconOvProperty = DependencyProperty.Register("IconOv", typeof(string), typeof(PlayButton));

        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public string IconOv { get => (string)GetValue(IconOvProperty); set => SetValue(IconOvProperty, value); }
    }

    class PlayButtonRepeat : System.Windows.Controls.Primitives.RepeatButton
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(PlayButtonRepeat));
        public static readonly DependencyProperty IconOvProperty = DependencyProperty.Register("IconOv", typeof(string), typeof(PlayButtonRepeat));

        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public string IconOv { get => (string)GetValue(IconOvProperty); set => SetValue(IconOvProperty, value); }
    }
}