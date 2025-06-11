using log4net;
using RaywattApp.Common.Bases;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;

namespace RaywattApp.ViewModels.Admin
{
    public partial class UserEditViewModel : ViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UserEditViewModel));

        private readonly SqlManager _sqlManager;

        private IDialogService _dialogService;

        public UserEditViewModel(SqlManager sqlManager, IDialogService dialogService)
        {
            _log.Debug("UserEditViewModel");

            Constants.CurrentPage = Constants.UserEditPage;

            _sqlManager = sqlManager;
            _dialogService = dialogService;
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }
    }
}
