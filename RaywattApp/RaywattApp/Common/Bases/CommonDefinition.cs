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
            RecordingPage
        }

        public enum PopupType : int
        {
            Message = 0,
            Setting,
            File
        }

        public enum PopupLevel : int
        {
            Info = 0,
            Warn,
            Error
        }

        public enum FileType : int
        {
            Export = 0,
            Import
        }
    }
}
