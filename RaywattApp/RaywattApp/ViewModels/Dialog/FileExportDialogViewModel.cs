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
using System.Windows.Media;
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
        private List<ImageSource> angioImages;

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
        private BitmapSource _angioImage;

        [ObservableProperty]
        private double _crossSectionScale;

        [ObservableProperty]
        private BitmapSource _longitudeImage;

        [ObservableProperty]
        private BitmapSource _lumenProfileImage;

        [ObservableProperty]
        private BitmapSource _calciumIndicator;

        [ObservableProperty]
        private double _maxCalciumDegree = -1;

        [ObservableProperty]
        private double _totalAngle;

        [ObservableProperty]
        private double _maxThickness;

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
        private List<LumenSidebranch> _lumenSidebranches = new List<LumenSidebranch>();

        [ObservableProperty]
        private bool _isDrawLumenSideBranch = false;

        [ObservableProperty]
        private List<LumenStent> _lumenStents = new List<LumenStent>();

        [ObservableProperty]
        private List<LumenGuidewire> _lumenGuidewires = new List<LumenGuidewire>();

        [ObservableProperty]
        private double _imagePartWidth;

        [ObservableProperty]
        private double _imagePartHeight;

        [ObservableProperty]
        private double _crossSectionPartWidth;

        [ObservableProperty]
        private double _textPartWidth;

        [ObservableProperty]
        private double _crossSectionSize;

        [ObservableProperty]
        private double _crossSectionImageSize;

        [ObservableProperty]
        private double _angioWidth;

        [ObservableProperty]
        private double _angioHeight;

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

        [ObservableProperty]
        private double _calciumIndicatorSize;

        [ObservableProperty]
        private double _calciumThicknessIndicatorSize;

        [ObservableProperty]
        private double _calciumThicknessIndicatorCenter;

        [ObservableProperty]
        private Point _calciumThicknessIndicatorPointCenter;

        [ObservableProperty]
        private double _crossSectionClipRadius;

        [ObservableProperty]
        private Point _crossSectionClipCenter;

        [ObservableProperty]
        private BitmapSource _sheathIndicator;

        public FileExportDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.ExportLongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;

            ImagePartWidth = Constants.ExportLongitudeWidth;
            ImagePartHeight = Constants.ExportHeight;
            TextPartWidth = 0;
            MeasureSeparator = Visibility.Collapsed;

            CurrentLumenContour = new LumenContour();

            Section = new Section();
            Section.Proximal.IsVisible = Visibility.Visible;
            Section.Distal.IsVisible = Visibility.Visible;

            if (CommonUtil.IsTestMode(DeviceStatus.TestMode, "Sidebranch"))
                IsDrawLumenSideBranch = true;
        }

        public double SetInitialize(PatientCase patientCase, List<Mat> crossSections, Mat lMode, FileExport fileExport)
        {
            PatientCase = patientCase;
            Degree = PatientCase.IndicatorDegree;            

            this.crossSections = crossSections;
            if (fileExport.AngioView && patientCase.AngioYn)
            {
                CommonUtil.ReadAngioParams(PatientCase);
                CommonUtil.ReadAngioImages(PatientCase);
                this.angioImages = PatientCase.AngioFrame.AngioImage;
            }

            imgCrossSectionMask = GenerateMask(crossSections[0]);
            imgCrossSectionBackground = crossSections[0].EmptyClone();
            LongitudeImage = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(lMode);

            FileExport = fileExport;

            if(fileExport.Longitude || fileExport.MeasureAuto || fileExport.MeasureManual)
                SetAnnotation();

            if ((fileExport.AngioView && patientCase.AngioYn) || fileExport.Longitude)
            {
                CrossSectionPartWidth = Constants.ExportCrossSectionSmall;
                CrossSectionSize = Constants.ExportCrossSectionSmall;
                CrossSectionImageSize = Constants.ExportCrossSectionImageSmall;

                if (fileExport.Longitude)
                {
                    ImagePartHeight = Constants.ExportHeight - Constants.ExportLongitudeHeight;

                    Section.Proximal.X = CommonUtil.GetPositionFromFrame(patientCase.SectionProximal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, Constants.SectionIndicatorCenterWidth);
                    Section.Distal.X = CommonUtil.GetPositionFromFrame(patientCase.SectionDistal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, Constants.SectionIndicatorWidth - Constants.SectionIndicatorCenterWidth);

                    int frameProximal = PatientCase.SectionProximal;
                    int frameDistal = PatientCase.SectionDistal;
                    
                    imglumenProfile = CommonUtil.MakeLumenProfileImage(LumenContours, LumenSidebranches, LumenStents, patientCase.AppositionThreshold, frameProximal, frameDistal, CommonUtil.IsPostCase(PatientCase.Procedure));
                    DrawLumenProfileImage();

                    List<int> colorFrames = new List<int>();
                    if (CommonUtil.IsPreCase(patientCase.Procedure))
                    {
                        if(Section.SetMlaMld(LumenContours, patientCase.SectionProximal, patientCase.SectionDistal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, patientCase.PullbackLength))
                            Section.VisibleMlaMld(true);
                        else
                            Section.VisibleMlaMld(false);

                        colorFrames = CommonUtil.GetCalciumList(LumenContours, patientCase.CalciumThreshold);
                    }
                    else
                    {
                        int stentProximal = 0, stentDistal = 0;
                        CommonUtil.GetStentProximalDistal(LumenStents, out stentProximal, out stentDistal);

                        if (Section.SetMsaMinExp(LumenContours, patientCase.SectionProximal, patientCase.SectionDistal, stentProximal, stentDistal, this.crossSections.Count, Constants.ExportLongitudeImageWidth, patientCase.PullbackLength))
                            Section.VislbleMsaMinExp(true);
                        else
                            Section.VislbleMsaMinExp(false);

                        colorFrames = CommonUtil.GetExpansionList(LumenContours, frameProximal, frameDistal, stentProximal, stentDistal, Section.RefArea, PatientCase.ExpansionThreshold);
                    }
                    imglumenProfileExtra = CommonUtil.MakeLumenProfileImageExtra(LumenContours.Count, colorFrames, CommonUtil.IsPreCase(PatientCase.Procedure));
                    DrawLumenProfileImageExtra();
                }

                if (!fileExport.AngioView || !patientCase.AngioYn)
                    CrossSectionPartWidth = Constants.ExportLongitudeWidth;
            }
            else
            {
                ImagePartWidth = Constants.ExportCrossSectionBig;
                CrossSectionPartWidth = Constants.ExportCrossSectionBig;
                CrossSectionSize = Constants.ExportCrossSectionBig;
                CrossSectionImageSize = Constants.ExportCrossSectionBig;

                if (fileExport.MeasureAuto || fileExport.MeasureManual)
                    CrossSectionImageSize = Constants.ExportCrossSectionImageBig;
            }

            if (fileExport.MeasureAuto || fileExport.MeasureManual)
            {
                if (CrossSectionPartWidth == Constants.ExportCrossSectionBig)
                {
                    ImagePartWidth = Constants.ExportLongitudeWidth;
                    CrossSectionPartWidth = Constants.ExportLongitudeWidth;
                }
                TextPartWidth = Constants.ExportTextPartSize;

                if (fileExport.MeasureManual)
                    MeasurementCommand = Constants.MeasureDrawAll;                
            }

            if(fileExport.MeasureAuto && fileExport.MeasureManual)
                MeasureSeparator = Visibility.Visible;

            //for Calcium
            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (FileExport.Longitude || (FileExport.AngioView && PatientCase.AngioYn))
                {
                    CalciumIndicatorSize = Constants.CalciumIndicatorExportSize;
                    CalciumThicknessIndicatorSize = Constants.CalciumThicknessIndicatorExportSize;
                    CalciumThicknessIndicatorCenter = Constants.CalciumThicknessIndicatorCenterExport;
                    CalciumThicknessIndicatorPointCenter = Constants.CalciumThicknessIndicatorPointCenterExport;
                }
                else if (FileExport.MeasureAuto)
                {
                    CalciumIndicatorSize = Constants.CalciumIndicatorExportSizeBig;
                    CalciumThicknessIndicatorSize = Constants.CalciumThicknessIndicatorExportSizeBig;
                    CalciumThicknessIndicatorCenter = Constants.CalciumThicknessIndicatorCenterExportBig;
                    CalciumThicknessIndicatorPointCenter = Constants.CalciumThicknessIndicatorPointCenterExportBig;
                }
            }

            CrossSectionScale = (1 / Constants.ImageResolution) * (CrossSectionImageSize / Constants.OCTImageSize);
            Zoom = new Zoom(CrossSectionImageSize);
            Zoom.SetFieldOfView(Constants.DefaultFoV / PatientCase.FieldOfView);
            LongitudeZoom = new Zoom();
            LongitudeZoom.ScaleX = Constants.ExportLongitudeImageWidth / Constants.LongitudeWidth;
            LongitudeZoom.ScaleY = Constants.ExportLongitudeImageHeight / Constants.LongitudeHeight;
            CrossSectionClipRadius = CrossSectionImageSize / 2;
            CrossSectionClipCenter = new Point(CrossSectionClipRadius, CrossSectionClipRadius);

            double sheathDiameter = RayGetProperty(Property.SheathDiameter);
            SheathIndicator = CommonUtil.DrawSheathIndicator((int)CrossSectionImageSize, sheathDiameter);

            return ImagePartWidth + TextPartWidth;
        }

        public void SetFinalize()
        {

        }

        public void SetFrameNumber(int frameNumber)
        {
            CrossSectionImage = DrawCrossSectionWithBackground(crossSections[frameNumber], new Scalar(0x0d, 0x0d, 0x0d));

            if (FileExport.AngioView && PatientCase.AngioYn)
            {
                double ratio = (double)PatientCase.AngioFrame.AngioImage.Count / crossSections.Count() * frameNumber ;
                int currentAngioFrameNumber = (int)ratio;
                AngioImage = (BitmapSource)angioImages[currentAngioFrameNumber];
            }

            FrameNumber = frameNumber;
            DisplayFrameNumber = frameNumber + 1;

            if (CommonUtil.IsPreCase(PatientCase.Procedure))
            {
                if (FileExport.Longitude)
                {
                    DrawCalciumIndicator((int)Constants.CalciumIndicatorExportSize);
                }
                else if (FileExport.MeasureAuto)
                {
                    DrawCalciumIndicator((int)Constants.CalciumIndicatorExportSizeBig);
                }
            }

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

                //sidebranch
                LumenSidebranch lumenSidebranch = new LumenSidebranch();
                LumenSidebranches.Add(lumenSidebranch);

                //stent
                LumenStent lumenStent = new LumenStent();
                LumenStents.Add(lumenStent);

                //guidewire
                LumenGuidewire lumenGuidewire = new LumenGuidewire();
                LumenGuidewires.Add(lumenGuidewire);
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
                    LumenContours = CommonUtil.JsonToLumenContours(patientCaseAnnotations[0].LumenContour);
                }

                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].LumenSidebranch))
                {
                    LumenSidebranches = JsonConvert.DeserializeObject<List<LumenSidebranch>>(patientCaseAnnotations[0].LumenSidebranch);
                }

                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].LumenStent))
                {
                    LumenStents = JsonConvert.DeserializeObject<List<LumenStent>>(patientCaseAnnotations[0].LumenStent);
                }

                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].LumenGuidewire))
                {
                    LumenGuidewires = JsonConvert.DeserializeObject<List<LumenGuidewire>>(patientCaseAnnotations[0].LumenGuidewire);
                }

                if (!string.IsNullOrEmpty(patientCaseAnnotations[0].CoRegistration))
                {
                    PatientCase.StrCoRegistration = patientCaseAnnotations[0].CoRegistration;
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

        private bool DrawLumenProfileImageExtra()
        {
            if (imglumenProfileExtra == null) return false;

            LumenProfileImageExtra = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(imglumenProfileExtra);
            return true;
        }

        private void DrawCalciumIndicator(int calciumIndicatorSize)
        {
            if (LumenContours[FrameNumber].Calcium == null)
                return;
            CalciumIndicator = CommonUtil.DrawCalciumIndicator(LumenContours[FrameNumber].Calcium.List, Constants.CalciumIndicatorColor, calciumIndicatorSize);

            TotalAngle = LumenContours[FrameNumber].Calcium.TotalAngle;
            MaxThickness = LumenContours[FrameNumber].Calcium.MaxThickness;
            MaxCalciumDegree = LumenContours[FrameNumber].Calcium.MaxThicknessDegree;
        }
    }
}
