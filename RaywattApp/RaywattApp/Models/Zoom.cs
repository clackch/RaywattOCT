using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RaywattApp.Models
{
    public partial class Zoom : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Zoom));

        [ObservableProperty]
        private Visibility _visibility;

        [ObservableProperty]
        private double _mouseX;

        [ObservableProperty]
        private double _mouseY;

        [ObservableProperty]
        private double _scaleX;

        [ObservableProperty]
        private double _scaleY;

        [ObservableProperty]
        private double _translateX;

        [ObservableProperty]
        private double _translateY;

        [ObservableProperty]
        private double _rectLeft;

        [ObservableProperty]
        private double _rectTop;

        [ObservableProperty]
        private double _rectWidth;

        [ObservableProperty]
        private double _rectHeight;

        [ObservableProperty]
        private double _offsetX;

        [ObservableProperty]
        private double _offsetY;

        [ObservableProperty]
        public bool _isCaptured = false;

        public Zoom()
        {
            ScaleX = 1;
            ScaleY = 1;
            RectLeft = 0;
            RectTop = 0;
            RectWidth = Constants.MiniMapCanvasSize;
            RectHeight = Constants.MiniMapCanvasSize;
            Visibility = Visibility.Collapsed;
        }

        private ICommand _cmdSetCaptured;
        public ICommand CmdSetCaptured
        {
            get { return _cmdSetCaptured ?? (this._cmdSetCaptured = new RelayCommand<object>(SetCaptured)); }
        }

        private ICommand _cmdMoveRect;
        public ICommand CmdMoveRect
        {
            get { return this._cmdMoveRect ?? (this._cmdMoveRect = new RelayCommand<object>(MoveRect)); }
        }

        private void SetCaptured(object param)
        {
            if(param != null)
            {
                Zoom zoom = (Zoom)param;
                OffsetX = zoom.MouseX - RectLeft;
                OffsetY = zoom.MouseY - RectTop;

                IsCaptured = true;
            }
            else
            {
                IsCaptured = false;
            }
        }

        private void MoveRect(object param)
        {
            if (IsCaptured)
            {
                Zoom zoom = (Zoom)param;

                double nextX = zoom.MouseX - OffsetX;
                double nextY = zoom.MouseY - OffsetY;

                if (nextX < 0)
                    RectLeft = 0;
                else if(nextX + RectWidth > Constants.MiniMapCanvasSize)
                    RectLeft = Constants.MiniMapCanvasSize - RectWidth;
                else
                    RectLeft = nextX;

                if (nextY < 0)
                    RectTop = 0;
                else if (nextY + RectHeight > Constants.MiniMapCanvasSize)
                    RectTop = Constants.MiniMapCanvasSize - RectHeight;
                else
                    RectTop = nextY;

                double ratio = Constants.CrossSectionSize / Constants.MiniMapCanvasSize;
                TranslateX = -RectLeft * ratio * ScaleX;
                TranslateY = -RectTop * ratio * ScaleY;
            }
        }

        public bool ZoomIn()
        {
            _log.Debug("ZoomIn");

            if (ScaleX >= Constants.ZoomScaleMax)
                return false;

            double ratio = Constants.CrossSectionSize / Constants.MiniMapCanvasSize;
            double centerLeft = RectLeft + RectWidth / 2;
            double centerTop = RectTop + RectHeight / 2;

            RectWidth = RectWidth / Constants.AnnotationScale;
            RectHeight = RectHeight / Constants.AnnotationScale;
            RectLeft = centerLeft - RectWidth / 2;
            RectTop = centerTop - RectHeight / 2;

            ScaleX *= Constants.AnnotationScale;
            ScaleY *= Constants.AnnotationScale;
            TranslateX = -RectLeft * ratio * ScaleX;
            TranslateY = -RectTop * ratio * ScaleY;

            Visibility = Visibility.Visible;

            return true;
        }

        public bool ZoomOut()
        {
            _log.Debug("ZoomOut");

            if (ScaleX == Constants.ZoomScaleDefault)
                return false;

            double ratio = Constants.CrossSectionSize / Constants.MiniMapCanvasSize;
            double centerLeft = RectLeft + RectWidth / 2;
            double centerTop = RectTop + RectHeight / 2;

            RectWidth = RectWidth * Constants.AnnotationScale;
            RectHeight = RectHeight * Constants.AnnotationScale;

            if (centerLeft + RectWidth / 2 > Constants.MiniMapCanvasSize)
                centerLeft = Constants.MiniMapCanvasSize - RectWidth / 2;

            if(centerTop + RectHeight / 2 > Constants.MiniMapCanvasSize)
                centerTop = Constants.MiniMapCanvasSize - RectHeight / 2;

            RectLeft = centerLeft - RectWidth / 2 < 0 ? 0 : centerLeft - RectWidth / 2;
            RectTop = centerTop - RectHeight / 2 < 0 ? 0 : centerTop - RectHeight / 2;

            ScaleX /= Constants.AnnotationScale;
            ScaleY /= Constants.AnnotationScale;
            TranslateX = -RectLeft * ratio * ScaleX;
            TranslateY = -RectTop * ratio * ScaleY;

            if (ScaleX == Constants.ZoomScaleDefault)
                Visibility = Visibility.Collapsed;

            return true;
        }
    }
}
