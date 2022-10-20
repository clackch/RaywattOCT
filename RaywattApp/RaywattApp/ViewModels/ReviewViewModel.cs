using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RaywattApp.ViewModels
{
    public partial class ReviewViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private Dictionary<string, string> _vesselComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private Dictionary<string, string> _procedureComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private Dictionary<string, string> _physicianComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private string _currentVessel;

        private string previousVesselKey;

        private string vesselOther;

        private bool vesselOpened;

        [ObservableProperty]
        private string _currentProcedure;

        private string previousProcedureKey;

        private string procedureOther;

        private bool proceduereOpened;

        [ObservableProperty]
        private string _currentPhysician;

        private ICommand _endReviewCommand;
        public ICommand EndReviewCommand
        {
            get { return this._endReviewCommand ?? (this._endReviewCommand = new RelayCommand(EndReview)); }
        }

        private ICommand _editCaseCommand;
        public ICommand EditCaseCommand
        {
            get { return this._editCaseCommand ?? (this._editCaseCommand = new RelayCommand(EditCase)); }
        }

        private ICommand _vesselChangedCommand;
        public ICommand VesselChangedCommand
        {
            get { return this._vesselChangedCommand ?? (this._vesselChangedCommand = new RelayCommand<KeyValuePair<string, string>>(ChangedVessel)); }
        }

        private ICommand _vesselOpenedCommand;
        public ICommand VesselOpenedCommand
        {
            get { return this._vesselOpenedCommand ?? (this._vesselOpenedCommand = new RelayCommand<KeyValuePair<string, string>>(OpenVessel)); }
        }

        private ICommand _vesselClosedCommand;
        public ICommand VesselClosedCommand
        {
            get { return this._vesselClosedCommand ?? (this._vesselClosedCommand = new RelayCommand<KeyValuePair<string, string>>(CloseVessel)); }
        }

        public ReviewViewModel(SqlManager sqlManager)
        {
            _log.Debug("ReviewViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.ReviewPage;

            _sqlManager = sqlManager;

            vesselOpened = false;
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

                SetInit();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void SetInit()
        {
            Dictionary<string, string> procedure = CodeDefinition.Codes["PROC"];

            CurrentVessel = CodeDefinition.Codes["VESS"].FirstOrDefault(x => x.Value == PatientCase.Vessel).Key;
            
            if (CurrentVessel == null)
            {
                VesselComboBox = GetVesselList(PatientCase.Vessel);
                CurrentVessel = "$OTH";
                vesselOther = PatientCase.Vessel;
            }
            else
            {
                VesselComboBox = GetVesselList();
            }
            
            previousVesselKey = CurrentVessel;

            foreach (var item in procedure)
            {
                ProcedureComboBox[item.Key] = item.Value;
            }
            CurrentProcedure = CodeDefinition.Codes["PROC"].FirstOrDefault(x => x.Value == PatientCase.Procedure).Key;

            if (CurrentProcedure == null)
            {
                ProcedureComboBox["$OTH"] = PatientCase.Procedure;
                CurrentProcedure = "$OTH";
            }

            previousProcedureKey = CurrentProcedure;
        }

        private void ChangedVessel(KeyValuePair<string, string> selectedVessel)
        {
            _log.Debug("ChangedVessel");

            if (vesselOpened)
                return;

            if (selectedVessel.Key == "$OTH")
            {
                WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "EditVessProcPopupControl", Type = (int)CommonDefinition.PopupType.Edit, PopupId = (int)CommonDefinition.EditList.Vessel, ParentObject = this, Parameter = vesselOther });
            }
            else if(previousVesselKey == "$OTH")
            {
                VesselComboBox = GetVesselList();
                previousVesselKey = selectedVessel.Key;
            }
            else
            {
                previousVesselKey = selectedVessel.Key;
            }
        }

        private void OpenVessel(KeyValuePair<string, string> selectedVessel)
        {
            _log.Debug("OpenVessel");

            if (selectedVessel.Key == "$OTH")
            {
                VesselComboBox = GetVesselList();
                vesselOpened = true;
                CurrentVessel = "$OTH";
                vesselOpened = false;
            }
        }

        private void CloseVessel(KeyValuePair<string, string> selectedVessel)
        {
            _log.Debug("CloseVessel");

            if (selectedVessel.Key == "$OTH")
            {
                VesselComboBox = GetVesselList(vesselOther);
                vesselOpened = true;
                CurrentVessel = "$OTH";
                vesselOpened = false;
            }
        }

        public Dictionary<string, string> GetVesselList(string? other = null)
        {
            Dictionary<string, string> vessel = CodeDefinition.Codes["VESS"];
            Dictionary<string, string> vesselComboBox = new Dictionary<string, string>();
            foreach (var item in vessel)
            {
                vesselComboBox[item.Key] = item.Value;
            }

            if(other != null)
            {
                vesselComboBox["$OTH"] = other;
            }

            return vesselComboBox;
        }

        public override void CallbackPopup()
        {
            _log.Debug("CallbackPopup : " + PopupCallback.PopupId + "/" + PopupCallback.PopupAnswer);

            if (PopupCallback != null)
            {
                if (PopupCallback.PopupId == (int)CommonDefinition.EditList.Vessel)
                {
                    if (PopupCallback.PopupAnswer)
                    {
                        vesselOther = PopupCallback.PopupParameter.ToString();

                        if(vesselOther != "")
                        {
                            VesselComboBox = GetVesselList(vesselOther);
                            CurrentVessel = "$OTH";
                            previousVesselKey = CurrentVessel;
                        }
                    }
                    else
                    {
                        VesselComboBox = GetVesselList();
                        CurrentVessel = previousVesselKey;
                    }
                        
                }
                else if(PopupCallback.PopupId == (int)CommonDefinition.EditList.Procedure)
                {

                }
                else if(PopupCallback.PopupId == (int)CommonDefinition.EditList.Case)
                {
                    if (PopupCallback.PopupAnswer)
                    {
                        Dictionary<string, Object> data = (Dictionary<string, Object>)PopupCallback.PopupParameter;
                        PatientCase.PhysicianName = data["physicianName"].ToString();
                        PatientCase.AccessionNumber = data["accessionNumber"].ToString();
                        PatientCase.Comment = data["comment"].ToString();
                    }
                }
            }
        }

        private void EndReview()
        {
            _log.Debug("EndReview");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["comment"] = PatientCase.Comment;

            if(CurrentVessel == "$OTH")
            {
                sqlParameters["vessel"] = vesselOther;
            }
            else
            {
                sqlParameters["vessel"] = CurrentVessel;
            }
            
            sqlParameters["procedure"] = CurrentProcedure;

            int nRows = _sqlManager.UpdatePatientCase(sqlParameters);

            if (nRows == 1)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
            }
        }

        private void EditCase()
        {
            _log.Debug("EditCase");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["physicianName"] = PatientCase.PhysicianName;
            parameter["accessionNumber"] = PatientCase.AccessionNumber;
            parameter["comment"] = PatientCase.Comment;

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "EditCasePopupControl", Type = (int)CommonDefinition.PopupType.Edit, PopupId = (int)CommonDefinition.EditList.Case, ParentObject = this, Parameter = parameter });
        }
    }
}
