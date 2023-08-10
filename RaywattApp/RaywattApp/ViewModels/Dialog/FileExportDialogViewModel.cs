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

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private string _title = "";

        public UserControl userControl;

        private List<Mat> crossSections;

        private Mat imglumenProfile;
        private Mat imgCrossSectionBackground;
        private Mat imgCrossSectionMask;
        private Mat imglumenProfileExtra;

        [ObservableProperty]
        private int _frameNumber = -1;

        [ObservableProperty]
        private int _displayFrameNumber;

        [ObservableProperty]
        private BitmapSource _crossSectionImage;

        [ObservableProperty]
        private double _crossSectionScale = (1 / Constants.MillimeterPerPixel ) * (Constants.CrossSectionSize / Constants.OCTImageSize);

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        [ObservableProperty]
        private BitmapSource _lumenProfileImage;

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

        [ObservableProperty]
        private Section _section;

        [ObservableProperty]
        private BitmapSource _lumenProfileImageExtra;

        public FileExportDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.ExportLongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;

            TextPartWidth = 0;
            MeasureSeparator = Visibility.Collapsed;

            CurrentLumenContour = new LumenContour();

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;
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
                {
                    Section.Proximal.X = CommonUtil.GetPositionFromFrame(patientCase.SectionProximal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, Constants.SectionIndicatorCenterWidth);
                    Section.Distal.X = CommonUtil.GetPositionFromFrame(patientCase.SectionDistal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);
                    DrawLumenProfileImage();

                    if (CommonUtil.IsPreCase(patientCase.Procedure))
                    {
                        Section.SetMlaMld(LumenContours, patientCase.SectionProximal, patientCase.SectionDistal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, patientCase.PullbackType);
                        Section.VisibleMlaMld(true);
                    }
                    else
                    {
                        Section.SetMsaMinExp(LumenContours, patientCase.SectionProximal, patientCase.SectionDistal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, patientCase.PullbackType);
                        Section.VislbleMsaMinExp(true);
                    }
                }

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
            IndicatorLongitude.X = curPosition - Constants.ExportLongitudeIndicatorWidth / 2;
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

                    int frameProximal = PatientCase.SectionProximal;
                    int frameDistal = PatientCase.SectionDistal;
                    //Test
                    List<int> sidebranchs = new List<int>() { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420 };
                    List<int> appositionFrames = new List<int>() { 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 340, 341, 342, 343, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370 };
                    imglumenProfile = CommonUtil.MakeLumenProfileImage(LumenContours, frameProximal, frameDistal, sidebranchs, CommonUtil.IsPostCase(PatientCase.Procedure), appositionFrames);

                    //Test
                    List<int> colorFrames = new List<int>() { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 300, 301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370 };
                    imglumenProfileExtra = CommonUtil.MakeLumenProfileImageExtra(LumenContours.Count, colorFrames, CommonUtil.IsPreCase(PatientCase.Procedure));
                    LumenProfileImageExtra = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileExtra);
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
