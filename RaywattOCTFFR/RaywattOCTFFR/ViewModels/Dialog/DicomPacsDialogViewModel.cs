using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using log4net;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;

namespace RaywattOCTFFR.ViewModels.Dialog
{
    public partial class DicomPacsDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DicomPacsDialogViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private DicomServer _selectedDicomServer;

        [ObservableProperty]
        private IList<DicomServer> _dicomServers;

        private ICommand _selectCommand;
        public ICommand SelectCommand
        {
            get { return this._selectCommand ?? (this._selectCommand = new RelayCommand<IDialogWindow>(Select, CanSelect)); }
        }

        private ICommand _selectionChangedCommand;
        public ICommand SelectionChangedCommand
        {
            get { return this._selectionChangedCommand ?? (this._selectionChangedCommand = new RelayCommand<DicomServer>(SelectionChanged)); }
        }

        public DicomPacsDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["server_type"] = "PACS";
            DicomServers = _sqlManager.SelectDicomServer(sqlParameters);
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            int selectedDicomServerId = (int)data["selectedDicomServerId"];

            SelectedDicomServer = DicomServers.FirstOrDefault(x => x.Id == selectedDicomServerId);
        }

        private void Select(IDialogWindow dialog)
        {
            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["selectedDicomServer"] = SelectedDicomServer;

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private bool CanSelect(IDialogWindow dialog)
        {
            _log.Debug("CanSelect");

            return SelectedDicomServer == null ? false : true;
        }

        private void SelectionChanged(DicomServer dicomServer)
        {
            _log.Debug("SelectionChanged");

            (SelectCommand as RelayCommand<IDialogWindow>).NotifyCanExecuteChanged();
        }
    }
}
