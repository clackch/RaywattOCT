using System.Windows.Shapes;

namespace RaywattOCTFFR.Common.Annotation.Models
{
    public class AreaGeometry : Contour
    {
        private int group;
        public int Group { get { return group; } set { group = value; OnPropertyChanged(nameof(Group)); } }

        private bool isClosed;
        public bool IsClosed { get { return isClosed; } set { isClosed = value; } }

        private Path path;
        public Path Path { get { return path; } set { path = value; } }
    }
}
