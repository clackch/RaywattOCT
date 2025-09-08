using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RaywattApp.Common.Behaviors
{
    public static class DatePickerPopupOffsetBehavior
    {
        public static readonly DependencyProperty AutoVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AutoVerticalOffset",
                typeof(bool),
                typeof(DatePickerPopupOffsetBehavior),
                new PropertyMetadata(false, OnAutoVerticalOffsetChanged));

        public static readonly DependencyProperty AutoHorizontalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AutoHorizontalOffsetProperty",
                typeof(bool),
                typeof(DatePickerPopupOffsetBehavior),
                new PropertyMetadata(false, OnAutoHorizontalOffsetChanged));


        public static bool GetAutoVerticalOffset(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoVerticalOffsetProperty);
        }

        public static void SetAutoVerticalOffset(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoVerticalOffsetProperty, value);
        }
        public static bool GetAutoHorizontalOffset(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoHorizontalOffsetProperty);
        }

        public static void SetAutoHorizontalOffset(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoHorizontalOffsetProperty, value);
        }

        private static void OnAutoVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DatePicker datePicker && (bool)e.NewValue)
            {
                datePicker.Loaded += (s, args) =>
                {
                    var popup = FindPopup(datePicker);
                    if (popup != null)
                    {
                        popup.Opened += (ps, pa) =>
                        {
                            if (popup.Child is FrameworkElement child)
                            {
                                void Handler(object? sender, EventArgs e)
                                {
                                    child.LayoutUpdated -= Handler;
                                    try
                                    {
                                        var parentWindow = Window.GetWindow(datePicker);
                                        if (parentWindow == null)
                                        {
                                            popup.VerticalOffset = 6;
                                            return;
                                        }

                                        var pickerTransform = datePicker.TransformToVisual(parentWindow);
                                        var pickerPosition = pickerTransform.Transform(new Point(0, datePicker.ActualHeight));
                                        double spaceBelow = SystemParameters.WorkArea.Height - pickerPosition.Y;
                                        double popupHeight = child.ActualHeight;

                                        popup.VerticalOffset = (spaceBelow < popupHeight) ? -6 : 6;
                                        popup.Placement = (spaceBelow < popupHeight) ? PlacementMode.Top : PlacementMode.Bottom;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        popup.VerticalOffset = 6;
                                        popup.Placement = PlacementMode.Bottom;
                                    }
                                }
                                child.LayoutUpdated += Handler;
                            }
                        };
                    }
                };
            }
        }

        private static void OnAutoHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DatePicker datePicker && (bool)e.NewValue)
            {
                datePicker.Loaded += (s, args) =>
                {
                    var popup = FindPopup(datePicker);
                    if (popup != null)
                    {
                        popup.Opened += (ps, pa) =>
                        {
                            if (popup.Child is FrameworkElement child)
                            {
                                void Handler(object? sender, EventArgs e)
                                {
                                    child.LayoutUpdated -= Handler;
                                    try
                                    {
                                        double datePickerWidth = datePicker.ActualWidth;
                                        double popupWidth = child.ActualWidth;

                                        double horizontalOffset = (datePickerWidth - popupWidth) / 2;

                                        popup.HorizontalOffset = horizontalOffset;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        popup.HorizontalOffset = -1; // default
                                    }
                                }
                                child.LayoutUpdated += Handler;
                            }
                        };
                    }
                };
            }
        }

        private static Popup? FindPopup(DatePicker datePicker)
        {
            var template = datePicker.Template;
            if (template == null) return null;
            return template.FindName("PART_Popup", datePicker) as Popup;
        }
    }
}
