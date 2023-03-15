using System.Windows;

namespace RaywattApp.Views.Component
{    public interface SvgComponentBase
    {
        private const string IconExtenstion = ".svg";

        protected static string GetPath(string path, string value, string postfix = "")
        {
            return path + value + postfix + IconExtenstion;
        }

        protected static void OnIconPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var iconComponent = obj as SvgComponentBase;
            if (iconComponent == null) return;

            string value = e.NewValue as string;
            if (string.IsNullOrEmpty(value)) return;

            iconComponent.InitializeIconPath(value);
        }

        public abstract void InitializeIconPath(string iconName);
    }
}
