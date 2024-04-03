using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels
{
    public partial class PhysicianListViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PhysicianListViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

        [ObservableProperty]
        private IList<Physician> _physicianList;

        [ObservableProperty]
        private Physician _physician;

        [ObservableProperty]
        private string _searchKeyword;

        [ObservableProperty]
        private Physician _selectedPhysician;

        [ObservableProperty]
        private Visibility visibilityPhysician;

        private IList<Code> pullbackTypes;

        [ObservableProperty]
        private string _pbLength;

        [ObservableProperty]
        private string _pbSpeed;

        [ObservableProperty]
        private string _pbTime;

        private ICommand _newPhysicianCommand;
        public ICommand NewPhysicianCommand
        {
            get { return this._newPhysicianCommand ?? (this._newPhysicianCommand = new RelayCommand(NewPhysician)); }
        }

        private ICommand _editPhysicianCommand;
        public ICommand EditPhysicianCommand
        {
            get { return this._editPhysicianCommand ?? (this._editPhysicianCommand = new RelayCommand(EditPhysician)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _showPhysicianCommand;
        public ICommand ShowPhysicianCommand
        {
            get { return this._showPhysicianCommand ?? (this._showPhysicianCommand = new RelayCommand<Physician>(ShowPhysician)); }
        }

        private ICommand _searchCommand;
        public ICommand SearchCommand
        {
            get { return this._searchCommand ?? (this._searchCommand = new RelayCommand(Search)); }
        }

        public PhysicianListViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PhysicianListViewModel");

            Constants.CurrentPage = Constants.PhysicianListPage;

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
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Init()
        {
            _log.Debug("Init");

            VisibilityPhysician = Visibility.Collapsed;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "PBTY";
            pullbackTypes = _sqlManager.SelectCode(sqlParameters);

            SearchKeyword = "";
            Search();
        }

        private void NewPhysician()
        {
            _log.Debug("NewPhysician");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PhysicianEditPage) { Parameter = parameter });
        }

        private void EditPhysician()
        {
            _log.Debug("EditPhysician");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            parameter["physician"] = Physician;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PhysicianEditPage) { Parameter = parameter });
        }

        private void Back()
        {
            _log.Debug("Back");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["prevStatus"] = PrevStatus;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.PatientListPage) { Parameter = parameter });
        }

        private void ShowPhysician(Physician physician)
        {
            if (physician == null)
                return;

            Physician = physician;

            Code pullback = pullbackTypes.FirstOrDefault(x => x.Key == Physician.PullbackType);

            if (pullback != null)
            {
                string[] temp = pullback.Buffer1.Split("|");
                PbLength = temp[0];
                PbSpeed = temp[1];
                PbTime = temp[2];
            }
        }

        private void Search()
        {
            _log.Debug("Search");

            VisibilityPhysician = Visibility.Collapsed;

            int physicianId = 0;
            if (SelectedPhysician != null)
                physicianId = SelectedPhysician.Id;

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["SearchKeyword"] = SearchKeyword.Trim();

            PhysicianList = _sqlManager.SelectPhysicianList(sqlParameters);
            if (PhysicianList != null && PhysicianList.Count > 0)
            {
                VisibilityPhysician = Visibility.Visible;
                SelectedPhysician = PhysicianList.FirstOrDefault(x => x.Id == physicianId);

                if(SelectedPhysician == null)
                    SelectedPhysician = PhysicianList[0];

                ShowPhysician(SelectedPhysician);
            }
        }
    }
}
