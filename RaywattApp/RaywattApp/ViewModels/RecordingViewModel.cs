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
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return this._saveCommand ?? (this._saveCommand = new RelayCommand(Save)); }
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
                Patient = (Patient)extraData;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = Patient});
        }

        private void Save()
        {
            _log.Debug("Save");

            PatientCase.Id = Patient.Id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            PatientCase.PatientId = Patient.Id;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["patient_id"] = PatientCase.PatientId;
            sqlParameters["physician_id"] = "";
            sqlParameters["accession_number"] = "";
            sqlParameters["accession_name"] = "";
            sqlParameters["comment"] = "comment";
            sqlParameters["vessel"] = "V001";
            sqlParameters["procedure"] = "P001";
            sqlParameters["thumbnail_no"] = 1;
            sqlParameters["still_image_yn"] = "N";

            int nRows = _sqlManager.InsertPatientCase(sqlParameters);

            if (nRows == 1)
            {
                WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = Patient });
            }
            else
            {
                _log.Error("Insert Error");
            }

        }
    }
}
