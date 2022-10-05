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

        private readonly IDatabaseService _databaseService;

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

        public PatientListViewModel(IDatabaseService databaseService)
        {
            _log.Debug("PatientListViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.PatientListPage;

            _databaseService = databaseService;

            //Header Name
            SetHeaderNameInit();

            //Order
            ColumOrderField = "update_date";
            bColumnOrderBy = false;
            strColumnOrder = ColumOrderField + " DESC";

            //Initial Order Field
            HeaderUpdateDate = HeaderUpdateDate + " ▼";

            //Initialize Complete
            bCheckInit = true;
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

        private void GetTotalCnt(string commandText, Dictionary<string, Object> commandParameters)
        {
            _log.Debug("GetTotalCnt");

            commandText = $"SELECT count(*) FROM (" + commandText + ") t";

            PagingTotalCnt = _databaseService.GetDataCount(commandText, commandParameters);
        }

        override protected void Search()
        {
            _log.Debug("Search");

            Dictionary<string, Object> commandParameters = new Dictionary<string, Object>();
            commandParameters["id"] = "%" + SearchKeyword + "%";
            commandParameters["lastname"] = "%" + SearchKeyword + "%";
            commandParameters["firstname"] = "%" + SearchKeyword + "%";
            string commandText =
                $"SELECT id, lastname, firstname, birthdate, gender, to_char(create_date,'YYYY-MM-DD HH24:MI:SS') createdate, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') updatedate " +
                $"FROM rv_schema.patient " +
                $"WHERE id LIKE @id OR lastname LIKE @lastname OR firstname LIKE @firstname";

            //Paging을 위한 전체 Row 수 Count
            GetTotalCnt(commandText, commandParameters);

            //Ordering & Paging
            commandText += $"{_databaseService.getAddtionalCondition(strColumnOrder, PagingSelectedPageSize, PagingOffset)}";

            var datas = _databaseService.GetDatas<Patient>(commandText, commandParameters);
            PatientList = datas;            
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
