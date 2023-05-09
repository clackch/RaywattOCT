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

        public ReviewAngioCoRegViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewAngioCoRegViewModel");

            Constants.CurrentPage = Constants.ReviewAngioCoRegPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
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
    }
}
