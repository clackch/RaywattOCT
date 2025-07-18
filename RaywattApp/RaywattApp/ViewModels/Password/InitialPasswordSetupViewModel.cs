using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Messages;
using RaywattApp.Models;
using System.Windows;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Password
{
    partial class InitialPasswordSetupViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(InitialPasswordSetupViewModel));

        [ObservableProperty]
        private TextValidator _id = new TextValidator();

        [ObservableProperty]
        private string _password;

        [RelayCommand]
        private void CheckPassword()
        {
            MessageBox.Show($"입력한 비밀번호: {Password}");
        }

        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get { return this._cancelCommand ?? (_cancelCommand = new RelayCommand(Cancel)); }
        }

        private ICommand _yesCommand;
        public ICommand YesCommand
        {
            get { return this._yesCommand ?? (_yesCommand = new RelayCommand(Yes)); }
        }

        public InitialPasswordSetupViewModel()
        {
            
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        private void Cancel()
        {
            // login 화면으로 이동
            WeakReferenceMessenger.Default.Send(new NavigationMessage(Constants.OutsetLoginPage));
        }

        private void Yes()
        {
            string id = Id.Text.Trim();
            string password = Password;
            // step 1. 패스워드 규칙
            // step 2. DB Update
            // step 3. 다음 페이지로 이동
        }
    }
}
