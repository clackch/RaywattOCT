using log4net;
using RaywattApp.Common.Dialog;
using RaywattApp.Services;

namespace RaywattApp.ViewModels.Dialog
{
    public partial class TermsConditionsDialogViewModel : DialogViewModelBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(TermsConditionsDialogViewModel));

        private readonly SqlManager _sqlManager;

        public TermsConditionsDialogViewModel(SqlManager sqlManager)
        {
            _sqlManager = sqlManager;
        }
    }
}
