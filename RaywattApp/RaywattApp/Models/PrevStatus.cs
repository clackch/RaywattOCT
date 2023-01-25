using CommunityToolkit.Mvvm.ComponentModel;

namespace RaywattApp.Models
{
    public partial class PrevStatus : ObservableObject
    {
        [ObservableProperty]
        private string listKeyword;

        [ObservableProperty]
        private string listSortField;

        [ObservableProperty]
        private bool listSortDirection;

        [ObservableProperty]
        private string listSort;

        [ObservableProperty]
        private int listPageOffset;

        [ObservableProperty]
        private int listPageSize;

        [ObservableProperty]
        private int listPageGroup;

        [ObservableProperty]
        private int listPageNumber;

        [ObservableProperty]
        private int detailPageGroup;

        [ObservableProperty]
        private int detailPageNumber;

        [ObservableProperty]
        private int detailPageOffset;

        [ObservableProperty]
        private string detailSelectedGroup;
    }
}
