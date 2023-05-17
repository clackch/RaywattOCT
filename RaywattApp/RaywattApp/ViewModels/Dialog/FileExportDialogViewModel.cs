using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using Newtonsoft.Json;
using OpenCvSharp;
using RaywattApp.Common.Annotation.Models;
using RaywattApp.Common.Bases;
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

        private double crossSectionBig = 1024;
        private double crossSectionSmall = 500;

        [ObservableProperty]
        private double _crossSectionSize;

        [ObservableProperty]
        private double _longitudeWidth;

        [ObservableProperty]
        private double _longitudeHeight;

        [ObservableProperty]
        private double _longitudeScaleX;

        [ObservableProperty]
        private double _longitudeScaleY;

        [ObservableProperty]
        private Zoom _zoom = new Zoom();

        [ObservableProperty]
        private int _measureAutoframeNumber = -1;

        [ObservableProperty]
        private int _measureManualframeNumber = -1;

        [ObservableProperty]
        private string _measurementCommand;

        [ObservableProperty]
        private FileExport? _fileExport;

        public FileExportDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;

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
                CrossSectionSize = crossSectionSmall;

                if (fileExport.Longitude)
                {
                    LongitudeWidth = 884;
                    LongitudeHeight = 105;
                    LongitudeScaleX = LongitudeWidth / Constants.LongitudeWidth;
                    LongitudeScaleY = LongitudeHeight / Constants.LongitudeHeight;

                    DrawLumenProfileImage();
                }
            }
            else
            {
                CrossSectionSize = crossSectionBig;
            }

            Zoom.ScaleX = CrossSectionSize / crossSectionBig;
            Zoom.ScaleY = CrossSectionSize / crossSectionBig;

            if(fileExport.MeasureAuto || fileExport.MeasureManual)
            {
                if(fileExport.MeasureManual)
                    MeasurementCommand = Constants.MeasureDrawAll;
            }
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
                MeasureAutoframeNumber = frameNumber;

            if(FileExport.MeasureManual)
                MeasureManualframeNumber = frameNumber;

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
            curPosition *= LongitudeWidth;
            IndicatorLongitude.X = curPosition - Constants.LongitudeIndicatorWidth / 2;
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
                    MakeLumenProfileImage(LumenContours);
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

        private bool MakeLumenProfileImage(List<LumenContour> lumenContours)
        {
            const double totalArea = 512 * 512 * Math.PI;

            if (imglumenProfile == null)
            {
                imglumenProfile = new Mat(100, lumenContours.Count, MatType.CV_8UC3);
            }
            imglumenProfile.SetTo(new Scalar(0x4f, 0x4f, 0x4f));

            int curFrame = 0;
            foreach (LumenContour lumenContour in lumenContours)
            {
                double area = lumenContour.MlContour.Area;

                int lumenArea = (int)(area / totalArea * imglumenProfile.Rows);
                int yStart = (imglumenProfile.Rows - lumenArea) / 2;

                Cv2.Line(imglumenProfile, new OpenCvSharp.Point(curFrame, yStart), new OpenCvSharp.Point(curFrame, yStart + lumenArea), new Scalar(0x16, 0x16, 0x16));
                curFrame++;
            }

            return true;
        }

        private bool DrawLumenProfileImage()
        {
            if (imglumenProfile == null) return false;

            LumenProfileImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfile);
            return true;
        }
    }
}
