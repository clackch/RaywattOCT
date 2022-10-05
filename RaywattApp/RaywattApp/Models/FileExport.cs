using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace RaywattApp.Models
{
    public partial class FileExport : ObservableValidator
    {
        [ObservableProperty]
        private string type; //Native, DICOM, Standard

        [ObservableProperty]
        private List<string> selectedItem;

        [ObservableProperty]
        private string material; //Pullback, Current Frame, Bookmarked Frames

        [ObservableProperty]
        private string purpose; //Archive, Share, Report a Problem

        [ObservableProperty]
        private bool passwordProtected;

        [ObservableProperty]
        private string alternatePatiendId;

        [ObservableProperty]
        private string fileOption; //Leave Unchanged, Mark as Archived, Remove when Complete

        [ObservableProperty]
        private string diskType;

        [ObservableProperty]
        private string volumeLabel;

        [ObservableProperty]
        private bool ejectWhenComplete;
    }
}
