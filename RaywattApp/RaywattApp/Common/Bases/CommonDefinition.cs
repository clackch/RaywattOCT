namespace RaywattApp.Common.Bases
{
    public class CommonDefinition
    {
        public static int CurrentPage;

        public enum PageList : int
        {
            PatientListPage = 0,
            PatientDetailPage,
            PatientNewPage,
            PatientEditPage,
            LiveViewPage,
            CalibrationPage,
            RecordingPage,
            ReviewPage
        }

        public enum FileType : int
        {
            Export = 0,
            Import
        }

        public enum FolderAction : int
        {
            Create = 0,
            Rename
        }
    }
}
