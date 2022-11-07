using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class Patient : ObservableValidator
    {
        private string _id;
        public string Id
        {
            get { return _id; }
            set
            {
                if(value.Length <= Constants.MaxPatientId)
                {
                    if(!CommonUtil.ValidateId(value))
                        return;

                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        private string _lastname;
        public string Lastname
        {
            get { return _lastname; }
            set
            {
                if (value.Length <= Constants.MaxPatientLastname)
                {
                    if (!CommonUtil.ValidateText(value))
                        return;

                    _lastname = value;
                    OnPropertyChanged(nameof(Lastname));
                }
            }
        }

        private string _firstname;
        public string Firstname
        {
            get { return _firstname; }
            set
            {
                if (value.Length <= Constants.MaxPatientFirstname)
                {
                    if (!CommonUtil.ValidateText(value))
                        return;

                    _firstname = value;
                    OnPropertyChanged(nameof(Firstname));
                }
            }
        }

        private string _alternateId;
        public string AlternateId
        {
            get { return _alternateId; }
            set
            {
                if (value == null)
                    return;

                if (value.Length <= Constants.MaxPatientId)
                {
                    if (!CommonUtil.ValidateId(value))
                        return;

                    _alternateId = value;
                    OnPropertyChanged(nameof(AlternateId));
                }
            }
        }

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private DateTime birthdate;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        private DateTime createDate;

        [ObservableProperty]
        private DateTime updateDate;

        [ObservableProperty]
        private string lastCase;

        [ObservableProperty]
        private string displayLastCase;

        [ObservableProperty]
        private IList<PatientCase> patientCaseList;

        [ObservableProperty]
        private bool isChecked;
    }
}
