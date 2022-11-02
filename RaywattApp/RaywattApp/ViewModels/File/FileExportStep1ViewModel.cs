using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.File;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using RaywattApp.Services;
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

        [ObservableProperty]
        FileExport _fileExportData;

        [ObservableProperty]
        IList<Patient> _patientList;

        [ObservableProperty]
        IList<PatientCase> _patientCaseList;

        [ObservableProperty]
        private bool? _checkBoxAllSelected;

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

        public FileExportStep1ViewModel(SqlManager sqlManager)
        {
            _log.Debug("FileExportStep1ViewModel");

            _sqlManager = sqlManager;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                FileExportData = (FileExport)extraData;
                SetCondition();
            }
            else
            {
                FileExportData = new FileExport();
                FileExportData.Type = "N";

                SetPatientList(null);
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Next()
        {
            _log.Debug("Next");

            if (FileExportData.SelectedItem == null)
                FileExportData.SelectedItem = new List<string>();
            else
                FileExportData.SelectedItem.Clear();

            if (FileExportData.PatientList == null)
                FileExportData.PatientList = new List<string>();
            else
                FileExportData.PatientList.Clear();

            foreach (Patient patient in PatientList)
            {
                if (patient.PatientCaseList == null)
                    continue;

                foreach(PatientCase patientCase in patient.PatientCaseList)
                {
                    if (patientCase.IsChecked)
                    {
                        _log.Debug(patientCase.Id);
                        if(!FileExportData.PatientList.Contains(patient.Id))
                            FileExportData.PatientList.Add(patient.Id);
                        FileExportData.SelectedItem.Add(patientCase.Id);
                    }
                }
            }

            switch (FileExportData.Type)
            {
                case "N":
                    WeakReferenceMessenger.Default.Send(new PopupNavigationMessage("Views/File/FileExportStep2NativePage.xaml") { Parameter = FileExportData });
                    break;
                case "D":
                    break;
                case "S":
                    break;
                default:
                    break;
            }

        }

        private void SetCondition()
        {
            _log.Debug("SetCondition");

            foreach(var item in FileExportData.SelectedItem)
            {
                _log.Debug(item.ToString());
            }

            if (FileExportData.Type == null)
                FileExportData.Type = "N";

            SetPatientList(FileExportData.PatientId);
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

            if (FileExportData.PatientList == null || FileExportData.PatientList.Count == 0)
                return;

            foreach (Patient patient in PatientList)
            {
                foreach (string pId in FileExportData.PatientList)
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

            if (FileExportData.SelectedItem == null || FileExportData.SelectedItem.Count == 0)
                return;

            foreach (Patient patient in PatientList)
            {
                if (patient.PatientCaseList == null)
                    continue;

                foreach (PatientCase patientCase in patient.PatientCaseList)
                {
                    foreach (string item in FileExportData.SelectedItem)
                    {
                        if (patientCase.Id == item)
                        {
                            patientCase.IsChecked = true;
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
            }
            else if (checkBox.IsChecked == false)
            {
                foreach (PatientCase patientCase in PatientCaseList)
                {
                    patientCase.IsChecked = false;
                }
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
            }
            else if (isChecked && !isNotChecked)
            {
                CheckBoxAllSelected = true;
            }
            else if (!isChecked && isNotChecked)
            {
                CheckBoxAllSelected = false;
            }
        }
    }
}
