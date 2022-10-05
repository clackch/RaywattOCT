using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Services;
using RaywattApp.Models;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.ViewModels
{
    public partial class PatientDetailViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PatientDetailViewModel));

        private readonly IDatabaseService _databaseService;

        [ObservableProperty]
        private Patient _patient;

        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get { return this._exportCommand ?? (this._exportCommand = new RelayCommand(Export)); }
        }

        private ICommand _backCommand;
        public ICommand BackCommand
        {
            get { return this._backCommand ?? (this._backCommand = new RelayCommand(Back)); }
        }

        private ICommand _newRecordingCommand;
        public ICommand NewRecordingCommand
        {
            get { return this._newRecordingCommand ?? (this._newRecordingCommand = new RelayCommand(NewRecording)); }
        }

        public PatientDetailViewModel(IDatabaseService databaseService)
        {
            _log.Debug("PatientDetailViewModel");

            CommonDefinition.CurrentPage = (int)CommonDefinition.PageList.PatientDetailPage;
            _databaseService = databaseService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Back()
        {
            _log.Debug("Back");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/PatientListPage.xaml"));
        }

        private void NewRecording()
        {
            _log.Debug("NewRecording");

            WeakReferenceMessenger.Default.Send(new NavigationMessage("Views/RecordingPage.xaml") { Parameter = Patient });
        }

        private void Export()
        {
            _log.Debug("Export");

            FileExport fileExportData = new FileExport();
            List<string> selectedItem = new List<string>();
            selectedItem.Add("TEST111");
            selectedItem.Add("TEST222");
            selectedItem.Add("TEST333");

            fileExportData.SelectedItem = selectedItem;

            //항목 선택된 건 Parameter로 넘기도록
            WeakReferenceMessenger.Default.Send(new PopupMessage(true) { ControlName = "FilePopupControl", Type = (int)CommonDefinition.PopupType.File, FileType = (int)CommonDefinition.FileType.Export, Parameter = fileExportData });
        }
    }
}
