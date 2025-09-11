using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Paging;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class PhysicianEditViewModel : PagingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PhysicianEditViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private Physician _physician;

        [ObservableProperty]
        private bool _isNew;

        [ObservableProperty]
        private Dictionary<string, string> _flushMediaList;

        [ObservableProperty]
        private Dictionary<string, string> _pullbackTriggerList;

        [ObservableProperty]
        private Dictionary<string, string> _pullbackTypeList;

        [ObservableProperty]
        private Dictionary<string, string> _colormapList;

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
        private string _selectedColormap;

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

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get { return this._saveCommand ?? (this._saveCommand = new RelayCommand(Save, CanSave)); }
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (this._cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _deleteCommand;
        public ICommand DeleteCommand
        {
            get { return this._deleteCommand ?? (this._deleteCommand = new RelayCommand(Delete)); }
        }

        public PhysicianEditViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PhysicianEditViewModel");

            Constants.CurrentPage = Constants.PhysicianEditPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            Init();
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                PrevStatus = (PrevStatus)data["prevStatus"];

                if (data.TryGetValue("physician", out var physicianObj) && physicianObj is Physician physician)
                {
                    Physician = physician;
                    IsNew = false;
                    SetDefault(Physician);
                }
                else
                {
                    Physician = new Physician();
                    IsNew = true;
                    SetDefault();
                }

                Physician.PropertyChanged += Physician_PropertyChanged;
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Init()
        {
            _log.Debug("Init");

            MaxCalciumThreshold = Constants.MaxCalciumThreshold;
            MinCalciumThreshold = Constants.MinCalciumThreshold;
            MaxExpansionThreshold = Constants.MaxExpansionThreshold;
            MinExpansionThreshold = Constants.MinExpansionThreshold;
            MaxAppositionThreshold = Constants.MaxAppositionThreshold;
            MinAppositionThreshold = Constants.MinAppositionThreshold;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);

            FlushMediaList = CodeDefinition.Codes["FLMD"];
            PullbackTriggerList = CodeDefinition.Codes["PBTG"];
            PullbackTypeList = CodeDefinition.Codes["PBTY"];
            ColormapList = CodeDefinition.Codes["CLMP"];
        }

        private void Physician_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _log.Debug("Physician_PropertyChanged");

            (SaveCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private void Save()
        {
            _log.Debug("Save");

            Physician.FlushMedia = SelectedFlushMedia;
            Physician.PullbackType = SelectedPullbackType;
            Physician.Colormap = SelectedColormap;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["lastname"] = Physician.Lastname.Trim();
            sqlParameters["firstname"] = Physician.Firstname.Trim();
            sqlParameters["flush_media"] = Physician.FlushMedia;
            sqlParameters["pullback_trigger"] = Physician.PullbackTrigger;
            sqlParameters["pullback_type"] = Physician.PullbackType;
            sqlParameters["colormap"] = Physician.Colormap;
            sqlParameters["calcium_threshold"] = Physician.CalciumThreshold;
            sqlParameters["expansion_threshold"] = Physician.ExpansionThreshold;
            sqlParameters["apposition_threshold"] = Physician.AppositionThreshold;

            int nRows = 0;

            if (IsNew)
            {
                nRows = _sqlManager.InsertPhysician(sqlParameters);
            }
            else
            {
                sqlParameters["id"] = Physician.Id;
                nRows = _sqlManager.UpdatePhysician(sqlParameters);
            }

            if (nRows != 1)
                _log.Error(IsNew ? "Insert Error" : "Update Error");

            GoBack();
        }

        private bool CanSave()
        {
            _log.Debug("CanSave");

            if (Physician.Lastname == null || string.IsNullOrEmpty(Physician.Lastname.Trim()))
                return false;

            if (Physician.Firstname == null || string.IsNullOrEmpty(Physician.Firstname.Trim()))
                return false;

            return true;
        }

        private void Cancel()
        {
            _log.Debug("Cancel");

            GoBack();
        }

        private void GoBack()
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PhysicianListPage) { Parameter = parameter });
        }

        private void Delete()
        {
            _log.Debug("Delete Physician");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["Information"];
            parameter["message"] = _l10n["Confirm deletion of selected physician"];
            var result = _dialogService.OpenDialog(new ConfirmDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = Physician.Id;
                int res = _sqlManager.DeletePhysician(sqlParameters);

                if (res != 1)
                {
                    _log.Error("Delete Error : id=" + Physician.Id);
                }

                GoBack();
            }
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

        private void SetDefault(Physician physician = null)
        {
            _log.Debug("SetDefault");

            if(physician == null)
            {
                SelectedFlushMedia = "SALI";
                Physician.PullbackTrigger = "MANL";
                SelectedPullbackType = "STSH";
                SelectedColormap = "GRGR";
                Physician.CalciumThreshold = 180;
                Physician.ExpansionThreshold = 90;
                Physician.AppositionThreshold = 0.3;
            }
            else
            {
                SelectedFlushMedia = Physician.FlushMedia;
                SelectedPullbackType = Physician.PullbackType;
                SelectedColormap = Physician.Colormap;
            }
        }
    }
}
