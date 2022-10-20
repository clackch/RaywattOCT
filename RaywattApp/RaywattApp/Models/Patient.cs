using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;

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

        [ObservableProperty]
        private DateTime birthdate;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        private string createDate;

        [ObservableProperty]
        private string updateDate;

        [ObservableProperty]
        private string lastCase;
    }
}
