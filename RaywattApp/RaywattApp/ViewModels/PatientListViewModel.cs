using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using RaywattApp.Common.Paging;
using log4net;
using RaywattApp.Common.Bases;

namespace RaywattApp.ViewModels
{
    public partial class PatientListViewModel : PagingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientListViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private IList<Patient> _patientList;

        [ObservableProperty]
        private string _searchKeyword;

        private ICommand _importCommand;
        public ICommand ImportCommand
        {
            get { return this._importCommand ?? (this._importCommand = new RelayCommand(Import)); }
        }

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get { return this._exportCommand ?? (this._exportCommand = new RelayCommand(Export)); }
        }

        private ICommand _searchCommand;
        public ICommand SearchCommand
        {
            get { return this._searchCommand ?? (this._searchCommand = new RelayCommand(Search)); }
        }

        private ICommand _gridDoubleClickCommand;
        public ICommand GridDoubleClickCommand
        {
            get { return this._gridDoubleClickCommand ?? (this._gridDoubleClickCommand = new RelayCommand<Patient>(MovePatientDetail)); }
        }

        private ICommand _newPatientCommand;
        public ICommand NewPatientCommand
        {
            get { return this._newPatientCommand ?? (this._newPatientCommand = new RelayCommand(MovePatientNew)); }
        }

        [ObservableProperty]
        private string _headerId;

        [ObservableProperty]
        private string _headerLastname;

        [ObservableProperty]
        private string _headerFirstname;

        [ObservableProperty]
        private string _headerBirthdate;

        [ObservableProperty]
        private string _headerGender;

        [ObservableProperty]
        private string _headerCreateDate;

        [ObservableProperty]
        private string _headerUpdateDate;

        [ObservableProperty]
        private string _headerLastCase;

        public PatientListViewModel(SqlManager sqlManager)
        {
            _log.Debug("PatientListViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.PatientListPage;

            _sqlManager = sqlManager;

            //Header Name
            SetHeaderNameInit();

            //Order
            ColumOrderField = "last_case";
            bColumnOrderBy = false;
            strColumnOrder = ColumOrderField + " DESC";

            //Initial Order Field
            HeaderLastCase = HeaderLastCase + " ▼";

            //Initialize Complete
            bCheckInit = true;

            //Initialize SearchKeyword
            SearchKeyword = "";
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            Search();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        override protected void Search()
        {
            _log.Debug("Search");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["SearchKeyword"] = SearchKeyword.Trim();

            Dictionary<string, Object> sqlAdditionalCondition = new Dictionary<string, Object>();
            sqlAdditionalCondition["ORDER"] = strColumnOrder;
            sqlAdditionalCondition["LIMIT"] = PagingSelectedPageSize;
            sqlAdditionalCondition["OFFSET"] = PagingOffset;

            //Paging을 위한 전체 Row 수 Count
            PagingTotalCnt = _sqlManager.PageCountPatientList(sqlParameters);

            PatientList = _sqlManager.PageSelectPatientList(sqlParameters, sqlAdditionalCondition);
        }

        override protected void SetHeaderNameInit()
        {
            _log.Debug("SetHeaderNameInit");

            HeaderId = _l10n["ID"];
            HeaderLastname = _l10n["Last Name"];
            HeaderFirstname = _l10n["First Name"];
            HeaderBirthdate = _l10n["Birth Date"];
            HeaderGender = _l10n["Gender"];
            HeaderCreateDate = _l10n["Create Date"];
            HeaderUpdateDate = _l10n["Update Date"];
            HeaderLastCase = _l10n["Last Case (total)"];
        }

        private void Import()
        {
            _log.Debug("Import");

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "FilePopupControl", Type = (int)CommonDefinition.PopupType.File, FileType = (int)CommonDefinition.FileType.Import });
        }

        private void Export()
        {
            _log.Debug("Export");

            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "FilePopupControl", Type = (int)CommonDefinition.PopupType.File, FileType = (int)CommonDefinition.FileType.Export });
        }

        private void MovePatientDetail(Patient param)
        {
            _log.Debug("MovePatientDetail");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = param});
        }

        private void MovePatientNew()
        {
            _log.Debug("MovePatientNew");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientNewPage.xaml"));
        }
    }
}
