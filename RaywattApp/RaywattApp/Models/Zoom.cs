using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using System.Windows;
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
        private bool _isCaptured = false;

        private double ratio;

        private double minimapSize;

        private double zoomScaleDefault;

        private double zoomScaleMax;

        public Zoom(double imageSize = Constants.CrossSectionSize, double minimapSize = Constants.MiniMapCanvasSize)
        {
            this.minimapSize = minimapSize;
            this.ratio = imageSize / this.minimapSize;
            this.zoomScaleDefault = imageSize / Constants.OCTImageSize;
            this.zoomScaleMax = this.zoomScaleDefault * 2;

            ScaleX = this.zoomScaleDefault;
            ScaleY = this.zoomScaleDefault;
            RectLeft = 0;
            RectTop = 0;
            RectWidth = this.minimapSize;
            RectHeight = this.minimapSize;
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

        private ICommand _manipulationStartingCommand;
        public ICommand ManipulationStartingCommand
        {
            get { return this._manipulationStartingCommand ?? (this._manipulationStartingCommand = new RelayCommand<ManipulationStartingEventArgs>(Window_ManipulationStarting)); }
        }

        private ICommand _manipulationDeltaCommand;
        public ICommand ManipulationDeltaCommand
        {
            get { return this._manipulationDeltaCommand ?? (this._manipulationDeltaCommand = new RelayCommand<ManipulationDeltaEventArgs>(Window_ManipulationDelta)); }
        }

        private ICommand _manipulationCompletedCommand;
        public ICommand ManipulationCompletedCommand
        {
            get { return this._manipulationCompletedCommand ?? (this._manipulationCompletedCommand = new RelayCommand<ManipulationCompletedEventArgs>(Window_ManipulationCompleted)); }
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
                else if(nextX + RectWidth > this.minimapSize)
                    RectLeft = this.minimapSize - RectWidth;
                else
                    RectLeft = nextX;

                if (nextY < 0)
                    RectTop = 0;
                else if (nextY + RectHeight > this.minimapSize)
                    RectTop = this.minimapSize - RectHeight;
                else
                    RectTop = nextY;

                TranslateX = -RectLeft * this.ratio * ScaleX;
                TranslateY = -RectTop * this.ratio * ScaleY;
            }
        }

        public bool ZoomIn()
        {
            _log.Debug("ZoomIn");

            if (ScaleX >= this.zoomScaleMax)
                return false;

            double nextScale = ScaleX * Constants.AnnotationScale;

            if (nextScale <= this.zoomScaleMax)
            {
                ScaleX = nextScale;
                ScaleY = nextScale;
                ZoomInSetting(Constants.AnnotationScale);
            }

            else
            {
                double annotationScale = this.zoomScaleMax / ScaleX;
                ScaleX = this.zoomScaleMax;
                ScaleY = this.zoomScaleMax;
                ZoomInSetting(annotationScale);
            }

            return true;
        }

        public bool ZoomOut()
        {
            _log.Debug("ZoomOut");

            if (ScaleX <= this.zoomScaleDefault)
                return false;

            double nextScale = ScaleX / Constants.AnnotationScale;

            if (nextScale >= this.zoomScaleDefault)
            {
                ScaleX = nextScale;
                ScaleY = nextScale;
                ZoomOutSetting(Constants.AnnotationScale);
            }

            else
            {
                double annotationScale = this.zoomScaleDefault / ScaleX;
                ScaleX = this.zoomScaleDefault;
                ScaleY = this.zoomScaleDefault;
                ZoomOutSetting(annotationScale);
            }

            return true;
        }

        public void Window_ManipulationStarting(ManipulationStartingEventArgs e)
        {
            _log.Debug("Manipulation Starting");
            e.Handled = true;
        }

        public void Window_ManipulationDelta(ManipulationDeltaEventArgs e)
        {
            double deltaScale = e.DeltaManipulation.Scale.X;           

            if(deltaScale != 1)
            {
                double newScale = ScaleX * deltaScale;

                if (newScale <= this.zoomScaleMax && newScale >= this.zoomScaleDefault)
                {
                    ScaleX = newScale;
                    ScaleY = newScale;

                    if (deltaScale > 1) //확대
                        ZoomInSetting(deltaScale);

                    else if (deltaScale < 1) //축소
                        ZoomOutSetting(1 / deltaScale);
                }
                else if (newScale < this.zoomScaleDefault)
                {                   
                    ScaleX = this.zoomScaleDefault;
                    ScaleY = this.zoomScaleDefault;
                    deltaScale = this.zoomScaleDefault / ScaleX;
                    ZoomOutSetting(deltaScale);
                }
            }

            e.Handled = true;
        }

        public void Window_ManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            _log.Debug("Manipulation Completed");
            e.Handled = true;
        }

        private void ZoomInSetting(double scale)
        {
            //_log.Debug("zoomin");
            double centerLeft = RectLeft + RectWidth / 2;
            double centerTop = RectTop + RectHeight / 2;

            RectWidth /= scale;
            RectHeight /= scale;
            RectLeft = centerLeft - RectWidth / 2;
            RectTop = centerTop - RectHeight / 2; 

            TranslateX = -RectLeft * this.ratio * ScaleX; 
            TranslateY = - RectTop * this.ratio * ScaleY;

            Visibility = Visibility.Visible;
        }

        private void ZoomOutSetting(double scale)
        {
            //_log.Debug("zoomout");
            double centerLeft = RectLeft + RectWidth / 2;
            double centerTop = RectTop + RectHeight / 2;

            RectWidth = RectWidth * scale;
            RectHeight = RectHeight * scale;

            if (centerLeft + RectWidth / 2 > this.minimapSize)
                centerLeft = this.minimapSize - RectWidth / 2;

            if (centerTop + RectHeight / 2 > this.minimapSize)
                centerTop = this.minimapSize - RectHeight / 2;

            RectLeft = centerLeft - RectWidth / 2 < 0 ? 0 : centerLeft - RectWidth / 2;
            RectTop = centerTop - RectHeight / 2 < 0 ? 0 : centerTop - RectHeight / 2;

            TranslateX = -RectLeft * this.ratio * ScaleX;
            TranslateY = -RectTop * this.ratio * ScaleY;

            if (ScaleX <= this.zoomScaleDefault)
            {
                Visibility = Visibility.Collapsed;
                TranslateX = 0; TranslateY = 0;
                RectTop = 0; RectLeft = 0;
                RectWidth = this.minimapSize;
                RectHeight = this.minimapSize;
            }
        }
    }
}
