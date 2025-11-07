using CommunityToolkit.Mvvm.ComponentModel;
using RaywattApp.Common.Bases;

namespace RaywattApp.Models
{
    public class Institute : ObservableObject
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value.Length <= Constants.MaxConfigurationValue)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _comment;
        public string Comment
        {
            get { return _comment; }
            set
            {
                if (value.Length <= Constants.MaxConfigurationValue)
                {
                    _comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }
    }
}
