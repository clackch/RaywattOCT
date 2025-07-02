using RaywattApp.Common.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using RaywattApp.Views.Dialog;
using RaywattApp.Common.Bases;
using RayCoreWrapper;
using RaywattApp.Common.Util;
using System.Runtime.InteropServices;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class MwlSearchDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MwlSearchDialogViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService? _dialogService;

        [ObservableProperty]
        private string _localHostAeTitle;

        [ObservableProperty]
        private DicomServer _dicomServer = new DicomServer();

        [ObservableProperty]
        private bool _isChecking = false;

        [ObservableProperty]
        private string _patientName;

        [ObservableProperty]
        private string _patientId;

        [ObservableProperty]
        private string _procedureId;

        [ObservableProperty]
        private string _accessionNumber;

        [ObservableProperty]
        private string _scheduledStationAe;

        [ObservableProperty]
        private DateTime _spsStartDateFrom;

        [ObservableProperty]
        private DateTime _spsStartDateTo = DateTime.MaxValue;

        [ObservableProperty]
        private bool _useSpsStartDate = false;

        private IntPtr dicomClient;
        private IntPtr dicomWorklists;

        private DeviceStatus deviceStatus;

        List<DicomWorklist> worklists = new List<DicomWorklist>();

        public MwlSearchDialogViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;

            dicomClient = RayExportWrapper.CreateDcmClient();
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            LocalHostAeTitle = (string)data["localHostAeTitle"];
            DicomServer = (DicomServer)data["dicomServer"];
            deviceStatus = (DeviceStatus)data["deviceStatus"];
        }

        protected override async void AnswerYes(IDialogWindow dialog)
        {
            _log.Debug("AnswerYes");

            Validate();

            await Search();

            RayExportWrapper.DestroyDcmClient(dicomClient);

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = worklists;
            CloseDialogWithResult(dialog, dialogResults);
        }

        private async Task Search()
        {
            IsChecking = true;
            RayExportWrapper.DicomNetRWError res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Echo(dicomClient));
            _log.DebugFormat("Echo : {0}", res);
            IsChecking = false;

            if (res == RayExportWrapper.DicomNetRWError.NoConnection || res == RayExportWrapper.DicomNetRWError.EchoFail)
            {
                IsChecking = true;
                bool usePeerVerification = CommonUtil.IsTestMode(deviceStatus.TestMode, "CertIgnore") == true ? false : true;
                res = await Task.Run(() => (RayExportWrapper.DicomNetRWError)RayExportWrapper.Initialize(dicomClient, LocalHostAeTitle, DicomServer.IpAddress, int.Parse(DicomServer.Port), DicomServer.AeTitle, DicomServer.TlsYn, usePeerVerification, DicomServer.CaFilePath));
                _log.DebugFormat("Initialize : {0}", res);
                IsChecking = false;

                if (res != RayExportWrapper.DicomNetRWError.Normal)
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = CommonUtil.GetDicomResultMessage(res);
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                    return;
                }
            }

            int count = 0;
            IsChecking = true;
            dicomWorklists = await Task.Run(() => RayExportWrapper.FindWorklist(dicomClient, PatientId, PatientName, AccessionNumber, "*" /* Modality */, ScheduledStationAe, SpsStartDateFrom.ToString("yyyyMMdd"), SpsStartDateTo.ToString("yyyyMMdd"), ProcedureId, out count));
            IsChecking = false;

            if (dicomWorklists != IntPtr.Zero)
            {
                IntPtr current = dicomWorklists;

                for (int i = 0; i < count; i++)
                {
                    var dicomWorklist = Marshal.PtrToStructure<DicomWorklist>(current);
                    worklists.Add(dicomWorklist);

                    current += Marshal.SizeOf<DicomWorklist>();
                }
            }
            RayExportWrapper.FreeMemory(dicomWorklists);
        }

        private bool Validate()
        {
            PatientName = string.IsNullOrWhiteSpace(PatientName) ? "*" : PatientName.Trim();
            PatientId = string.IsNullOrWhiteSpace(PatientId) ? "*" : PatientId.Trim();
            ProcedureId = string.IsNullOrWhiteSpace(ProcedureId) ? "*" : ProcedureId.Trim();
            AccessionNumber = string.IsNullOrWhiteSpace(AccessionNumber) ? "*" : AccessionNumber.Trim();
            ScheduledStationAe = string.IsNullOrWhiteSpace(ScheduledStationAe) ? "*" : ScheduledStationAe.Trim();

            // TODO[haeun]: SpsStartDate 시작, 끝 날짜의 대소 확인이 필요한지 결정

            return true;
        }


        partial void OnUseSpsStartDateChanged(bool value)
        {
            if (value) // isChecked
            {
                SpsStartDateTo = DateTime.Now;
                SpsStartDateFrom = DateTime.Now;
            }
            else
            {
                SpsStartDateFrom = DateTime.MinValue;
                SpsStartDateTo = DateTime.MaxValue;
            }
        }
    }
}
