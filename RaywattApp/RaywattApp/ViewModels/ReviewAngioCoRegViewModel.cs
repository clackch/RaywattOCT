using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using System.Diagnostics;
using OpenCvSharp;
using System.IO;
using RaywattApp.Common.Converters;
using RaywattApp.Common.Annotation.Models;
using System.Windows.Media;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;
using Point = System.Windows.Point;

namespace RaywattApp.ViewModels
{
    public partial class ReviewAngioCoRegViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewAngioCoRegViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        private ICommand _cancleCommand;
        public ICommand CancelCommand
        {
            get { return this._cancleCommand ?? (this._cancleCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand(Ok)); }
        }

        public List<Mat> CrossSectionAngioImages { get; private set; }
        private List<ImageSource> crossSectionAngioImageSources { get; set; }
        public ImageSource CurrentAngioImage
        {
            get
            {
                if (crossSectionAngioImageSources != null && _angioFrameNumber >= 0 && _angioFrameNumber < crossSectionAngioImageSources.Count)
                {
                    return crossSectionAngioImageSources[_angioFrameNumber];
                }
                return null;
            }
        }

        private Point _crossSectionMousePosition;
        public Point CrossSectionMousePosition
        {
            get => _crossSectionMousePosition;
            set
            {
                _crossSectionMousePosition = value;
                OnPropertyChanged(nameof(CrossSectionMousePosition));
                _log.Debug("CrossSetionMousePosition" + CrossSectionMousePosition.X.ToString());
            }
        }

        private int _angioFrameNumber;
        public int AngioFrameNumber
        {
            get => _angioFrameNumber;
            set
            {
                _angioFrameNumber = value;
                OnPropertyChanged(nameof(AngioFrameNumber));
                OnPropertyChanged(nameof(CurrentAngioImage));
            }
        }

        private int _angioFrameLength;
        public int AngioFrameLength
        {
            get => _angioFrameLength;
            set
            {
                _angioFrameLength = value;
                OnPropertyChanged(nameof(AngioFrameLength));
            }
        }

        public ReviewAngioCoRegViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewAngioCoRegViewModel");

            Constants.CurrentPage = Constants.ReviewAngioCoRegPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            CrossSectionAngioImages = new List<Mat>();
            crossSectionAngioImageSources = new List<ImageSource>();
            ReadAngioFrames();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Ok()
        {
            _log.Debug("Ok");

            GoToPreviousPage(true);
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            GoToPreviousPage(false);
        }

        private void GoToPreviousPage(bool isSave)
        {
            _log.Debug("GoToPreviousPage");

            if (isSave)
            {

            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(ReviewStatus.CurrentPage) { Parameter = parameter });
        }

        void ReadAngioFrames()
        {
            int x1 = 240, y1 = 70, x2 = 780, y2 = 970;
            string[] filePaths = Directory.GetFiles(@"C:\Raywatt\system\3rdparty\angioSamples", "*.angioframes");

            foreach (string filePath in filePaths)
            {
                using (BinaryReader reader = new BinaryReader(System.IO.File.Open(filePath, FileMode.Open)))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        int width = x2 - x1, height = y2 - y1;
                        int channels = 1;

                        byte[] data = reader.ReadBytes(1024 * 1024 * channels);
                        Mat frame = new Mat(1024, 1024, MatType.CV_8UC1, data);

                        OpenCvSharp.Rect roi = new OpenCvSharp.Rect(x1, y1, width, height);
                        frame = new Mat(frame, roi);
                        Cv2.Resize(frame, frame, new OpenCvSharp.Size(Constants.AngioSize, Constants.AngioSize));
                        CrossSectionAngioImages.Add(frame);
                        crossSectionAngioImageSources.Add(ConvertMatsToImageSource(frame));
                    }
                }
                AngioFrameLength = crossSectionAngioImageSources.Count - 1;
                break;
            }
        }

        private ImageSource ConvertMatsToImageSource(Mat mat)
        {
            using (var stream = new MemoryStream())
            {

                mat.WriteToStream(stream, ".bmp");

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
    }
}
