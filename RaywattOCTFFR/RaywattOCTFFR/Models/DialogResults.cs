namespace RaywattOCTFFR.Models
{
    public class DialogResults
    {
        public enum Answer
        {
            Undefined,
            Yes,
            No,
            Extra
        }

        public Answer DialogAnswer { get; set; }

        public object? DialogReturn;
    }
}
