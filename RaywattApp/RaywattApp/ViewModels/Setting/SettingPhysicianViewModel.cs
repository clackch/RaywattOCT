using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Setting;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingPhysicianViewModel : SettingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingPhysicianViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<Physician> _physicianList;

        private IList<Physician> originPhysicianList;

        [ObservableProperty]
        private Physician _newPhysician;

        private ICommand _rowAddCommand;
        public ICommand RowAddCommand
        {
            get { return this._rowAddCommand ?? (this._rowAddCommand = new RelayCommand(AddRow, CanAddPhysician)); }
        }

        private ICommand _rowDeleteCommand;
        public ICommand RowDeleteCommand
        {
            get { return this._rowDeleteCommand ?? (this._rowDeleteCommand = new RelayCommand<Physician>(DeleteRow)); }
        }

        private ICommand _gridDoubleClickCommand;
        public ICommand GridDoubleClickCommand
        {
            get { return this._gridDoubleClickCommand ?? (this._gridDoubleClickCommand = new RelayCommand<Physician>(EditPhysician)); }
        }

        public SettingPhysicianViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingPhysicianViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            PhysicianList = new ObservableCollection<Physician>();
            NewPhysician = new Physician();
            NewPhysician.Name = "";
            NewPhysician.PropertyChanged += NewPhysician_PropertyChanged;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
            }

            Search();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Okay()
        {
            _log.Debug("Okay");

            Save();
            CloseDialog();
        }

        protected override void Apply()
        {
            _log.Debug("Apply");

            Save();
            Search();
        }

        private void Search()
        {
            _log.Debug("Search");

            PhysicianList.Clear();

            originPhysicianList = _sqlManager.SelectPhysicianList();

            foreach(Physician physician in originPhysicianList)
            {
                Physician newPhysician = new Physician();
                newPhysician.Name = physician.Name;
                PhysicianList.Add(newPhysician);
            }
        }

        private void NewPhysician_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _log.Debug("NewPhysician_PropertyChanged");

            (RowAddCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        private bool CanAddPhysician()
        {
            _log.Debug("CanAddPhysician");

            if (string.IsNullOrEmpty(NewPhysician.Name.Trim()))
                return false;

            return true;
        }

        private void AddRow()
        {
            _log.Debug("AddRow");

            foreach(Physician item in PhysicianList)
            {
                if (item.Name.Equals(NewPhysician.Name.Trim()))
                {
                    NewPhysician.ValidateName = _l10n["Name is duplicated"];
                    return;
                }
            }

            NewPhysician.ValidateName = "";

            Physician physician = new Physician();
            physician.Name = NewPhysician.Name;
            PhysicianList.Add(physician);

            NewPhysician.Name = "";
        }

        private void DeleteRow(Physician physician)
        {
            _log.Debug("DeleteRow");

            if (physician == null)
                return;

            PhysicianList.Remove(physician);
        }

        private void EditPhysician(Physician physician)
        {
            if (physician == null)
                return;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["physician"] = physician;
            parameter["physicianList"] = PhysicianList;
            var result = _dialogService.OpenDialog(new SettingEditPhysicianDialogControl(), parameter, Constants.SettingDialogWidth, Constants.SettingDialogHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Yes)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)result.DialogReturn;
                physician.Name = data["physicianName"].ToString();
            }
        }

        private void Save()
        {
            int resDel = _sqlManager.DeletePhysician();

            if(resDel != -1)
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();

                int cntList = PhysicianList.Count;
                int resAdd = 0;

                foreach(Physician item in PhysicianList)
                {
                    if (item.Name == null || item.Name.Trim() == "")
                        continue;

                    sqlParameters.Clear();
                    sqlParameters["name"] = item.Name.Trim();

                    resAdd += _sqlManager.InsertPhysician(sqlParameters);
                }

                if(cntList != resAdd)
                {
                    _log.Error("Insert Error");
                }
            }
        }
    }
}
