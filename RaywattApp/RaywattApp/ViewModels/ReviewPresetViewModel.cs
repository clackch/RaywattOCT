using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class ReviewPresetViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReviewPresetViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Patient _patient;

        [ObservableProperty]
        private PatientCase _patientCase;

        [ObservableProperty]
        private IList<PatientCasePreset> _patientCasePresetList;

        [ObservableProperty]
        private PatientCasePreset _patientCasePreset;

        private bool _isPreset;

        [ObservableProperty]
        private Visibility _notPresetMode;

        [ObservableProperty]
        private Visibility _modifyMode;

        [ObservableProperty]
        private Visibility _selectionMode;

        [ObservableProperty]
        private int _presetIndex;

        [ObservableProperty]
        private int _maxCalciumThreshold;

        [ObservableProperty]
        private int _minCalciumThreshold;

        [ObservableProperty]
        private int _maxExpansionThreshold;

        [ObservableProperty]
        private int _minExpansionThreshold;

        [ObservableProperty]
        private double _maxAppositionThreshold;

        [ObservableProperty]
        private double _minAppositionThreshold;

        private ICommand _okCommand;
        public ICommand OkCommand
        {
            get { return this._okCommand ?? (this._okCommand = new RelayCommand(Ok)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _newCommand;
        public ICommand NewCommand
        {
            get { return this._newCommand ?? (this._newCommand = new RelayCommand(New)); }
        }

        private ICommand _editCommand;
        public ICommand EditCommand
        {
            get { return this._editCommand ?? (this._editCommand = new RelayCommand(Edit)); }
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get { return this._deleteCommand ?? (this._deleteCommand = new RelayCommand(Delete)); }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return this._saveCommand ?? (this._saveCommand = new RelayCommand(Save)); }
        }

        private ICommand _cancleCommand;
        public ICommand CancelCommand
        {
            get { return this._cancleCommand ?? (this._cancleCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _showPresetCommand;
        public ICommand ShowPresetCommand
        {
            get { return this._showPresetCommand ?? (this._showPresetCommand = new RelayCommand<PatientCasePreset>(ShowPreset)); }
        }

        public ReviewPresetViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("ReviewPresetViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.ReviewPresetPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            MaxCalciumThreshold = Constants.MaxCalciumThreshold;
            MinCalciumThreshold = Constants.MinCalciumThreshold;
            MaxExpansionThreshold = Constants.MaxExpansionThreshold;
            MinExpansionThreshold = Constants.MinExpansionThreshold;
            MaxAppositionThreshold = Constants.MaxAppositionThreshold;
            MinAppositionThreshold = Constants.MinAppositionThreshold;
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

                if (String.IsNullOrEmpty(PatientCase.Id))
                {
                    PatientCasePresetList = _sqlManager.SelectPatientCasePresetList();

                    _isPreset = true;
                    NotPresetMode = Visibility.Collapsed;
                    ModifyMode = Visibility.Collapsed;
                    SelectionMode = Visibility.Visible;
                }
                else
                {
                    PatientCasePresetList = new List<PatientCasePreset>();
                    PatientCasePreset patientCasePreset = new PatientCasePreset();
                    patientCasePreset.Id = PatientCase.Id;
                    patientCasePreset.PresetName = PatientCase.PresetName;
                    patientCasePreset.CalciumThreshold = PatientCase.CalciumThreshold;
                    patientCasePreset.ExpansionCalculation = PatientCase.ExpansionCalculation;
                    patientCasePreset.ExpansionThreshold = PatientCase.ExpansionThreshold;
                    patientCasePreset.AppositionThreshold = PatientCase.AppositionThreshold;
                    PatientCasePresetList.Add(patientCasePreset);

                    _isPreset = false;
                    NotPresetMode = Visibility.Visible;
                    ModifyMode = Visibility.Visible;
                    SelectionMode = Visibility.Collapsed;
                }

                PresetIndex = 0;
                PatientCasePreset = PatientCasePresetList[PresetIndex];
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Ok()
        {
            _log.Debug("Ok");

            PatientCase.Id = Patient.Id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            PatientCase.PatientId = Patient.Id;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = PatientCase.Id;
            sqlParameters["patient_id"] = PatientCase.PatientId;
            sqlParameters["physician_name"] = PatientCase.PhysicianName;
            sqlParameters["accession_number"] = PatientCase.AccessionNumber;
            sqlParameters["accession_name"] = PatientCase.AccessionName;
            sqlParameters["comment"] = PatientCase.Comment;
            sqlParameters["vessel"] = PatientCase.Vessel;
            sqlParameters["procedure"] = PatientCase.Procedure;
            sqlParameters["thumbnail_no"] = PatientCase.ThumbnailNo;
            sqlParameters["still_image_yn"] = PatientCase.StillImageYn;
            sqlParameters["image"] = PatientCase.Image;
            sqlParameters["pullback_type"] = PatientCase.PullbackType;
            sqlParameters["brightness"] = PatientCase.Brightness;
            sqlParameters["contrast"] = PatientCase.Contrast;
            sqlParameters["angio_co_registration"] = PatientCase.AngioCoRegistration;
            sqlParameters["preset_name"] = PatientCasePreset.PresetName;
            sqlParameters["calcium_threshold"] = PatientCasePreset.CalciumThreshold;
            sqlParameters["expansion_calculation"] = PatientCasePreset.ExpansionCalculation;
            sqlParameters["expansion_threshold"] = PatientCasePreset.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = PatientCasePreset.AppositionThreshold;

            int nRows = _sqlManager.InsertPatientCase(sqlParameters);

            if (nRows == 1)
            {
                GoToReview();
            }
            else
            {
                _log.Error("Insert Error");
            }
        }

        private void Back()
        {
            _log.Debug("Back");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPage.xaml") { Parameter = parameter });
        }

        private void New()
        {
            _log.Debug("New");

            ModifyMode = Visibility.Visible;
            SelectionMode = Visibility.Collapsed;

            PatientCasePreset = new PatientCasePreset();
            PatientCasePreset.CalciumThreshold = Constants.DefaultCalciumThreshold;
            PatientCasePreset.ExpansionCalculation = Constants.PresetTapered;
            PatientCasePreset.ExpansionThreshold = Constants.DefaultExpansionThreshold;
            PatientCasePreset.AppositionThreshold = Constants.DefaultAppositionThreshold;
        }

        private void Edit()
        {
            _log.Debug("Edit");

            if (PatientCasePreset.DefaultSet)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Default Preset can not be modified"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
            }
            else
            {
                ModifyMode = Visibility.Visible;
                SelectionMode = Visibility.Collapsed;
            }
        }

        private void Delete()
        {
            _log.Debug("Delete");

            if (PatientCasePreset.DefaultSet)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Default Preset can not be deleted"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["Are you sure to delete selected Preset?"];
                var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter);

                if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
                {
                    Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                    sqlParameters["id"] = PatientCasePreset.Id;
                    int cntDel = _sqlManager.DeletePatientCasePreset(sqlParameters);

                    if (cntDel == 1)
                    {
                        PatientCasePresetList = _sqlManager.SelectPatientCasePresetList();
                        PresetIndex = 0;
                        PatientCasePreset = PatientCasePresetList[PresetIndex];
                    }
                    else
                    {
                        _log.Error("Delete Error");
                    }
                }
            }
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            if (_isPreset)
            {
                ModifyMode = Visibility.Collapsed;
                SelectionMode = Visibility.Visible;

                if (String.IsNullOrEmpty(PatientCasePreset.Id))
                {
                    PresetIndex = 0;
                    PatientCasePreset = PatientCasePresetList[PresetIndex];
                }
            }
            else
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["patient"] = Patient;
                parameter["patientCase"] = PatientCase;
                parameter["prevStatus"] = PrevStatus;
                WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPage.xaml") { Parameter = parameter });
            }
        }

        private void Save()
        {
            _log.Debug("Save");

            if (_isPreset)
            {
                if (String.IsNullOrEmpty(PatientCasePreset.Id))
                {
                    Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                    sqlParameters["id"] = "Custom_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    sqlParameters["preset_name"] = PatientCasePreset.PresetName.Trim();
                    sqlParameters["calcium_threshold"] = PatientCasePreset.CalciumThreshold;
                    sqlParameters["expansion_calculation"] = PatientCasePreset.ExpansionCalculation;
                    sqlParameters["expansion_threshold"] = PatientCasePreset.ExpansionThreshold;
                    sqlParameters["apposition_threshold"] = PatientCasePreset.AppositionThreshold;

                    int nRows = _sqlManager.InsertPatientCasePreset(sqlParameters);

                    if(nRows == 1)
                    {
                        PatientCasePresetList = _sqlManager.SelectPatientCasePresetList();

                        for(int i=0; i<PatientCasePresetList.Count; i++)
                        {
                            if (PatientCasePresetList[i].Id.Equals(sqlParameters["id"].ToString()))
                            {
                                PresetIndex = i;
                                PatientCasePreset = PatientCasePresetList[PresetIndex];
                                break;
                            }
                        }
                    }
                    else
                    {
                        _log.Error("Insert Error");
                    }
                }
                else
                {
                    Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                    sqlParameters["id"] = PatientCasePreset.Id;
                    sqlParameters["preset_name"] = PatientCasePreset.PresetName.Trim();
                    sqlParameters["calcium_threshold"] = PatientCasePreset.CalciumThreshold;
                    sqlParameters["expansion_calculation"] = PatientCasePreset.ExpansionCalculation;
                    sqlParameters["expansion_threshold"] = PatientCasePreset.ExpansionThreshold;
                    sqlParameters["apposition_threshold"] = PatientCasePreset.AppositionThreshold;

                    int nRows = _sqlManager.UpdatePatientCasePreset(sqlParameters);

                    if (nRows == 1)
                    {
                        PatientCasePresetList = _sqlManager.SelectPatientCasePresetList();

                        for (int i = 0; i < PatientCasePresetList.Count; i++)
                        {
                            if (PatientCasePresetList[i].Id.Equals(sqlParameters["id"].ToString()))
                            {
                                PresetIndex = i;
                                PatientCasePreset = PatientCasePresetList[PresetIndex];
                                break;
                            }
                        }
                    }
                    else
                    {
                        _log.Error("Update Error");
                    }
                }

                ModifyMode = Visibility.Collapsed;
                SelectionMode = Visibility.Visible;
            }
            else
            {
                GoToReview();
            }
        }

        private void GoToReview()
        {
            _log.Debug("GoToReview");

            PatientCase.PresetName = PatientCasePreset.PresetName;
            PatientCase.CalciumThreshold = PatientCasePreset.CalciumThreshold;
            PatientCase.ExpansionCalculation = PatientCasePreset.ExpansionCalculation;
            PatientCase.ExpansionThreshold = PatientCasePreset.ExpansionThreshold;
            PatientCase.AppositionThreshold = PatientCasePreset.AppositionThreshold;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = Patient;
            parameter["patientCase"] = PatientCase;
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/ReviewPage.xaml") { Parameter = parameter });
        }

        private void ShowPreset(PatientCasePreset patientCasePreset)
        {
            ModifyMode = Visibility.Collapsed;
            SelectionMode = Visibility.Visible;

            PatientCasePreset = patientCasePreset;
        }
    }
}
