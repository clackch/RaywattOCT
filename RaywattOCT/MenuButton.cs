using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RaywattOCT
{
    class MenuButton : System.Windows.Controls.Button
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(MenuButton));
        public static readonly DependencyProperty IconOvProperty = DependencyProperty.Register("IconOv", typeof(string), typeof(MenuButton));
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register("IconSize", typeof(int), typeof(MenuButton));

        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public string IconOv { get => (string)GetValue(IconOvProperty); set => SetValue(IconOvProperty, value); }
        public int IconSize { get => (int)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    }
}
