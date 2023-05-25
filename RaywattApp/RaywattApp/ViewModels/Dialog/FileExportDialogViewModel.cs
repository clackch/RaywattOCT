using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using Newtonsoft.Json;
using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static RaywattOCT.RayCoreWrapper;
using Point = System.Windows.Point;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class FileExportDialogViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

        protected readonly SqlManager _sqlManager;

        [ObservableProperty]
        private string _title = "";

        public UserControl userControl;

        private List<Mat> crossSections;        

        [ObservableProperty]
        private int _frameNumber = -1;

        [ObservableProperty]
        private int _displayFrameNumber;

        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        [ObservableProperty]
        protected double _crossSectionScale = 65;

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        [ObservableProperty]
        private BitmapSource _lumenProfileImage;

        protected Mat imglumenProfile;
        protected Mat imgCrossSectionBackground;
        protected Mat imgCrossSectionMask;

        private double degree;
        public double Degree
        {
            get { return degree; }
            set { degree = value; OnPropertyChanged(nameof(Degree)); RaySetProperty(Property.LongitudeDegree, degree); }
        }

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private ObservableCollection<AreaGeometry> _currAreaGeometries;

        private List<Measurement> measurements = new List<Measurement>();
        public List<Measurement> Measurements { get { return measurements; } set { measurements = value; OnPropertyChanged(nameof(Measurements)); } }

        private ObservableCollection<LengthGeometry> _lModeLengthGeometries;
        public ObservableCollection<LengthGeometry> LModeLengthGeometries { get { return _lModeLengthGeometries; } set { _lModeLengthGeometries = value; OnPropertyChanged(nameof(LModeLengthGeometries)); } }

        private List<TextGeometry> _lModeTextGeometries;
        public List<TextGeometry> LModeTextGeometries { get { return _lModeTextGeometries; } set { _lModeTextGeometries = value; OnPropertyChanged(nameof(LModeTextGeometries)); } }

        [ObservableProperty]
        private LumenContour _currentLumenContour;

        private List<LumenContour> _lumenContours = new List<LumenContour>();
        public List<LumenContour> LumenContours { get { return _lumenContours; } set { _lumenContours = value; OnPropertyChanged(nameof(LumenContours)); } }

        [ObservableProperty]
        private double _crossSectionPartWidth;

        [ObservableProperty]
        private double _textPartWidth;

        [ObservableProperty]
        private double _crossSectionSize;

        [ObservableProperty]
        private double _crossSectionImageSize;

        [ObservableProperty]
        private Visibility _measureSeparator;

        [ObservableProperty]
        private Zoom _zoom;

        [ObservableProperty]
        private Zoom _longitudeZoom;

        [ObservableProperty]
        private int _measureAutoFrameNumber = -1;

        [ObservableProperty]
        private int _measureManualFrameNumber = -1;

        [ObservableProperty]
        private string _measurementCommand;

        [ObservableProperty]
        private FileExport? _fileExport;

        public FileExportDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = 12 / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;

            TextPartWidth = 0;
            MeasureSeparator = Visibility.Collapsed;

            CurrentLumenContour = new LumenContour();
        }

        public void SetInitialize(PatientCase patientCase, List<Mat> crossSections, Mat lMode, FileExport fileExport)
        {
            PatientCase = patientCase;
            Degree = PatientCase.IndicatorDegree;

            this.crossSections = crossSections;
            imgCrossSectionMask = GenerateMask(crossSections[0]);
            imgCrossSectionBackground = crossSections[0].EmptyClone();
            LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(lMode);

            FileExport = fileExport;

            if(fileExport.Longitude || fileExport.MeasureAuto || fileExport.MeasureManual)
                SetAnnotation();

            if (fileExport.AngioView || fileExport.Longitude)
            {
                CrossSectionPartWidth = Constants.ExportCrossSectionSmall;
                CrossSectionSize = Constants.ExportCrossSectionSmall;
                CrossSectionImageSize = Constants.ExportCrossSectionImageSmall;

                if (fileExport.Longitude)
                    DrawLumenProfileImage();
                if (!fileExport.AngioView)
                    CrossSectionPartWidth = Constants.ExportLongitudeWidth;
            }
            else
            {
                CrossSectionPartWidth = Constants.ExportCrossSectionBig;
                CrossSectionSize = Constants.ExportCrossSectionBig;
                CrossSectionImageSize = Constants.ExportCrossSectionBig;

                if (fileExport.MeasureAuto || fileExport.MeasureManual)
                    CrossSectionImageSize = Constants.ExportCrossSectionImageBig;
            }

            if (fileExport.MeasureAuto || fileExport.MeasureManual)
            {
                if(CrossSectionPartWidth == Constants.ExportCrossSectionBig)
                    CrossSectionPartWidth = Constants.ExportLongitudeWidth;
                TextPartWidth = Constants.ExportTextPartSize;

                if (fileExport.MeasureManual)
                    MeasurementCommand = Constants.MeasureDrawAll;                
            }

            if(fileExport.MeasureAuto && fileExport.MeasureManual)
                MeasureSeparator = Visibility.Visible;

            Zoom = new Zoom(CrossSectionImageSize / Constants.OCTImageSize);
            LongitudeZoom = new Zoom();
            LongitudeZoom.ScaleX = Constants.ExportLongitudeImageWidth / Constants.LongitudeWidth;
            LongitudeZoom.ScaleY = Constants.ExportLongitudeImageHeight / Constants.LongitudeHeight;
        }

        public void SetFinalize()
        {

        }

        public void SetFrameNumber(int frameNumber)
        {
            CrossSectionImage = DrawCrossSectionWithBackground(crossSections[frameNumber], new Scalar(0x0d, 0x0d, 0x0d));

            FrameNumber = frameNumber;
            DisplayFrameNumber = frameNumber + 1;

            if (FileExport.MeasureAuto)
                MeasureAutoFrameNumber = frameNumber;

            if (FileExport.MeasureManual)
            {
                MeasureManualFrameNumber = frameNumber;
                CurrAreaGeometries = Measurements[frameNumber].AreaGeometries;
            }

            updateNavigator(frameNumber, this.crossSections.Count);
        }

        private BitmapSource DrawCrossSectionWithBackground(Mat image, Scalar background)
        {
            // Background Masking
            imgCrossSectionBackground.SetTo(background);
            Cv2.CopyTo(imgCrossSectionBackground, image, imgCrossSectionMask);

            BitmapSource bitmap = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(image);

            return bitmap;
        }

        private Mat GenerateMask(Mat image)
        {
            Mat mask = image.EmptyClone();
            OpenCvSharp.Point center = new OpenCvSharp.Point(mask.Width / 2, mask.Height / 2);

            mask.SetTo(Scalar.White);
            Cv2.Circle(mask, center, mask.Width / 2, Scalar.Black, -1);

            return mask;
        }

        private void updateNavigator(int curFrame, int totalFrame)
        {
            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= Constants.ExportLongitudeImageWidth;
            IndicatorLongitude.X = curPosition - 12 / 2;
            IndicatorLongitude.CenterX = curPosition;
        }

        private void SetAnnotation()
        {
            // Initialize with empty objects
            for (int i = 0; i < this.crossSections.Count; i++)
            {
                LumenContour lumenContour = new LumenContour();
                DiameterInfo diameterInfo = new DiameterInfo();
                diameterInfo.value = 0.0;

                lumenContour.MlContour.Points = new List<Point>();
                lumenContour.MlContour.MaxDiameter = diameterInfo;
                lumenContour.MlContour.MinDiameter = diameterInfo;
                lumenContour.Points = new List<Point>();
                lumenContour.MaxDiameter = diameterInfo;
                lumenContour.MinDiameter = diameterInfo;

                LumenContours.Add(lumenContour);
            }

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            IList<PatientCaseAnnotation> patientCaseAnnotations = _sqlManager.SelectPatientCaseAnnotation(sqlParameters);

            if (patientCaseAnnotations != null && patientCaseAnnotations.Count == 1)
            {
                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].CrossSection))
                {
                    Measurements = JsonConvert.DeserializeObject<List<Measurement>>(patientCaseAnnotations[0].CrossSection);
                }

                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].Longitude))
                {
                    Measurement lModeMeasurement = JsonConvert.DeserializeObject<Measurement>(patientCaseAnnotations[0].Longitude);
                    LModeLengthGeometries = lModeMeasurement.LengthGeometries;
                    LModeTextGeometries = lModeMeasurement.TextGeometries;
                }

                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].LumenContour))
                {
                    LumenContours = JsonConvert.DeserializeObject<List<LumenContour>>(patientCaseAnnotations[0].LumenContour);
                    imglumenProfile = CommonUtil.MakeLumenProfileImage(LumenContours);
                }
            }

            for (int i = 0; i < this.crossSections.Count; i++)
            {
                Measurement measurement = new Measurement();
                measurement.FrameNumber = i;
                measurement.AreaGeometries = new ObservableCollection<AreaGeometry>();
                measurement.LengthGeometries = new ObservableCollection<LengthGeometry>();
                measurement.TextGeometries = new List<TextGeometry>();
                Measurements.Add(measurement);
            }

            Measurements = Measurements.DistinctBy(x => x.FrameNumber).OrderBy(x => x.FrameNumber).ToList();
        }

        private bool DrawLumenProfileImage()
        {
            if (imglumenProfile == null) return false;

            LumenProfileImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfile);
            return true;
        }
    }
}
