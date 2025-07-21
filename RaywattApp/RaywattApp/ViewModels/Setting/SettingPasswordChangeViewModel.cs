using CommunityToolkit.Mvvm.ComponentModel;
using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingPasswordChangeViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingAboutViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        [ObservableProperty]
        string _loginID = string.Empty;

        public SettingPasswordChangeViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _sqlManager = sqlManager;
            _dialogService = dialogService;
            LoginID = "asdfasdf";
        }


    }
}
