using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattOCTFFR.Common.Bases;
using RaywattOCTFFR.Common.Dialog;
using RaywattOCTFFR.Common.File;
using RaywattOCTFFR.Common.Messages;
using RaywattOCTFFR.Common.Util;
using RaywattOCTFFR.Models;
using RaywattOCTFFR.Services;
using RaywattOCTFFR.Views.Dialog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Navigation;

namespace RaywattOCTFFR.ViewModels.File
{
    public partial class FileImportStep2ViewModel : FileBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(FileImportStep2ViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        private FileImport _fileImport = new FileImport();

        [ObservableProperty]
        private IList<Code> _vesselList;

        private double _catheterSize = 2.6;
        public double CatheterSize
        {
            get { return _catheterSize; }
            set
            {
                _catheterSize = Math.Floor(value * 10) / 10;
                OnPropertyChanged(nameof(CatheterSize));                
            }
        }

        [ObservableProperty]
        private int _pullbackLength = 60;

        [ObservableProperty]
        private bool _isDistalToProximal = true;

        [ObservableProperty]
        private Dictionary<string, string> _genderComboBox = new Dictionary<string, string>();

        [ObservableProperty]
        private string _selectedGender;

        private ICommand _nextCommand;
        public ICommand NextCommand
        {
            get { return this._nextCommand ?? (this._nextCommand = new RelayCommand(Next, CanSavePatient)); }
        }

        public FileImportStep2ViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("FileImportStep2ViewModel");

            Constants.CurrentPage = Constants.FileImportStep2Page;

            _sqlManager = sqlManager;
            _dialogService = dialogService;

            foreach (var gender in CodeDefinition.Codes["GEND"])
            {
                GenderComboBox.Add(gender.Key, _l10n[gender.Value]);
            }

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["classification"] = "VESS";
            VesselList = _sqlManager.SelectCode(sqlParameters);
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            var extraData = ((NavigationEventArgs)navigatedEventArgs).ExtraData;

            if (extraData != null)
            {
                Dictionary<string, Object> data = (Dictionary<string, Object>)extraData;
                if (data.TryGetValue("fileImport", out var fileImportObj) && fileImportObj is FileImport fileImportData)
                {
                    FileImport = fileImportData;
                    int pullbackLength = string.IsNullOrWhiteSpace(FileImport.PatientCase.PullbackLength) ? 0 : int.Parse(FileImport.PatientCase.PullbackLength);
                    if (pullbackLength >= Constants.PullbackLengthMin && pullbackLength <= Constants.PullbackLengthMax)
                        PullbackLength = pullbackLength;
                    IsDistalToProximal = FileImport.PatientCase.IsDistalToProximal;
                    SelectedGender = FileImport.Patient.Gender;

                    var vessel = FileImport.PatientCase.Vessel;
                    var isValid = vessel != null && CodeDefinition.Codes["VESS"].ContainsKey(vessel);
                    FileImport.PatientCase.Vessel = !isValid || vessel is "$000" or "$001" ? "$002" : VesselList.FirstOrDefault(x => x.Key == vessel)?.Buffer1 ?? "$002";

                    FileImport.Patient.PropertyChanged += Patient_PropertyChanged;
                }
            }
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Patient_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _log.Debug("Patient_PropertyChanged");

            (NextCommand as RelayCommand).NotifyCanExecuteChanged();
        }

        protected override void Back()
        {
            _log.Debug("Back");

            Dictionary<string, Object> parameter = new Dictionary<string, Object>();
            parameter["fileImport"] = FileImport;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.FileImportStep1Page) { Parameter = parameter });
        }

        protected override void Next()
        {
            _log.Debug("Next");

            string importedFile = Constants.TempPath + "\\" + FileImport.PatientCase.Image;
            if (System.IO.File.Exists(importedFile))
            {
                MoveNextPage();
            }
            else
            {
                string filePath = CommonUtil.CheckFile(FileImport.FilePath, FileImport.PatientCase.Image);
                if (!string.IsNullOrEmpty(filePath))
                {
                    if (ImportFile(filePath))
                    {
                        MoveNextPage();
                    }
                }
                else
                {
                    Dictionary<string, object> parameter = new Dictionary<string, object>();
                    parameter["title"] = _l10n["Information"];
                    parameter["message"] = _l10n["No image file found."];
                    var result2 = _dialogService.OpenDialog(new AlertDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);
                }

            }
        }

        private void MoveNextPage()
        {
            FileImport.PatientCase.CatheterSize = CatheterSize;
            FileImport.PatientCase.SheathDiameter = CatheterSize / 3.0;
            FileImport.PatientCase.PullbackLength = PullbackLength.ToString();
            FileImport.PatientCase.IsDistalToProximal = IsDistalToProximal;
            FileImport.PatientCase.ZOffset = 0;

            Dictionary<string, Object> parameter = new Dictionary<string, Object>();
            parameter["fileImport"] = FileImport;
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.FileImportStep3Page) { Parameter = parameter });
        }

        private bool ImportFile(string filePath)
        {
            _log.Debug("ImportFile");

            Dictionary<string, string> importfiles = new Dictionary<string, string>();
            FileImport.PatientCase.Image = CommonUtil.GenerateFileName(CommonUtil.GetFileExtension(FileImport.PatientCase.ImportType, true));
            CommonUtil.DeleteFolder(Constants.TempPath);
            importfiles.Add(filePath, CommonUtil.CreateFolder(Constants.TempPath) + "\\" + FileImport.PatientCase.Image);

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["title"] = _l10n["File Import"];
            parameter["isSkipConfirmation"] = true;
            parameter["fileImport"] = importfiles;
            parameter["path"] = FileImport.FilePath;
            var result = _dialogService.OpenDialog(new FileCopyDialogControl(), parameter, Constants.ApplicationWidth, Constants.ApplicationHeight);

            if (result != null && result.DialogAnswer == DialogResults.Answer.Undefined)
            {
                return true;
            }
            else
            {
                CommonUtil.DeleteFolder(Constants.TempPath);
                return false;
            }
        }

        private bool CanSavePatient()
        {
            _log.Debug("CanSavePatient");

            return ValidatePatient();
        }

        private bool ValidatePatient()
        {
            _log.Debug("ValidatePatient");

            if (string.IsNullOrWhiteSpace(FileImport.Patient.Id))
                return false;

            if (string.IsNullOrWhiteSpace(FileImport.Patient.Lastname))
                return false;

            if (string.IsNullOrWhiteSpace(FileImport.Patient.Firstname))
                return false;

            return true;
        }
    }
}
