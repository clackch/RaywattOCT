using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Util;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class RecordingPresetViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecordingPresetViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private Physician _physician;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private Dictionary<string, string> _flushMediaList;

        [ObservableProperty]
        private Dictionary<string, string> _pullbackTriggerList;

        [ObservableProperty]
        private Dictionary<string, string> _pullbackTypeList;

        [ObservableProperty]
        private string _selectedFlushMedia;

        private IList<Code> pullbackTypes;

        [ObservableProperty]
        private string _pbLength;

        [ObservableProperty]
        private string _pbSpeed;

        [ObservableProperty]
        private string _pbTime;

        private string _selectedPullbackType;
        public string SelectedPullbackType
        {
            get { return _selectedPullbackType; }
            set { _selectedPullbackType = value; SetPullback(); }
        }

        [ObservableProperty]
        private Dictionary<string, string> _procedureList;

        [ObservableProperty]
        private Dictionary<string, string> _vesselList;

        [ObservableProperty]
        private Dictionary<string, string> _locationList;

        [ObservableProperty]
        private KeyValuePair<string, string> _currentProcedure;

        [ObservableProperty]
        private KeyValuePair<string, string> _currentVessel;

        [ObservableProperty]
        private KeyValuePair<string, string> _currentLocation;

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next)); }
        }

        public RecordingPresetViewModel(SqlManager sqlManager)
        {
            _log.Debug("RecordingPresetViewModel");

            Constants.CurrentPage = Constants.RecordingPresetPage;

            _sqlManager = sqlManager;

            Init();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                this.Patient = (Patient)data["patient"];
                this.PrevStatus = (PrevStatus)data["prevStatus"];

                if (data.ContainsKey("patientCase"))
                {
                    PatientCase = (PatientCase)data["patientCase"];
                    if (CodeDefinition.Codes["PROC"].ContainsKey(PatientCase.Procedure))
                        CurrentProcedure = new KeyValuePair<string, string>(PatientCase.Procedure, CodeDefinition.Codes["PROC"][PatientCase.Procedure]);
                    if (CodeDefinition.Codes["VESS"].ContainsKey(PatientCase.Vessel))
                        CurrentVessel = new KeyValuePair<string, string>(PatientCase.Vessel, CodeDefinition.Codes["VESS"][PatientCase.Vessel]);
                    if (CodeDefinition.Codes["LOCT"].ContainsKey(PatientCase.Location))
                        CurrentLocation = new KeyValuePair<string, string>(PatientCase.Location, CodeDefinition.Codes["LOCT"][PatientCase.Location]);
                }
                else
                {
                    Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                    sqlParameters["id"] = Patient.PhysicianId;
                    IList<Physician> physicians = _sqlManager.SelectPhysician(sqlParameters);

                    PatientCase = new PatientCase();
                    PatientCase.PatientId = Patient.Id;
                    PatientCase.PhysicianName = physicians[0].Name;
                    PatientCase.FlushMedia = physicians[0].FlushMedia;
                    PatientCase.PullbackTrigger = physicians[0].PullbackTrigger;
                    PatientCase.PullbackType = physicians[0].PullbackType;
                    PatientCase.Colormap = physicians[0].Colormap;
                    PatientCase.CalciumThreshold = physicians[0].CalciumThreshold;
                    PatientCase.ExpansionThreshold = physicians[0].ExpansionThreshold;
                    PatientCase.AppositionThreshold = physicians[0].AppositionThreshold;
                    PatientCase.AccessionNumber = "";
                    PatientCase.Comment = "";

                    sqlParameters.Clear();
                    sqlParameters["classification"] = "Present";
                    IList<Configuration> presents = _sqlManager.SelectConfiguration(sqlParameters);
                    if (presents != null && presents.Count > 0)
                    {
                        PatientCase.Brightness = int.Parse(presents.FirstOrDefault(x => x.Key == "brightness").Value);
                        PatientCase.Contrast = int.Parse(presents.FirstOrDefault(x => x.Key == "contrast").Value);
                        PatientCase.FieldOfView = double.Parse(presents.FirstOrDefault(x => x.Key == "FoV").Value);
                    }
                    CommonUtil.SetColormap(PatientCase.Colormap);

                    CurrentProcedure = new KeyValuePair<string, string>("$001", CodeDefinition.Codes["PROC"]["$001"]);
                    CurrentVessel = new KeyValuePair<string, string>("$000", CodeDefinition.Codes["VESS"]["$000"]);
                    CurrentLocation = new KeyValuePair<string, string>("$000", CodeDefinition.Codes["LOCT"]["$000"]);
                }

                SelectedFlushMedia = PatientCase.FlushMedia;
                SelectedPullbackType = PatientCase.PullbackType;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            base.OnNavigating(sender, navigationEventArgs);
        }

        private void Init()
        {
            _log.Debug("Init");

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);

            FlushMediaList = CodeDefinition.Codes["FLMD"];
            PullbackTriggerList = CodeDefinition.Codes["PBTG"];
            PullbackTypeList = CodeDefinition.Codes["PBTY"];

            ProcedureList = CodeDefinition.Codes["PROC"];
            VesselList = CodeDefinition.Codes["VESS"];
            LocationList = CodeDefinition.Codes["LOCT"];
        }

        private void SetPullback()
        {
            _log.Debug("SetPullback");

            if (SelectedPullbackType == null) return;

            Code pullback = pullbackTypes.FirstOrDefault(x => x.Key == SelectedPullbackType);

            if (pullback != null)
            {
                string[] temp = pullback.Buffer1.Split("|");
                PbLength = temp[0];
                PbSpeed = temp[1];
                PbTime = temp[2];
            }
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = this.Patient;
            parameter["prevStatus"] = this.PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientDetailPage) { Parameter = parameter });
        }

        private void Next()
        {
            _log.Debug("Next");

            PatientCase.FlushMedia = SelectedFlushMedia;
            PatientCase.PullbackType = SelectedPullbackType;
            PatientCase.PullbackLength = PbLength + "0";
            PatientCase.Procedure = CurrentProcedure.Key;
            PatientCase.Vessel = CurrentVessel.Key;
            PatientCase.Location = CurrentLocation.Key;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.RecordingSetupPage) { Parameter = parameter });
        }
    }
}
