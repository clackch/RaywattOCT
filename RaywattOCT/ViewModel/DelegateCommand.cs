using System;
using System.Windows.Input;

namespace RaywattOCT.ViewModel
{
    public class DelegateCommand : ICommand
    {
        private readonly Func<bool> canExecute;
        private readonly Action execute;
        private readonly Action<object> executeWithParam;

        public DelegateCommand(Action execute) : this(execute, null) { }

        public DelegateCommand(Action<object> executeWithParam) : this(executeWithParam, null) { }

        public DelegateCommand(Action execute, Func<bool> canExecute) 
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public DelegateCommand(Action<object> executeWithParam, Func<bool> canExecute)
        {
            this.executeWithParam = executeWithParam;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (canExecute == null) return true;
            return canExecute();
        }

        public void Execute(object parameter)
        {
            if (parameter == null) execute();
            else executeWithParam(parameter);
        }

        public void RaiseCanExecuteChanged() 
        {
            if (CanExecuteChanged != null) 
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
