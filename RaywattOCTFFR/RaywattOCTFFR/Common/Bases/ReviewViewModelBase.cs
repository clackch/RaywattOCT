using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using Newtonsoft.Json;
using OpenCvSharp;
using RaywattOCTFFR.Common.Annotation.Models;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using static RaywattOCT.RayCoreFFRWrapper;
using RaywattOCTFFR.Common.Util;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace RaywattOCTFFR.Common.Bases
{
    public abstract partial class ReviewViewModelBase : OCTViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModelBase));

        protected readonly SqlManager _sqlManager;

        protected IDialogService _dialogService;

        protected CancellationTokenSource? _cts;

        protected bool isEndReview = true;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private ReviewStatus _reviewStatus = new ReviewStatus();

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private bool _expandLeftUpMenu;

        [ObservableProperty]
        private bool _expandLeftDownMenu;

        [ObservableProperty]
        private bool _expandRightMenu;

        private int frameNumber = -1;
        public int FrameNumber
        {
            get { return frameNumber; }
            set
            {
                frameNumber = value;
                OnPropertyChanged(nameof(FrameNumber));
                DisplayFrameNumber = FrameNumber + 1;
            }
        }

        [ObservableProperty]
        private int displayFrameNumber;

        [ObservableProperty]
        private ObservableCollection<Bookmark> bookmarks;

        [ObservableProperty]
        private Indicator _indicatorLongitude;

        [ObservableProperty]
        private FileImport _fileImport = new FileImport();

        private ICommand _reviewTypeSwitchCommand;
        public ICommand ReviewTypeSwitchCommand
        {
            get { return this._reviewTypeSwitchCommand ?? (this._reviewTypeSwitchCommand = new RelayCommand<string>(ReviewTypeSwitch)); }
        }

        private ICommand _editPresetCommand;
        public ICommand EditPresetCommand
        {
            get { return this._editPresetCommand ?? (this._editPresetCommand = new RelayCommand(EditPreset)); }
        }

        private ICommand _endReviewCommand;
        public ICommand EndReviewCommand
        {
            get { return this._endReviewCommand ?? (this._endReviewCommand = new RelayCommand(EndReview)); }
        }

        private ICommand _expandCollapseCommand;
        public ICommand ExpandCollapseCommand
        {
            get { return this._expandCollapseCommand ?? (this._expandCollapseCommand = new RelayCommand<string>(ExpandCollapseMenu)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next)); }
        }

        private ICommand _cmdMoveIndicator;
        public ICommand CmdMoveIndicator
        {
            get { return this._cmdMoveIndicator ?? (this._cmdMoveIndicator = new RelayCommand<object>(MoveIndicator)); }
        }

        private ICommand _cmdTouchMoveIndicator;
        public ICommand CmdTouchMoveIndicator
        {
            get { return this._cmdTouchMoveIndicator ?? (this._cmdTouchMoveIndicator = new RelayCommand<object>(TouchMoveIndicator)); }
        }

        private ICommand _cmdPlayback;
        public ICommand CmdPlayback
        {
            get { return this._cmdPlayback ?? (this._cmdPlayback = new RelayCommand<object>(Playback)); }
        }

        public ReviewViewModelBase()
        {
            _log.Debug("ReviewViewModelBase");
        }

        public ReviewViewModelBase(IDialogService dialogService)
        {
            _log.Debug("ReviewViewModelBase");

            _dialogService = dialogService;
        }

        public ReviewViewModelBase(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewViewModelBase");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
            base.OnNavigated(sender, navigatedEventArgs);
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            base.OnNavigating(sender, navigationEventArgs);

            if (_cts != null && !_cts.IsCancellationRequested)
                _cts?.Cancel();
        }

        private void ReviewTypeSwitch(string url)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;

            WeakReferenceMessenger.Default.Send(new NavigationMessage(url) { Parameter = parameter });
        }

        private void EditPreset()
        {
            _log.Debug("EditPreset");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            parameter["reviewStatus"] = ReviewStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.ReviewPresetPage) { Parameter = parameter });
        }

        private List<int> GetBookmarks()
        {
            List<int> bookmarks = new List<int>();

            if(Constants.CurrentPage != Constants.ReviewPage)
            {
                Bookmarks = JsonConvert.DeserializeObject<ObservableCollection<Bookmark>>(PatientCase.Bookmark);
            }

            foreach(Bookmark bookmark in Bookmarks)
            {
                bookmarks.Add(bookmark.FrameNumber);
            }

            bookmarks.Sort();

            return bookmarks;
        }

        protected virtual void Save() { }

        protected virtual void Back() { }

        protected virtual void Next() { }

        private void EndReview()
        {
            _log.Debug("EndReview");

            DeviceStatus.IsOCTImagingDone = true;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
        }

        private void ExpandCollapseMenu(string param)
        {
            switch (param)
            {
                case Constants.LeftUpMenu:
                    ExpandLeftUpMenu = !ExpandLeftUpMenu;
                    break;
                case Constants.LeftDownMenu:
                    ExpandLeftDownMenu = !ExpandLeftDownMenu;
                    break;
                case Constants.RightMenu:
                    ExpandRightMenu = !ExpandRightMenu;
                    break;
                default:
                    break;
            }
        }

        protected void StopPlayback()
        {
            if (!DeviceStatus.IsPaused)
            {
                Playback();
                ReviewStatus.IsPlay = false;
            }
                
        }

        protected void Playback(object param)
        {
            string action = (string)param;

            if (action.ToLower().Equals("prev"))
            {
                StopPlayback();

                PrevFrame(RaySession.Review);
            }
            else if (action.ToLower().Equals("next"))
            {
                StopPlayback();

                NextFrame(RaySession.Review);
            }
            else if (action.ToLower().Equals("play"))
            {
                Playback();

                if (!IsPaused)
                {
                    ReviewStatus.IsMeasurementOn = false;
                    ReviewStatus.IsPlay = true;
                }
                else
                {
                    ReviewStatus.IsPlay = false;
                }

            }
        }

        protected override void UpdateCrossSectionImage()
        {
            if (DrawCrossSectionImage())
            {
                DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Review];
                if (!IndicatorLongitude.IsCaptured)
                    updateNavigator(imageInfo.Current, imageInfo.Total);

                FrameNumber = imageInfo.Current;
            }
        }

        private void MoveIndicator(object param)
        {
            Indicator indicator = (Indicator)param;

            if (indicator.IsCaptured)
            {
                if (indicator.IsLongitudeClicked)
                {
                    StopPlayback();

                    indicator.IsLongitudeClicked = false;
                    return;
                }

                if (indicator.IsLongitudeMove)
                {
                    indicator.IndicatorDiff = indicator.PointLongitudeX - indicator.Coordinate.X - indicator.X;
                    indicator.IsLongitudeMove = false;
                }

                double indicatorX = indicator.PointLongitudeX - indicator.Coordinate.X - indicator.IndicatorDiff;
                double indicatorCenterX = indicatorX + Constants.LongitudeIndicatorWidth / 2;

                if (indicatorCenterX < 0)
                {
                    indicator.X = 0 - Constants.LongitudeIndicatorWidth / 2;
                    indicator.CenterX = 0;
                    setCurrentFrame(0);
                }
                else if (indicatorCenterX > Constants.LongitudeWidth)
                {
                    indicator.X = Constants.LongitudeWidth - Constants.LongitudeIndicatorWidth / 2;
                    indicator.CenterX = Constants.LongitudeWidth;
                    setCurrentFrame(Constants.LongitudeWidth);
                }
                else
                {
                    indicator.X = indicatorX;
                    indicator.CenterX = indicatorCenterX;
                    setCurrentFrame(indicatorCenterX);
                }
            }
        }

        private void TouchMoveIndicator(object param)
        {
            StopPlayback();

            MouseEventArgs e = (MouseEventArgs)param;
            var position = e.GetPosition((IInputElement)e.Source);

            IndicatorLongitude.X = position.X - Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.CenterX = IndicatorLongitude.X + Constants.LongitudeIndicatorWidth / 2;
            setCurrentFrame(IndicatorLongitude.CenterX);
        }

        protected void updateNavigator(int curFrame, int totalFrame)
        {
            if (FrameNumber == curFrame)
                return;

            double curPosition = (double)curFrame / (totalFrame - 1);
            curPosition *= Constants.LongitudeWidth;
            IndicatorLongitude.X = curPosition - Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.CenterX = curPosition;
        }

        private void setCurrentFrame(double navigatorPosition)
        {
            double curPosition = navigatorPosition / Constants.LongitudeWidth;

            if (longitudeFrameInfo != null)
            {
                curPosition *= (longitudeFrameInfo.totalFrame - 1);
                curPosition = Math.Round(curPosition);
                MoveToFrame(RaySession.Review, (int)curPosition);
            }
        }

        protected void LoadImageFromRaw(string path, double imageResolution, double zOffset, string colormap, int brightness, int contrast)
        {
            RayError result = (RayError)RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }
            SetCrossSectionBackground(RaySession.Review, Constants.CardBackgroundColor);
            int numOfFrames = RayStartReview(path, imageResolution, zOffset);

            if (numOfFrames < (int)RayError.OK)
            {
                // To-Do: Error
                _log.Error("numOfFrames :" + numOfFrames + " < (int)RayError.OK");
                _log.Error("Image Path : " + path);

                return;
            }
            else
            {
                // Wait for Review to start
                for (int i = 0; i < 100; i++)
                {
                    if ((RayScannerState)RayGetProperty(Property.CurrentState) == RayScannerState.Review)
                        break;
                    Thread.Sleep(5);
                }
            }
            Thread.Sleep(100);

            CommonUtil.SetColormap(colormap);

            result = (RayError)RaySetProperty(Property.Brightness, brightness);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }
            result = (RayError)RaySetProperty(Property.Contrast, contrast);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }

            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total = 0;
            DeviceStatus.IsOCTImagingDone = false;

            GetImageInfo(RaySession.Review);

            IndicatorLongitude.IsVisible = Visibility.Visible;
        }

        protected async Task<int> CountFramesAsync(IImageService service, string path)
        {
            _cts = new CancellationTokenSource();
            DeviceStatus.IsOCTImagingDone = false;

            try
            {
                DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
                DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total = await service.CountFramesAsync(path, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                _log.Debug("OperationCanceledException");
            }
            finally
            {
                DeviceStatus.IsOCTImagingDone = true;
                _cts?.Cancel();
                _cts?.Dispose();
            }

            return DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;
        }

        protected async Task StreamEnumerableAsync(IImageService service, string path)
        {
            _cts = new CancellationTokenSource();
            CrossSectionImages = new ObservableCollection<Mat>();
            DeviceStatus.IsOCTImagingDone = false;

            try
            {
                int numOfFrames = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;

                await foreach (var (idx, mat) in service.StreamEnumerableAsync(path, _cts.Token))
                {
                    var cloned = mat.Clone();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CrossSectionImages.Add(cloned);
                        imgLongitude = BuildLongitude(CrossSectionImages, numOfFrames, 0);
                        longitudeFrameInfo = new FrameInfo((CrossSectionImages.Count << 16) | numOfFrames);
                        DrawLongitudeImage();
                        IndicatorLongitude.IsVisible = Visibility.Visible;
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (OperationCanceledException)
            {
                _log.Debug("OperationCanceledException");
            }
            finally
            {
                DeviceStatus.IsOCTImagingDone = true;
                _cts?.Cancel();
                _cts?.Dispose();
            }
        }

        protected void InitializeImportData(Dictionary<string, Object> data)
        {
            if (data.TryGetValue("reviewStatus", out var reviewStatusObj) && reviewStatusObj is ReviewStatus reviewStatusData)
            {
                ReviewStatus = reviewStatusData;
            }

            if (Constants.ImportTypeTiff.Equals(FileImport.PatientCase.ImportType) || Constants.ImportTypeDicom.Equals(FileImport.PatientCase.ImportType))
            {
                if (data.TryGetValue("crossSectionImages", out var crossSectionImagesObj) && crossSectionImagesObj is ObservableCollection<Mat> crossSectionImagestData)
                {
                    CrossSectionImages = crossSectionImagestData;
                }
                if (data.TryGetValue("longitudeImage", out var longitudeImageObj) && longitudeImageObj is BitmapSource longitudeImageData)
                {
                    LongitudeImage = longitudeImageData;
                }
                if (data.TryGetValue("longitudeFrameInfo", out var longitudeFrameInfoObj) && longitudeFrameInfoObj is FrameInfo longitudeFrameInfoData)
                {
                    longitudeFrameInfo = longitudeFrameInfoData;
                }
            }
            else if (Constants.ImportTypeRaw.Equals(FileImport.PatientCase.ImportType))
            {
                RayError result = (RayError)RaySetProperty(Property.Brightness, FileImport.PatientCase.Brightness);
                if (result != RayError.OK)
                {
                    _log.Error("RaySetProperty Error");
                }
            }

            DeviceStatus.ReviewImageInfo imageInfo = DeviceStatus.ReviewImageInfos[(int)RaySession.Review];
            MoveToFrame(RaySession.Review, imageInfo.Current);
            updateNavigator(imageInfo.Current, imageInfo.Total);
            IndicatorLongitude.IsVisible = Visibility.Visible;
            IndicatorLongitude.IsEnabled = true;

            if (ReviewStatus.IsPlay)
            {
                Playback();
            }
        }

        protected void MoveImportPage(string page)
        {
            Dictionary<string, Object> parameter = new Dictionary<string, Object>();
            parameter["reviewStatus"] = ReviewStatus;
            parameter["initializeImport"] = true;
            parameter["fileImport"] = FileImport;
            if (Constants.ImportTypeTiff.Equals(FileImport.PatientCase.ImportType) || Constants.ImportTypeDicom.Equals(FileImport.PatientCase.ImportType))
            {
                parameter["crossSectionImages"] = CrossSectionImages;
                parameter["longitudeImage"] = LongitudeImage;
                parameter["longitudeFrameInfo"] = longitudeFrameInfo;
            }
            WeakReferenceMessenger.Default.Send(new NavigationMessage(page) { Parameter = parameter });
        }
    }
}
