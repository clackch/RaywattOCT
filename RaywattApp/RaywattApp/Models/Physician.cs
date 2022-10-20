using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class Physician : ObservableValidator
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set 
            { 
                if(value.Length <= 20)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [ObservableProperty]
        private string createDate;
    }
}
