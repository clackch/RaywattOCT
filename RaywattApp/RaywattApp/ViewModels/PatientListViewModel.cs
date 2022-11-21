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
using System.Windows.Navigation;
using System.Reflection;
using RaywattApp.Common.Dialog;
using RaywattApp.Views.Dialog;

namespace RaywattApp.ViewModels
{
    public partial class PatientListViewModel : PagingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientListViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private PrevStatus _prevStatus;

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

        public PatientListViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("PatientListViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.PatientListPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            //Header Name
            SetHeaderNameInit();

            //Order
            ColumOrderField = "last_case";
            bColumnOrderBy = false;
            strColumnOrder = ColumOrderField + " DESC";

            //Initial Order Field
            HeaderLastCase = HeaderLastCase + " ▼";
            strColumnHeaderColumn = "LastCase";

            //Initialize Complete
            bCheckInit = true;

            //Initialize SearchKeyword
            SearchKeyword = "";
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                PrevStatus = (PrevStatus)extraData;

                SetPrevStatus();
            }
            else
            {
                Search();
                ShowFirstLast();
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Search()
        {
            _log.Debug("Search");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["SearchKeyword"] = SearchKeyword.Trim();

            //Paging을 위한 전체 Row 수 Count
            PagingTotalCnt = _sqlManager.PageCountPatientList(sqlParameters);

            Dictionary<string, Object> sqlAdditionalCondition = new Dictionary<string, Object>();
            sqlAdditionalCondition["ORDER"] = strColumnOrder;
            sqlAdditionalCondition["LIMIT"] = PagingSelectedPageSize;
            sqlAdditionalCondition["OFFSET"] = PagingOffset;

            PatientList = _sqlManager.PageSelectPatientList(sqlParameters, sqlAdditionalCondition);
        }

        protected override void SetHeaderNameInit()
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

        private void SetPrevStatus()
        {
            SearchKeyword = PrevStatus.ListKeyword;
            strColumnOrder = PrevStatus.ListSort;
            strColumnHeaderColumn = PrevStatus.ListSortField;
            bColumnOrderBy = PrevStatus.ListSortDirection;
            PagingSelectedPageSize = PrevStatus.ListPageSize;
            PagingOffset = PrevStatus.ListPageOffset;

            ShowPageNo(PrevStatus.ListPageGroup);
            MovePageNo((PrevStatus.ListPageNumber + 1).ToString());
            SetHeaderNameInit();
            PropertyInfo piHeaderName = GetType().GetProperty("Header" + PrevStatus.ListSortField);

            if (piHeaderName != null)
            {
                if (PrevStatus.ListSortDirection)
                {
                    piHeaderName.SetValue(this, piHeaderName.GetValue(this) + " ▲");
                }
                else
                {
                    piHeaderName.SetValue(this, piHeaderName.GetValue(this) + " ▼");
                }
            }
        }

        private void Import()
        {
            _log.Debug("Import");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["fileType"] = CommonDefinition.FileType.Import;

            var result = _dialogService.OpenDialog(new FileDialogControl(), parameter);
        }

        private void Export()
        {
            _log.Debug("Export");

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["fileType"] = CommonDefinition.FileType.Export;

            var result = _dialogService.OpenDialog(new FileDialogControl(), parameter);
        }

        private void MovePatientDetail(Patient patient)
        {
            _log.Debug("MovePatientDetail");

            if (patient == null)
                return;

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["patient"] = patient;
            parameter["prevStatus"] = GetListStatus();
            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientDetailPage.xaml") { Parameter = parameter });
        }

        private void MovePatientNew()
        {
            _log.Debug("MovePatientNew");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientNewPage.xaml") {  Parameter = GetListStatus() });
        }

        private PrevStatus GetListStatus()
        {
            PrevStatus prevStatus = new PrevStatus();
            prevStatus.ListKeyword = SearchKeyword;
            prevStatus.ListSortField = strColumnHeaderColumn;
            prevStatus.ListSortDirection = bColumnOrderBy;
            prevStatus.ListSort = strColumnOrder;
            prevStatus.ListPageOffset = PagingOffset;
            prevStatus.ListPageSize = PagingSelectedPageSize;
            prevStatus.ListPageGroup = ((PagingNoIdx - 1) / Constants.PageNumberMax) * Constants.PageNumberMax + 1;
            prevStatus.ListPageNumber = PagingNoIdx;

            return prevStatus;
        }
    }
}
