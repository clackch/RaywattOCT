using System.Windows;

namespace RaywattApp.Common.Annotation.Models
{
    public class TextGeometry
    {
        private Point pointerPoint;
        public Point PointerPoint { get { return pointerPoint; } set { pointerPoint = value; } }

        private Point textPoint;
        public Point TextPoint { get { return textPoint; } set { textPoint = value; } }

        private int group;
        public int Group { get { return group; } set { group = value; } }

        private string text;
        public string Text { get { return text; } set { text = value; } }
    }
}
