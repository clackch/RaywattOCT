using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Models;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.ComponentModel;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class SettingEditPhysicianDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingEditPhysicianDialogViewModel));

        [ObservableProperty]
        private Physician _physician;

        [ObservableProperty]
        private ObservableCollection<Physician> _physicianList;

        private int _physicianIndex;

        private ICommand _yesCommand;
        public ICommand YesCommand
        {
            get { return this._yesCommand ?? (this._yesCommand = new RelayCommand<IDialogWindow>(AnswerYes, CanEditPhysician)); }
        }

        public override void SetParameter(object parameter)
        {
            Dictionary<string, Object> data = (Dictionary<string, Object>)parameter;
            Physician tempPhysician = (Physician)data["physician"];
            PhysicianList = (ObservableCollection<Physician>)data["physicianList"];
            _physicianIndex = PhysicianList.IndexOf(tempPhysician);

            Physician = new Physician();
            Physician.Name = tempPhysician.Name;
            Physician.PropertyChanged += Physician_PropertyChanged;
        }

        private void Physician_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _log.Debug("Physician_PropertyChanged");

            (YesCommand as RelayCommand<IDialogWindow>).NotifyCanExecuteChanged();
        }

        protected override void AnswerYes(IDialogWindow dialog)
        {
            for (int i=0; i<PhysicianList.Count; i++)
            {
                if (PhysicianList[i].Name.Equals(Physician.Name.Trim()) && i != _physicianIndex)
                {
                    Physician.ValidateName = _l10n["Name is duplicated."];
                    return;
                }
            }

            Dictionary<string, object> parameter = new Dictionary<string, object>();
            parameter["physicianName"] = Physician.Name.Trim();

            DialogResults dialogResults = new();
            dialogResults.DialogAnswer = DialogResults.Answer.Yes;
            dialogResults.DialogReturn = parameter;

            CloseDialogWithResult(dialog, dialogResults);
        }

        private bool CanEditPhysician(IDialogWindow dialog)
        {
            _log.Debug("CanEditPhysician");

            if (string.IsNullOrEmpty(Physician.Name.Trim()))
                return false;

            return true;
        }
    }
}
