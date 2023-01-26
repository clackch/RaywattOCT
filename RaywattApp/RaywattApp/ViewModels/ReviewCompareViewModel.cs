using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class ReviewCompareViewModel : ReviewViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewCompareViewModel));

        [ObservableProperty]
        private IList<PatientCase> _patientCases;

        [ObservableProperty]
        private PatientCase _displayPatientCase;

        private ICommand _caseSelectCancelCommand;
        public ICommand CaseSelectCancelCommand
        {
            get { return this._caseSelectCancelCommand ?? (this._caseSelectCancelCommand = new RelayCommand(CaseSelectCancel)); }
        }

        private ICommand _caseSelectOkCommand;
        public ICommand CaseSelectOkCommand
        {
            get { return this._caseSelectOkCommand ?? (this._caseSelectOkCommand = new RelayCommand<PatientCase>(CaseSelectOk)); }
        }

        public ReviewCompareViewModel(SqlManager sqlManager, IDialogService dialogService) : base(sqlManager, dialogService)
        {
            _log.Debug("ReviewCompareViewModel");

            Constants.CurrentPage = Constants.ReviewComparePage;

            ExpandLeftUpMenu = false;
            ExpandLeftDownMenu = false;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            base.OnNavigated(sender, navigatedEventArgs);
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                Patient = (Patient)data["patient"];
                PatientCase = (PatientCase)data["patientCase"];
                PrevStatus = (PrevStatus)data["prevStatus"];
                ReviewStatus = (ReviewStatus)data["reviewStatus"];
                ReviewStatus.CurrentPage = Constants.ReviewComparePage;

                if (ReviewStatus.SelectedPatientCase == null)
                    GetPatientCase(true);
                else
                    GetPatientCase(false);
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
            _log.Debug("OnNavigating");
            Save();
        }

        protected override void Save()
        {
            _log.Debug("Save");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["comment"] = PatientCase.Comment;
            sqlParameters["vessel"] = PatientCase.Vessel;
            sqlParameters["procedure"] = PatientCase.Procedure;
            sqlParameters["angio_co_registration"] = PatientCase.AngioCoRegistration;
            sqlParameters["preset_name"] = PatientCase.PresetName;
            sqlParameters["calcium_threshold"] = PatientCase.CalciumThreshold;
            sqlParameters["expansion_calculation"] = PatientCase.ExpansionCalculation;
            sqlParameters["expansion_threshold"] = PatientCase.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = PatientCase.AppositionThreshold;
            sqlParameters["measurements"] = PatientCase.Measurements;
            sqlParameters["bookmarks"] = PatientCase.Bookmarks;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);
            if (nRows == 0)
                _log.Error("Update Error");
        }

        private void CaseSelectCancel()
        {
            _log.Debug("CaseSelectCancel");

            DisplayPatientCase = ReviewStatus.SelectedPatientCase;
            ExpandLeftUpMenu = false;
        }

        private void CaseSelectOk(PatientCase patientCase)
        {
            _log.Debug("CaseSelectOk");

            ReviewStatus.SelectedPatientCase = patientCase;
            DisplayPatientCase = patientCase;
            ExpandLeftUpMenu = false;
        }

        private void GetPatientCase(bool isNullPatientCase)
        {
            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = Patient.Id;
            sqlParameters["procedure"] = "$001";//Pre-PCI

            PatientCases = _sqlManager.SelectPatientCaseList(sqlParameters);
            if(PatientCases != null && PatientCases.Count > 1)
            {
                if(isNullPatientCase)
                {
                    ReviewStatus.SelectedPatientCase = PatientCases[0];
                }
                else
                {
                    foreach (PatientCase patientCase in PatientCases)
                    {
                        if (patientCase.Id == ReviewStatus.SelectedPatientCase.Id)
                        {
                            ReviewStatus.SelectedPatientCase = patientCase;
                            break;
                        }
                    }
                }

                DisplayPatientCase = ReviewStatus.SelectedPatientCase;
            }
        }
    }
}
