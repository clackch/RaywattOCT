using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RaywattApp.Common.Behaviors
{
    public class IgnoreMouseDownBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty IgnoreElementProperty =
            DependencyProperty.Register(nameof(IgnoreElement), typeof(UIElement), typeof(IgnoreMouseDownBehavior), new PropertyMetadata(null));

        // 무시할 요소를 바인딩할 수 있도록 설정
        public UIElement IgnoreElement
        {
            get => (UIElement)GetValue(IgnoreElementProperty);
            set => SetValue(IgnoreElementProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDown += OnMouseDown; // 버블링 단계에서 마우스 이벤트 처리
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDown -= OnMouseDown; // 이벤트 핸들러 제거
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreElement != null && e.OriginalSource is DependencyObject source)
            {
                // IgnoreElement 또는 그 자식 요소에서 발생한 경우에만 이벤트 전파를 차단
                if (IsElementAncestorOf(source, IgnoreElement))
                {
                    e.Handled = true; // 이벤트 전파 차단
                }
            }
        }

        private bool IsElementAncestorOf(DependencyObject child, UIElement parent)
        {
            // 부모-자식 관계인지 확인하는 유틸리티 메서드
            var current = child;
            while (current != null)
            {
                if (current == parent)
                {
                    return true;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }
}
