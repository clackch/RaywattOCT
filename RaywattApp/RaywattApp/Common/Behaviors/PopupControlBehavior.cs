using log4net;
using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RaywattApp.Common.Behaviors
{
    /// <summary>
    /// PopupControlBehavior
    /// </summary>
    public class PopupControlBehavior : Behavior<ContentControl>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PopupControlBehavior));

        public string Control
        {
            get { return (string)GetValue(ControlProperty); }
            set { SetValue(ControlProperty, value); }
        }

        /// <summary>
        /// Control DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ControlProperty = DependencyProperty.Register(nameof(Control), typeof(string), typeof(PopupControlBehavior), new PropertyMetadata(null, ControlChanged));

        private static void ControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _log.Debug("ControlChanged");

            var behavior = (PopupControlBehavior)d;
            behavior.ResolveControl();
        }

        /// <summary>
        /// Control을 이용해서 컨트롤 인스턴스 시켜서 사용
        /// </summary>
        private void ResolveControl()
        {
            _log.Debug("ResolveControl : " + Control);

            if (string.IsNullOrEmpty(Control))
            {
                AssociatedObject.Content = null;
            }
            else
            {
                //GetType을 이용하기 위해서 AssemblyQualifiedName이 필요합니다.
                //예) typeof(AboutControl).AssemblyQualifiedName
                //다른 클래스라이브러리에 있는 컨트롤도 이름만 알면 만들 수 있습니다.
                var type = Type.GetType($"RaywattApp.Views.Controls.{Control}, RaywattApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (type == null)
                {
                    return;
                }
                var control = App.Current.Services.GetService(type);
                AssociatedObject.Content = control;
            }
        }

        public bool LayerPopup
        {
            get { return (bool)GetValue(LayerPopupProperty); }
            set { SetValue(LayerPopupProperty, value); }
        }

        /// <summary>
        /// LayerPopup DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LayerPopupProperty = DependencyProperty.Register(nameof(LayerPopup), typeof(bool), typeof(PopupControlBehavior), new PropertyMetadata(false, LayerPopupChanged));

        private static void LayerPopupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _log.Debug("LayerPopupChanged");

            var behavior = (PopupControlBehavior)d;
            behavior.CheckLayerPopup();
        }
        /// <summary>
        /// LayerPopup 속성 확인 후 false이면 ContentControl에 연결되어 있던 인스턴스 연결 삭제
        /// </summary>
        private void CheckLayerPopup()
        {
            _log.Debug("LayerPopup : " + LayerPopup);

            if (LayerPopup == false)
            {
                AssociatedObject.Content = null;
            }
        }
    }
}
