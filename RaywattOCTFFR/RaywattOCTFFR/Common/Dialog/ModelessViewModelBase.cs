using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using System.Windows;
using System.Windows.Input;

namespace RaywattOCTFFR.Common.Dialog
{
    public interface IModelessPatient
    {
        public void SetResult(object result);
    }

    public partial class ModelessViewModelBase : ObservableObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ModelessViewModelBase));

        public IModelessPatient? Parent { get; set; }

        public object? Parameter { get; set; }

        public double DialogWidth { get; set; }

        public double DialogHeight { get; set; }
        
        public virtual void SetParameter(IModelessPatient parent, object parameter) { }

        private ICommand _cmdClose;
        public ICommand CmdClose
        {
            get { return this._cmdClose ?? (this._cmdClose = new RelayCommand<Window>(CloseWindow)); }
        }

        protected virtual void CloseWindow(Window window)
        {
            window.Close();
        }
    }
}
