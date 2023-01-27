using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;

namespace RaywattApp.Models
{
    public partial class Physician : ObservableObject
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set 
            { 
                if(value.Length <= Constants.MaxPhysicianName)
                {
                    if (!CommonUtil.ValidateText(value))
                        return;

                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [ObservableProperty]
        private DateTime createDate;
    }
}
