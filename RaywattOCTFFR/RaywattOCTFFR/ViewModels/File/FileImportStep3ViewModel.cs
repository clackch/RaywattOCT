using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System.Collections.Generic;
using System;
using System.Windows.Navigation;
using static RaywattOCT.RayCoreFFRWrapper;
using CommunityToolkit.Mvvm.Messaging;
using RaywattOCTFFR.Common.Messages;
using System.Windows;
using System.Threading.Tasks;

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep3ViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep3ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private TiffService _tiffService;

        private DicomService _dicomService;

        public FileImportStep3ViewModel(SqlManager sqlManager, IDialogService dialogService, TiffService tiffService, DicomService dicomService)
        {
            _log.Debug("FileImportStep3ViewModel");

            Constants.CurrentPage = Constants.FileImportStep3Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
            _tiffService = tiffService;
            _dicomService = dicomService;

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
                    PatientCase = FileImport.PatientCase;

                    if (data.TryGetValue("initializeImport", out var initializeImportObj) && initializeImportObj is bool initializeImportData && initializeImportData)
                    {
                        InitializeImportData(data);                        
                    }
                    else
                    {
                        string path = Constants.TempPath + "\\" + PatientCase.Image;

                        if (Constants.ImportTypeRaw.Equals(PatientCase.ImportType))
                        {
                            LoadImageFromRaw(path, PatientCase.ImageResolution, PatientCase.ZOffset, PatientCase.Colormap, PatientCase.Brightness, PatientCase.Contrast);
                            PatientCase.NumOfFrames = DeviceStatus.ReviewImageInfos[(int)RaySession.Review].Total;
                            PatientCase.SectionDistal = PatientCase.NumOfFrames - 1;
                        }
                        else if (Constants.ImportTypeTiff.Equals(PatientCase.ImportType))
                        {
                            _ = LoadFromImageAsync(_tiffService, path);
                        }
                        else if (Constants.ImportTypeDicom.Equals(PatientCase.ImportType))
                        {
                            _ = LoadFromImageAsync(_dicomService, path);
                        }

                        Playback();
                    }                    
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
            base.OnNavigating(sender, navigationEventArgs);

            if (this.isEndReview)
            {
                if (Constants.ImportTypeRaw.Equals(PatientCase.ImportType))
                {
                    RayEndReview();
                    DeviceStatus.IsOCTImagingDone = true;
                }
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

            this.isEndReview = false;

            MoveImportPage(Constants.FileImportStep4Page);
        }

        private async Task LoadFromImageAsync(IImageService service, string path)
        {
            PatientCase.NumOfFrames = await CountFramesAsync(service, path);
            PatientCase.SectionDistal = PatientCase.NumOfFrames - 1;
            await StreamEnumerableAsync(service, path);
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
