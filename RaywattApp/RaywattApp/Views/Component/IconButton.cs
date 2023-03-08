using RaywattApp.Common.Annotation.Models;
using SharpVectors.Converters;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Views.Component
{
    class IconButton : Button
    {
        private const string IconPath = "/res/icon/";
        private const string IconExtenstion = ".svg";

        private static string GetPath(string value, string postfix="")
        {
            return IconPath + value + postfix + IconExtenstion;
        }

        public static DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(string), typeof(IconButton), new PropertyMetadata(null, OnIconPropertyChanged));
        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        private static void OnIconPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var iconButton = obj as IconButton;
            if (iconButton == null) return;

            string value = e.NewValue as string;
            if (string.IsNullOrEmpty(value)) return;

            iconButton.IconDefault = GetPath(value);
            iconButton.IconOver = GetPath(value, "_hover");
            iconButton.IconPressed = GetPath(value, "_active");
            iconButton.IconDisabled = GetPath(value, "_disabled");
        }

        public static readonly DependencyProperty IconDefaultProperty = DependencyProperty.Register(nameof(IconDefault), typeof(string), typeof(IconButton));
        public static readonly DependencyProperty IconOverProperty = DependencyProperty.Register(nameof(IconOver), typeof(string), typeof(IconButton));
        public static readonly DependencyProperty IconPressedProperty = DependencyProperty.Register(nameof(IconPressed), typeof(string), typeof(IconButton));
        public static readonly DependencyProperty IconDisabledProperty = DependencyProperty.Register(nameof(IconDisabled), typeof(string), typeof(IconButton));

        public string IconDefault { get => (string)GetValue(IconDefaultProperty); set => SetValue(IconDefaultProperty, value); }
        public string IconOver { get => (string)GetValue(IconOverProperty); set => SetValue(IconOverProperty, value); }
        public string IconPressed { get => (string)GetValue(IconPressedProperty); set => SetValue(IconPressedProperty, value); }
        public string IconDisabled { get => (string)GetValue(IconDisabledProperty); set => SetValue(IconDisabledProperty, value); }
    }
}
