using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Services;
using System.Windows.Navigation;
using System;
using System.Collections.Generic;

namespace RaywattApp.ViewModels
{
    public partial class RecordingViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand
        {
            get { return this._confirmCommand ?? (this._confirmCommand = new RelayCommand(Confirm)); }
        }

        public RecordingViewModel(SqlManager sqlManager)
        {
            _log.Debug("RecordingViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.RecordingPage;

            _sqlManager = sqlManager;

            PatientCase = new PatientCase();
        }
        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PrevStatus = (PrevStatus)data["prevStatus"];
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
        }

        private void Confirm()
        {
            _log.Debug("Confirm");

            PatientCase.PhysicianName = Constants.NotSelected;
            PatientCase.AccessionNumber = "";
            PatientCase.AccessionName = "";
            PatientCase.Comment = "";
            PatientCase.Vessel = "$000";
            PatientCase.Procedure = "$000";
            PatientCase.ThumbnailNo = 1;
            PatientCase.StillImageYn = "N";
            PatientCase.Image = "";
            PatientCase.PullbackType = "LONG";
            PatientCase.Brightness = 30;
            PatientCase.Contrast = 10;
            PatientCase.AngioCoRegistration = false;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            SetDetailStatusInit();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPresetPage.xaml") { Parameter = parameter });
        }

        private void SetDetailStatusInit()
        {
            PrevStatus.DetailSelectedGroup = null;
            PrevStatus.DetailPageOffset = 0;
            PrevStatus.DetailPageGroup = 1;
            PrevStatus.DetailPageNumber = 0;
        }
    }
}
