using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class Physician : ObservableValidator
    {
        [ObservableProperty]
        public int index;

        [ObservableProperty]
        private string id;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string createDate;

        [ObservableProperty]
        private string updateDate;
    }
}
