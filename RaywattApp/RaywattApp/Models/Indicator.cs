using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using System.Windows.Input;
using System.Windows;
using RaywattApp.Common.Bases;

namespace RaywattApp.Models
{
    public partial class Indicator : IndicatorBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Indicator));

        public Point Coordinate = new Point();

        [ObservableProperty]
        private bool _isCaptured = false;

        [ObservableProperty]
        private double _pointLongitudeX;

        public double IndicatorDiff;

        public bool OppositeCaptured;

        public bool IsCompare;

        public bool IsCrossSection;

        public bool IsLongitudeClicked;

        public bool IsLongitudeMove;

        public bool IsCrossSectionClicked;

        public bool IsSectionIndicator;

        public bool IsSectionProximal;

        private ICommand _cmdSetCaptured;

        public Indicator()
        {
        }

        public ICommand CmdSetCaptured
        {
            get { return _cmdSetCaptured ?? (this._cmdSetCaptured = new RelayCommand<bool>(SetCaptured)); }
        }

        private void SetCaptured(bool isCaptured)
        {
            IsCaptured = isCaptured;

            if (IsCaptured)
            {
                Constants.mainWindow.Cursor = (Cursor)Application.Current.Resources["grab"];
            }
            else
            {
                Constants.mainWindow.Cursor = (Cursor)Application.Current.Resources["arrow"];
            }

            if (IsCrossSection)
            {
                X = double.NaN;
                Y = double.NaN;
                IsCrossSectionClicked = true;
            }
            else
            {
                IsLongitudeClicked = true;
                IsLongitudeMove = true;
            }
            
            IsValid = false;
        }

        public void SetDirection(Point center, double degree)
        {
            if (double.IsNaN(X) || double.IsNaN(Y) || IsValid) return;

            double yOffset = Y - center.Y;
            if ((yOffset * degree) >= 0)
            {
                OppositeCaptured = true;
            }
            else
            {
                OppositeCaptured = false;
            }
            IsValid = true;
        }
    }
}
