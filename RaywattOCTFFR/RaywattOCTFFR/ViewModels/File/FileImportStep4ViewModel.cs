using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using System.Collections.Generic;
using System;
using System.Windows;
using static RaywattOCT.RayCoreFFRWrapper;
using System.Windows.Navigation;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using RaywattOCTFFR.Common.Util;

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep4ViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep4ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        private int zOffset;

        private ICommand _resetCommand;
        public ICommand ResetCommand
        {
            get { return this._resetCommand ?? (this._resetCommand = new RelayCommand(Reset)); }
        }

        private ICommand _cmdManualZoomIn;
        public ICommand CmdManualZoomIn
        {
            get { return _cmdManualZoomIn ?? (this._cmdManualZoomIn = new RelayCommand<bool>(ManualZoomIn)); }
        }

        public FileImportStep4ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportStep4ViewModel");

            Constants.CurrentPage = Constants.FileImportStep4Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            IndicatorLongitude = new Indicator();
            IndicatorLongitude.X = Constants.LongitudeIndicatorWidth / 2;
            IndicatorLongitude.IsVisible = Visibility.Collapsed;
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
                    PatientCase = FileImport.PatientCase;

                    InitializeImportData(data);

                    this.zOffset = PatientCase.ZOffset;
                    Constants.ZOffsetScale = CommonUtil.GetZOffsetScale(PatientCase.ZOffset);

                    DrawSheathIndicator(PatientCase.SheathDiameter);

                    CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault) * Constants.ZOffsetScale;
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
                }
            }
        }

        protected override void Back()
        {
            _log.Debug("Back");

            this.isEndReview = false;

            MoveImportPage(Constants.FileImportStep3Page);
        }

        protected override void Next()
        {
            _log.Debug("Next");

            this.isEndReview = false;

            PatientCase.ZOffset = this.zOffset;
            Constants.ZOffsetScale = CommonUtil.GetZOffsetScale(PatientCase.ZOffset);

            MoveImportPage(Constants.FileImportStep5Page);
        }

        private void ManualZoomIn(bool zoomIn)
        {
            _log.Debug("ManualZoomIn : " + ((zoomIn) ? "IN" : "OUT"));

            int sign = zoomIn ? 1 : -1;

            this.zOffset += sign;

            double sheathDiameter = PatientCase.SheathDiameter * CommonUtil.GetZOffsetScale(this.zOffset);

            if(sheathDiameter > 0.5 && sheathDiameter < 3)
            {                
                DrawSheathIndicator(sheathDiameter, false);
                CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault) * CommonUtil.GetZOffsetScale(this.zOffset);
            }
            else
            {
                this.zOffset += (sign * -1);
            }
        }

        private void Reset()
        {
            _log.Debug("Reset");

            this.zOffset = 0;

            DrawSheathIndicator(PatientCase.SheathDiameter, false);
            CrossSectionScale = (1 / Constants.ImageResolution) * (Constants.ZoomScaleDefault);
        }
    }
}
