using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattOCTFFR.Common.Bases;
using System.Windows;
using System.Windows.Input;
using System;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.Models
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

        private double _scaleX;
        public double ScaleX
        {
            get { return _scaleX; }
            set 
            { 
                _scaleX = value;
                OnPropertyChanged(nameof(ScaleX));
                ZoomScaleX = value / this.foV; 
            }
        }

        private double _scaleY;
        public double ScaleY
        {
            get { return _scaleY; }
            set 
            { 
                _scaleY = value;
                OnPropertyChanged(nameof(ScaleY));
                ZoomScaleY = value / this.foV; 
            }
        }

        private double _translateX;
        public double TranslateX
        {
            get { return _translateX; }
            set 
            { 
                _translateX = value;
                OnPropertyChanged(nameof(TranslateX));
                ZoomTranslateX = -RectLeft * this.ratio * ZoomScaleX;  
            }
        }

        private double _translateY;
        public double TranslateY
        {
            get { return _translateY; }
            set 
            { 
                _translateY = value;
                OnPropertyChanged(nameof(TranslateY));
                ZoomTranslateY = -RectTop * this.ratio * ZoomScaleY; 
            }
        }

        [ObservableProperty]
        private double _zoomScaleX;

        [ObservableProperty]
        private double _zoomScaleY;

        [ObservableProperty]
        private double _zoomTranslateX;

        [ObservableProperty]
        private double _zoomTranslateY;

        [ObservableProperty]
        private double _miniMapTranslateX;

        [ObservableProperty]
        private double _miniMapTranslateY;

        [ObservableProperty]
        private double _foVScaleX;

        [ObservableProperty]
        private double _foVScaleY;

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

        private double imageSize;

        private double minimapSize;

        private double zoomScaleDefault;

        private double zoomScaleMax;

        private double foV = 1.0f;

        private double foVPosition;

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

        public Zoom(double imageSize = Constants.CrossSectionSize, double minimapSize = Constants.MiniMapCanvasSize)
        {
            this.minimapSize = minimapSize;
            this.imageSize = imageSize;
            this.ratio = this.imageSize / this.minimapSize;
            this.zoomScaleDefault = this.imageSize / Constants.OCTImageSize;
            this.zoomScaleMax = this.zoomScaleDefault * Math.Pow(Constants.AnnotationScale, 4);

            ScaleX = this.zoomScaleDefault;
            ScaleY = this.zoomScaleDefault;
            RectLeft = 0;
            RectTop = 0;
            RectWidth = this.minimapSize;
            RectHeight = this.minimapSize;
            Visibility = Visibility.Collapsed;
        }

        public void SetFieldOfView(double fieldOfView)
        {
            this.foV = fieldOfView;
            this.foVPosition = this.minimapSize / 2 - this.minimapSize / this.foV / 2;
            this.zoomScaleDefault = this.imageSize / Constants.OCTImageSize * this.foV;
            this.zoomScaleMax = this.zoomScaleDefault * Math.Pow(Constants.AnnotationScale, 4);

            FoVScaleX = this.zoomScaleDefault;
            FoVScaleY = this.zoomScaleDefault;
            ScaleX = this.zoomScaleDefault;
            ScaleY = this.zoomScaleDefault;
            RectLeft = 0;
            RectTop = 0;
            TranslateX = GetTranslateX();
            TranslateY = GetTranslateY();
            RectWidth = this.minimapSize;
            RectHeight = this.minimapSize;
            MiniMapTranslateX = (this.minimapSize - this.minimapSize * this.foV) / 2;
            MiniMapTranslateY = (this.minimapSize - this.minimapSize * this.foV) / 2;
            Visibility = Visibility.Collapsed;
        }

        private double GetTranslateX()
        {
            return -(RectLeft / this.foV + this.foVPosition) * this.ratio * ScaleX;
        }

        private double GetTranslateY()
        {
            return -(RectTop / this.foV + this.foVPosition) * this.ratio * ScaleY;
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
                {
                    RectLeft = 0;
                }
                else if(nextX + RectWidth > this.minimapSize)
                {
                    RectLeft = this.minimapSize - RectWidth;
                }
                else
                {
                    RectLeft = nextX;
                }

                if (nextY < 0)
                {
                    RectTop = 0;
                }                    
                else if (nextY + RectHeight > this.minimapSize)
                {
                    RectTop = this.minimapSize - RectHeight;
                }
                else
                {
                    RectTop = nextY;
                }

                TranslateX = GetTranslateX();
                TranslateY = GetTranslateY();
            }
        }

        public bool ZoomIn()
        {
            _log.Debug("ZoomIn");

            if (ScaleX >= this.zoomScaleMax)
                return false;

            double nextScale = ScaleX * Constants.AnnotationScale;

            if (CommonUtil.GetRoundScale(nextScale) <= CommonUtil.GetRoundScale(this.zoomScaleMax))
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

            if (CommonUtil.GetRoundScale(nextScale) >= CommonUtil.GetRoundScale(this.zoomScaleDefault))
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

        public static void Window_ManipulationStarting(ManipulationStartingEventArgs e)
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

                if (CommonUtil.GetRoundScale(newScale) <= CommonUtil.GetRoundScale(this.zoomScaleMax) && CommonUtil.GetRoundScale(newScale) >= CommonUtil.GetRoundScale(this.zoomScaleDefault))
                {
                    ScaleX = newScale;
                    ScaleY = newScale;

                    if (deltaScale > 1) //확대
                        ZoomInSetting(deltaScale);

                    else if (deltaScale < 1) //축소
                        ZoomOutSetting(1 / deltaScale);
                }
                else if (CommonUtil.GetRoundScale(newScale) < CommonUtil.GetRoundScale(this.zoomScaleDefault))
                {                   
                    ScaleX = this.zoomScaleDefault;
                    ScaleY = this.zoomScaleDefault;
                    deltaScale = this.zoomScaleDefault / ScaleX;
                    ZoomOutSetting(deltaScale);
                }
            }

            e.Handled = true;
        }

        public static void Window_ManipulationCompleted(ManipulationCompletedEventArgs e)
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
            TranslateX = GetTranslateX();
            TranslateY = GetTranslateY();
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

            if (CommonUtil.GetRoundScale(ScaleX) <= CommonUtil.GetRoundScale(this.zoomScaleDefault))
            {
                Visibility = Visibility.Collapsed;
                RectTop = 0;
                RectLeft = 0;
                RectWidth = this.minimapSize;
                RectHeight = this.minimapSize;
            }

            TranslateX = GetTranslateX();
            TranslateY = GetTranslateY();
        }
    }
}
