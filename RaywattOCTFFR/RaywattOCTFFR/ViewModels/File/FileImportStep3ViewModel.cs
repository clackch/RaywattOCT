using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Windows.Navigation;
using static RaywattOCT.RayCoreWrapper;
using CommunityToolkit.Mvvm.Messaging;
using RaywattOCTFFR.Common.Messages;
using System.Windows;
using OpenCvSharp;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep3ViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep2ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private TiffService _tiffService;

        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private FileImport _fileImport = new FileImport();

        public FileImportStep3ViewModel(SqlManager sqlManager, IDialogService dialogService, TiffService tiffService)
        {
            _log.Debug("FileImportStep3ViewModel");

            Constants.CurrentPage = Constants.FileImportStep3Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _tiffService = tiffService;

            ReviewStatus = new ReviewStatus();

            CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault);

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Collapsed;
            IndicatorLongitude.IsEnabled = false;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
            base.OnNavigated(sender, navigatedEventArgs);

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                if (data.TryGetValue("fileImport", out var fileImportObj) && fileImportObj is FileImport fileImportData)
                {
                    FileImport = fileImportData;

                    if (Constants.ImportTypeRaw.Equals(FileImport.PatientCase.ImportType))
                    {
                        LoadImageFromRaw();                        
                    }
                    else if (Constants.ImportTypeTiff.Equals(FileImport.PatientCase.ImportType))
                    {
                        _ = LoadFromPathAsync();
                    }

                    Playback();
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            base.OnNavigating(sender, navigationEventArgs);

            if (Constants.ImportTypeRaw.Equals(FileImport.PatientCase.ImportType))
            {
                RayEndReview();
                DeviceStatus.IsOCTImagingDone = true;
            }
            else if (Constants.ImportTypeTiff.Equals(FileImport.PatientCase.ImportType))
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                    _cts?.Cancel();
            }
        }

        protected override void Back()
        {
            _log.Debug("Back");

            Dictionary<string, Object> parameter = new Dictionary<string, Object>();
            parameter["fileImport"] = FileImport;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.FileImportStep2Page) { Parameter = parameter });
        }

        protected override void Next()
        {
            _log.Debug("Next");
        }

        private void LoadImageFromRaw()
        {
            RayError result = (RayError)RaySetProperty(Property.LongitudeBackgroundColor, Constants.CardBackgroundColor);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }
            SetCrossSectionBackground(RaySession.Review, Constants.CardBackgroundColor);
            CommonUtil.SetColormap(FileImport.PatientCase.Colormap);
            int numOfFrames = RayStartReview(Constants.TempPath + "\\" + FileImport.PatientCase.Image, FileImport.PatientCase.ImageResolution, FileImport.PatientCase.ZOffset);

            if (numOfFrames < (int)RayError.OK)
            {
                // To-Do: Error
                _log.Error("numOfFrames :" + numOfFrames + " < (int)RayError.OK");
                _log.Error("Image Path : " + Constants.TempPath + "\\" + FileImport.PatientCase.Image);

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

            result = (RayError)RaySetProperty(Property.Brightness, FileImport.PatientCase.Brightness);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }
            result = (RayError)RaySetProperty(Property.Contrast, FileImport.PatientCase.Contrast);
            if (result != RayError.OK)
            {
                _log.Error("RaySetProperty Error");
            }

            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
            DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total = 0;
            DeviceStatus.IsOCTImagingDone = false;

            GetImageInfo(RaySession.Review);

            IndicatorLongitude.IsVisible = Visibility.Visible;

            FileImport.PatientCase.NumOfFrames = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;
        }        

        private async Task LoadFromPathAsync()
        {
            _cts = new CancellationTokenSource();
            CrossSectionImages = new ObservableCollection<Mat>();
            string path = Constants.TempPath + "\\" + FileImport.PatientCase.Image;
            DeviceStatus.IsOCTImagingDone = false;

            try
            {
                DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Current = 0;
                DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total = await _tiffService.CountFramesAsync(path, _cts.Token);
                FileImport.PatientCase.NumOfFrames = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;

                await foreach (var (idx, mat) in _tiffService.StreamEnumerableAsync(path, _cts.Token))
                {
                    var cloned = mat.Clone();
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CrossSectionImages.Add(cloned);
                        imgLongitude = BuildLongitude(CrossSectionImages, FileImport.PatientCase.NumOfFrames, 0);
                        longitudeFrameInfo = new FrameInfo((CrossSectionImages.Count << 16) | FileImport.PatientCase.NumOfFrames);
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

        protected override void UpdateCrossSectionImage()
        {
            base.UpdateCrossSectionImage();

            if (longitudeFrameInfo != null && (longitudeFrameInfo.curFrame == longitudeFrameInfo.totalFrame))
            {
                IndicatorLongitude.IsEnabled = true;
            }
        }

    }
}
