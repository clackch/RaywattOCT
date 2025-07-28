using System;
using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;

namespace RaywattApp.Models
{
    public partial class User : ObservableObject
    {
        private string _id;
        public string Id
        {
            get { return _id; }
            set
            {
                if (value.Length <= Constants.MaxPatientId)
                {
                    if (!CommonUtil.ValidateId(value))
                        return;

                    _id = value;
                    OnPropertyChanged(nameof(Id));

                    validateId = "";
                    OnPropertyChanged(nameof(ValidateId));
                }
            }
        }

        [ObservableProperty]
        private string validateId;

        [ObservableProperty]
        private string _password;

        private string _comment;
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (value.Length <= Constants.MaxPatientCaseComment)
                {
                    _comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }

        [ObservableProperty]
        private DateTime _passwordChangedAt;

        [ObservableProperty]
        private bool _passwordReset;

        [ObservableProperty]
        private DateTime _termsAgreedAt;

        [ObservableProperty]
        private DateTime _createDate;

        [ObservableProperty]
        private DateTime _updateDate;
    }
}
