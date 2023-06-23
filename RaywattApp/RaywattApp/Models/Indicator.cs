using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using System.Windows.Input;
using System.Windows;
using RaywattApp.Common.Bases;

namespace RaywattApp.Models
{
    public partial class Indicator : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Indicator));

        [ObservableProperty]
        public bool _isCaptured = false;

        [ObservableProperty]
        public Visibility _isVisible;

        [ObservableProperty]
        public bool _isEnabled;

        [ObservableProperty]
        public double _x = double.NaN;

        [ObservableProperty]
        public double _y = double.NaN;

        [ObservableProperty]
        public bool _isValid = false;

        [ObservableProperty]
        public double _centerX;

        public bool OppositeCaptured = false;

        public bool IsCompare = false;

        public bool IsCrossSection = false;

        public bool IsLongitudeClicked = false;

        public bool IsLongitudeMove = false;

        public bool IsCrossSectionClicked = false;

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
