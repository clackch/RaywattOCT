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
            RecordingPage,
            ReviewPage
        }

        public enum PopupType : int
        {
            Message = 0,
            Question,
            Setting,
            File,
            CaseEdit
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

        public enum QuestionList : int
        {
            PatientCaseDelete = 0,
        }
    }
}
