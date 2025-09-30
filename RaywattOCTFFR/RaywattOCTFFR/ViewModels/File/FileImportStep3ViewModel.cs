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

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep3ViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep2ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private FileImport _fileImport = new FileImport();

        public FileImportStep3ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportStep3ViewModel");

            Constants.CurrentPage = Constants.FileImportStep3Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            ReviewStatus = new ReviewStatus();

            CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault);

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Visible;
            IndicatorLongitude.IsEnabled = true;
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

                    LoadImageFromRaw();

                    GetImageInfo(RaySession.Review);

                    FileImport.PatientCase.NumOfFrames = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;

                    Playback();
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            base.OnNavigating(sender, navigationEventArgs);

            RayEndReview();
            DeviceStatus.IsOCTImagingDone = true;
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
            CommonUtil.SetColormap(FileImport.PatientCase.Colormap);
            int numOfFrames = RayStartReview(Constants.TempPath + "\\" + FileImport.PatientCase.Image, 0.01, FileImport.PatientCase.ZOffset);

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
        }
    }
}
