using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.Messages;
using RaywattApp.Common.Setting;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
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

        private bool isModified;

        private bool isNewRow;

        private Physician selectedPhysician;

        private string selectedPhysicianName;

        private ICommand _rowAddCommand;
        public ICommand RowAddCommand
        {
            get { return this._rowAddCommand ?? (this._rowAddCommand = new RelayCommand<DataGrid>(AddRow)); }
        }

        private ICommand _rowDeleteCommand;
        public ICommand RowDeleteCommand
        {
            get { return this._rowDeleteCommand ?? (this._rowDeleteCommand = new RelayCommand<Physician>(DeleteRow)); }
        }

        private ICommand _beginningEditCommand;
        public ICommand BeginningEditCommand
        {
            get { return this._beginningEditCommand ?? (this._beginningEditCommand = new RelayCommand<DataGrid>(BeginningEdit)); }
        }

        private ICommand _rowEditEndingCommand;
        public ICommand RowEditEndingCommand
        {
            get { return this._rowEditEndingCommand ?? (this._rowEditEndingCommand = new RelayCommand<DataGrid>(RowEditEnding)); }
        }

        public SettingPhysicianViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("SettingPhysicianViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            PhysicianList = new ObservableCollection<Physician>();

            isNewRow = false;
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

        private void AddRow(DataGrid dataGrid)
        {
            _log.Debug("AddRow");

            Physician physician = new Physician();
            physician.Name = "";
            PhysicianList.Add(physician);

            dataGrid.Focus();
            dataGrid.SelectedIndex = PhysicianList.Count - 1;
            dataGrid.CurrentCell = new DataGridCellInfo(PhysicianList[PhysicianList.Count - 1], dataGrid.Columns[0]); // set current cell    
            dataGrid.BeginEdit();

            isNewRow = true;
        }

        private void DeleteRow(Physician physician)
        {
            _log.Debug("DeleteRow");

            if (physician == null)
                return;

            PhysicianList.Remove(physician);

            CopyPhysicianList();
        }

        private void BeginningEdit(DataGrid dataGrid)
        {
            _log.Debug("BeginningEdit");

            isModified = true;

            selectedPhysician = (Physician)dataGrid.SelectedItem;
            if(selectedPhysician == null)
                selectedPhysician = (Physician)dataGrid.CurrentItem;

            selectedPhysicianName = selectedPhysician.Name.Trim();
        }

        private void RowEditEnding(DataGrid dataGrid)
        {
            _log.Debug("RowEditEnding");

            Physician physician = (Physician)dataGrid.SelectedItem;
            if (physician == null)
                physician = (Physician)dataGrid.CurrentItem;
            if (physician == null)
                physician = (dataGrid.DataContext as SettingPhysicianViewModel).selectedPhysician;

            if (physician.Name.Trim() == "")
            {
                if (isNewRow)
                {
                    PhysicianList.Remove(physician);
                    CopyPhysicianList();
                    isNewRow = false;
                }
                else
                {
                    selectedPhysician.Name = selectedPhysicianName;
                    PhysicianList[PhysicianList.IndexOf(physician)] = selectedPhysician;
                }
                return;
            }

            if (physician.Name.Trim().Equals(selectedPhysicianName))
            {
                return;
            }

            foreach (Physician item in originPhysicianList)
            {
                if (item.Name.Equals(physician.Name.Trim()))
                {
                    isModified = false;

                    if (isNewRow)
                    {
                        PhysicianList.Remove(physician);
                        CopyPhysicianList();
                        isNewRow = false;
                    }
                    else
                    {
                        selectedPhysician.Name = selectedPhysicianName;
                        PhysicianList[PhysicianList.IndexOf(physician)] = selectedPhysician;
                    }

                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["Name is duplicated."];
                    var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter);
                    break;
                }
            }

            if (isModified)
            {
                CopyPhysicianList();
            }
        }

        private void CopyPhysicianList()
        {
            originPhysicianList.Clear();

            foreach (Physician physician in PhysicianList)
            {
                Physician newPhysician = new Physician();
                newPhysician.Name = physician.Name.Trim();
                originPhysicianList.Add(newPhysician);
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
