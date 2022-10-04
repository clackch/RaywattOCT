using Microsoft.Xaml.Behaviors;
using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Navigation;
using RaywattApp.Common.Bases;
using log4net;

namespace RaywattApp.Common.Behaviors
{
    /// <summary>
    /// FrameBehavior
    /// </summary>
    public class FrameBehavior : Behavior<Frame>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FrameBehavior));
        /// <summary>
        /// Navigation DependencyProperty 변경 때문에 발생하는 프로퍼티 체인지 이벤트를 막기 위해 사용
        /// </summary>
        private bool _isWork;

        protected override void OnAttached()
        {
            _log.Debug("OnAttached");

            //Navigation 시작
            AssociatedObject.Navigating += AssociatedObject_Navigating;
            //Navigation 종료
            AssociatedObject.Navigated += AssociatedObject_Navigated;
        }

        /// <summary>
        /// Navigation 시작 이벤트 핸들러
        /// </summary>
        private void AssociatedObject_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            _log.Debug("AssociatedObject_Navigating");

            //네비게이션 시작전 상황을 뷰모델에 알려주기
            if (AssociatedObject.Content is Page pageContent && pageContent.DataContext is INavigationAware navigationAware)
            {
                navigationAware?.OnNavigating(sender, e);
            }
        }

        /// <summary>
        /// Navigation 종료 이벤트 핸들러
        /// </summary>
        private void AssociatedObject_Navigated(object sender, NavigationEventArgs e)
        {
            _log.Debug("AssociatedObject_Navigated");

            _isWork = true;
            //네비게이션이 완료된 Uri를 Navigation에 입력
            Navigation = e.Uri.ToString();
            _isWork = false;
            //네비게이션이 완료된 상황을 뷰모델에 알려주기
            if (AssociatedObject.Content is Page pageContent && pageContent.DataContext is INavigationAware navigationAware)
            {
                navigationAware.OnNavigated(sender, e);
            }
        }

        protected override void OnDetaching()
        {
            _log.Debug("OnDetaching");

            AssociatedObject.Navigating -= AssociatedObject_Navigating;
            AssociatedObject.Navigated -= AssociatedObject_Navigated;
        }

        public string Navigation
        {
            get { return (string)GetValue(NavigationProperty); }
            set { SetValue(NavigationProperty, value); }
        }

        /// <summary>
        /// Navigation DependencyProperty
        /// </summary>
        /// ※ 변수명은 "등록하는 Property 명칭" + "Property"로 해야함 (ex. Navigation + Property)
        public static readonly DependencyProperty NavigationProperty = DependencyProperty.Register(nameof(Navigation), typeof(string), typeof(FrameBehavior), new PropertyMetadata(null, NavigationChanged));

        /// <summary>
        /// Navigation PropertyChanged
        /// </summary>
        private static void NavigationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _log.Debug("NavigationChanged");

            var behavior = (FrameBehavior)d;
            if (behavior._isWork)
            {
                return;
            }
            behavior.Navigate(behavior.Parameter);
        }

        /// <summary>
        /// 화면 전환
        /// </summary>
        private void Navigate(object parameter)
        {
            _log.Debug("Navigate : " + Navigation);

            switch (Navigation)
            {
                case "GoBack":
                    //GoBack으로 오면 뒤로가기
                    if (AssociatedObject.CanGoBack)
                    {
                        AssociatedObject.GoBack();
                    }
                    break;
                case "Refresh":
                    AssociatedObject.Refresh();
                    break;
                case null:
                case "":
                    //Nothing
                    return;
                default:
                    //navigate
                    AssociatedObject.Navigate(new Uri(Navigation, UriKind.RelativeOrAbsolute), parameter);
                    break;
            }
        }

        public object Parameter
        {
            get { return GetValue(ParameterProperty); }
            set { SetValue(ParameterProperty, value); }
        }

        /// <summary>
        /// Navigation ParameterProperty
        /// </summary>
        /// ※ 변수명은 "등록하는 Property 명칭" + "Property"로 해야함 (ex. Parameter + Property)
        public static readonly DependencyProperty ParameterProperty = DependencyProperty.Register(nameof(Parameter), typeof(object), typeof(FrameBehavior), new PropertyMetadata(null, null));
    }
}
