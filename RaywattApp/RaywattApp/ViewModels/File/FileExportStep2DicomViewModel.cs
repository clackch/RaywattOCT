using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.File;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System.Collections.Generic;
using System;
using System.Windows.Navigation;
using RaywattApp.Common.Util;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Linq;
using RayCoreWrapper;
using System.Threading.Tasks;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep2DicomViewModel : FileExportStep2Base
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep2DicomViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private int _maxFrameWidth;

        [ObservableProperty]
        private int _minFrameWidth;

        [ObservableProperty]
        private int _frameTickFrequency;

        [ObservableProperty]
        private bool _isPacs;

        [ObservableProperty]
        private DicomServer _selectedDicomServer;

        [ObservableProperty]
        private string _localHostAeTitle;

        [ObservableProperty]
        private bool _isChecking = false;

        private IntPtr dicomClient;

        private ICommand _selectPacsCommand;
        public ICommand SelectPacsCommand
        {
            get { return this._selectPacsCommand ?? (this._selectPacsCommand = new RelayCommand(SelectPacs)); }
        }

        public FileExportStep2DicomViewModel(SqlManager sqlManager, IDialogService dialogService) : base(dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "LocalHost";
            IList<Configuration> localHost = _sqlManager.SelectConfiguration(sqlParameters);
            if (localHost != null && localHost.Count > 0)
            {
                LocalHostAeTitle = localHost.FirstOrDefault(x => x.Key == "AeTitle").Value;
            }

            dicomClient = RayExportWrapper.CreateDcmClient();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                FileExport = (FileExport)extraData;
                SetCondition();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);

            RayExportWrapper.DestroyDcmClient(dicomClient);
            dicomClient = IntPtr.Zero;
        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            if (FileExport.ExternalDrivePath == null)
                FileExport.ExternalDrivePath = "";

            GetExportSize();
            GetDrive();
        }

        private void GetExportSize()
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["ids"] = FileExport.SelectedItem;
            PatientCases = _sqlManager.SelectPatientCaseByList(sqlParameters);

            const double frameSize = Constants.ApplicationWidth * Constants.ApplicationHeight * 3.0;

            if (FileExport.Material == Constants.ExportMaterialPullback)
            {
                foreach (PatientCase patientCase in PatientCases)
                {
                    ExportSize += frameSize * patientCase.NumOfFrames;
                }
            }
            else if(FileExport.Material == Constants.ExportMaterialBookmarked)
            {
                ExportSize = frameSize * FileExport.BookmarkedFrames.Count;
            }
            else
            {
                ExportSize = frameSize;
            }

            UpdateFileSize(ExportSize);
        }

        protected override void FileSave()
        {
            if (FileExport.PatientInfoAnonymize)
            {
                foreach (PatientCase patientCase in PatientCases)
                {
                    patientCase.ImageFullPath = patientCase.ImageFullPath;
                    patientCase.IsAnonymize = true;

                    string alternateId = CommonUtil.GetRandomText(9);
                    patientCase.Id = alternateId + "_" + patientCase.Id.Split("_")[1];
                    patientCase.PatientId = alternateId;
                    patientCase.PatientName = Constants.ExportAnonymous;
                    patientCase.Birthdate = new DateTime(1900, 1, 1);                    
                }
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["File Export"];
            parameter["fileExport"] = FileExport;
            parameter["patientCases"] = PatientCases;
            parameter["dicomProperty"] = GetDicomProperty();
            parameter["isPacs"] = IsPacs;
            if (IsPacs)
            {
                parameter["localHostAeTitle"] = LocalHostAeTitle;
                parameter["selectedDicomServer"] = SelectedDicomServer;
                parameter["usePeerVerification"] = CommonUtil.IsTestMode(DeviceStatus.TestMode, "CertIgnore") == true ? false : true;
            }
                
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);

            RayExportWrapper.DestroyDcmClient(dicomClient);
            dicomClient = IntPtr.Zero;

            Close();
        }

        private Dictionary<string, string> GetDicomProperty()
        {
            IList<StringModel> dicomPropertyList = _sqlManager.SelectDicomPropertyList();

            Dictionary<string, string> dicomProperty = new Dictionary<string, string>();

            foreach(StringModel temp in dicomPropertyList)
            {
                dicomProperty.Add(temp.ReturnString, temp.ReturnString2);
            }

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "Terms&Cond";
            IList<Configuration> tnCs = _sqlManager.SelectConfiguration(sqlParameters);
            if (tnCs != null || tnCs.Count == 1)
            {
                //Institution Name
                dicomProperty.Add("00080080", tnCs[0].Buffer);
            }

            return dicomProperty;
        }

        protected override async void Export()
        {
            if (IsPacs)
            {
                //Validate
                if (SelectedDicomServer == null)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Select PACS server"];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    return;
                }                    

                //Connection Test
                bool res = await ConnectionTest();
                if (!res)
                    return;

                //Dicom 파일 생성 및 로컬 저장
                FileSave();
            }
            else
            {
                base.Export();
            }
        }

        private async Task<bool> ConnectionTest()
        {
            IsChecking = true;
            RayExportWrapper.DicomNetRWError res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Echo(dicomClient));
            _log.DebugFormat("Echo : {0}", res);
            IsChecking = false;

            if (res == RayExportWrapper.DicomNetRWError.NoConnection || res == RayExportWrapper.DicomNetRWError.EchoFail)
            {
                IsChecking = true;
                bool usePeerVerification = CommonUtil.IsTestMode(DeviceStatus.TestMode, "CertIgnore") == true ? false : true;
                res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Initialize(dicomClient, LocalHostAeTitle, SelectedDicomServer.IpAddress, int.Parse(SelectedDicomServer.Port), SelectedDicomServer.AeTitle, SelectedDicomServer.TlsYn, usePeerVerification, SelectedDicomServer.CaFilePath));
                _log.DebugFormat("Initialize : {0}", res);
                IsChecking = false;

                if (res != RayExportWrapper.DicomNetRWError.Normal)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = CommonUtil.GetDicomResultMessage(res);
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    return false;
                }
            }

            return true;
        }

        private void SelectPacs()
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedDicomServerId"] = SelectedDicomServer == null ? 0 : SelectedDicomServer.Id;

            var result = _dialogService.OpenDialog(new DicomPacsDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                DicomServer dicomServer = (DicomServer)data["selectedDicomServer"];
                SelectedDicomServer = dicomServer;
                
            }
        }
    }
}
