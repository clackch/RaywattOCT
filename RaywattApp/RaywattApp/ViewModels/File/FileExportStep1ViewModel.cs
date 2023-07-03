using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Common.File;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattApp.ViewModels.File
{
    public partial class FileExportStep1ViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileExportStep1ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        FileExport _fileExport;

        [ObservableProperty]
        Patient _patientChecked;

        [ObservableProperty]
        IList<Patient> _patientList;

        [ObservableProperty]
        IList<PatientCase> _patientCaseList;

        [ObservableProperty]
        private bool? _checkBoxAllSelected;

        [ObservableProperty]
        private bool _isBookmarkOn = false;

        private ICommand _showCaseCommand;
        public ICommand ShowCaseCommand
        {
            get { return this._showCaseCommand ?? (this._showCaseCommand = new RelayCommand<Patient>(ShowCase)); }
        }

        private ICommand _checkBoxToggleCommand;
        public ICommand CheckBoxToggleCommand
        {
            get { return this._checkBoxToggleCommand ?? (this._checkBoxToggleCommand = new RelayCommand<CheckBox>(ToggleCheckBox)); }
        }

        private ICommand _checkBoxClickCommand;
        public ICommand CheckBoxClickCommand
        {
            get { return this._checkBoxClickCommand ?? (this._checkBoxClickCommand = new RelayCommand(ChangeCheckBoxHeader)); }
        }

        public FileExportStep1ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileExportStep1ViewModel");

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                FileExport = (FileExport)extraData;

                if (FileExport.Material == null)
                    FileExport.Material = Constants.ExportMaterialPullback;

                if(FileExport.SelectedItem !=  null)
                {
                    SetCondition();
                }
                else
                {
                    FileExport.Type = Constants.ExportTypeNative;

                    SetPatientList(null);
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Next()
        {
            _log.Debug("Next");

            if (FileExport.SelectedItem == null)
                FileExport.SelectedItem = new List<string>();
            else
                FileExport.SelectedItem.Clear();

            if (FileExport.PatientList == null)
                FileExport.PatientList = new List<string>();
            else
                FileExport.PatientList.Clear();

            foreach (Patient patient in PatientList)
            {
                if (patient.PatientCaseList == null)
                    continue;

                foreach(PatientCase patientCase in patient.PatientCaseList)
                {
                    if (patientCase.IsChecked)
                    {
                        _log.Debug(patientCase.Id);
                        if(!FileExport.PatientList.Contains(patient.Id))
                            FileExport.PatientList.Add(patient.Id);
                        FileExport.SelectedItem.Add(patientCase.Id);
                    }
                }
            }

            if(FileExport.SelectedItem.Count == 0)
            {
                Dictionary<string, object> parameter = new Dictionary<string, object>();
                parameter["title"] = _l10n["Information"];
                parameter["message"] = _l10n["No items have been selected"];
                var result = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.FileExportDialogWidth, Constants.FileExportDialogHeight);

                return;
            }

            switch (FileExport.Type)
            {
                case Constants.ExportTypeNative :
                    WeakReferenceMessenger.Default.Send(new PopupNavigationMessage(Constants.FileExportStep2NativePage) { Parameter = FileExport });
                    break;
                case Constants.ExportTypeDicom:
                    WeakReferenceMessenger.Default.Send(new PopupNavigationMessage(Constants.FileExportStep2DicomPage) { Parameter = FileExport });
                    break;
                case Constants.ExportTypeStandard:
                    WeakReferenceMessenger.Default.Send(new PopupNavigationMessage(Constants.FileExportStep2StandardPage) { Parameter = FileExport });
                    break;
                default:
                    break;
            }

        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            foreach(var item in FileExport.SelectedItem)
            {
                _log.Debug(item.ToString());
            }

            if (FileExport.Type == null)
                FileExport.Type = Constants.ExportTypeNative;

            if (FileExport.IsFromReview)
            {
                if (FileExport.BookmarkedFrames.Count > 0)
                    IsBookmarkOn = true;
            }

            SetPatientList(FileExport.PatientId);
        }

        private void SetPatientList(string patientId)
        {
            _log.Debug("SetPatientList");

            if (String.IsNullOrEmpty(patientId))
            {
                PatientList = _sqlManager.SelectPatientListByCase(null);

                SetPatientCase();
            }
            else
            {
                Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                sqlParameters["id"] = patientId;
                PatientList = _sqlManager.SelectPatientListByCase(sqlParameters);

                PatientList[0].PatientCaseList = _sqlManager.SelectPatientCaseList(sqlParameters);
            }

            if (PatientList.Count > 0)
            {
                ShowCase(PatientList[0]);
                SetSelectedCase();
                ChangeCheckBoxHeader();
            }
                
        }

        private void SetPatientCase()
        {
            _log.Debug("SetPatientCase");

            if (FileExport.PatientList == null || FileExport.PatientList.Count == 0)
                return;

            foreach (Patient patient in PatientList)
            {
                foreach (string pId in FileExport.PatientList)
                {
                    if (patient.Id == pId)
                    {
                        Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
                        sqlParameters["id"] = patient.Id;

                        patient.PatientCaseList = _sqlManager.SelectPatientCaseList(sqlParameters);

                        break;
                    }
                }
            }
        }

        private void SetSelectedCase()
        {
            _log.Debug("SetSelectedCase");

            if (FileExport.SelectedItem == null || FileExport.SelectedItem.Count == 0)
                return;

            foreach (Patient patient in PatientList)
            {
                if (patient.PatientCaseList == null)
                    continue;

                foreach (PatientCase patientCase in patient.PatientCaseList)
                {
                    foreach (string item in FileExport.SelectedItem)
                    {
                        if (patientCase.Id == item)
                        {
                            patientCase.IsChecked = true;
                            patient.IsChecked = true;
                            break;
                        }
                    }
                }
            }
        }

        private void ShowCase(Patient patient)
        {
            _log.Debug("ShowCase");

            Dictionary<string, Object> sqlParameters = new Dictionary<string, Object>();
            sqlParameters["id"] = patient.Id;

            if (patient.PatientCaseList == null)
                patient.PatientCaseList = _sqlManager.SelectPatientCaseList(sqlParameters);

            PatientChecked = patient;
            PatientCaseList = patient.PatientCaseList;
            ChangeCheckBoxHeader();
        }

        private void ToggleCheckBox(CheckBox checkBox)
        {
            if (checkBox.IsChecked == true)
            {
                foreach (PatientCase patientCase in PatientCaseList)
                {
                    patientCase.IsChecked = true;
                }
                PatientChecked.IsChecked = true;
            }
            else if (checkBox.IsChecked == false)
            {
                foreach (PatientCase patientCase in PatientCaseList)
                {
                    patientCase.IsChecked = false;
                }
                PatientChecked.IsChecked = false;
            }
        }

        private void ChangeCheckBoxHeader()
        {
            bool isChecked = false;
            bool isNotChecked = false;

            foreach (PatientCase patientCase in PatientCaseList)
            {
                if (patientCase.IsChecked == true)
                    isChecked = true;

                if (patientCase.IsChecked == false)
                    isNotChecked = true;
            }

            if (isChecked && isNotChecked)
            {
                CheckBoxAllSelected = null;
                PatientChecked.IsChecked = null;
            }
            else if (isChecked && !isNotChecked)
            {
                CheckBoxAllSelected = true;
                PatientChecked.IsChecked = true;
            }
            else if (!isChecked && isNotChecked)
            {
                CheckBoxAllSelected = false;
                PatientChecked.IsChecked = false;
            }
        }
    }
}
