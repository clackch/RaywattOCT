using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using log4net;
using RaywattApp.Common.Setting;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;
using System.Windows.Input;

namespace RaywattApp.ViewModels.Setting
{
    public partial class SettingTermsConditionsViewModel : SettingBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SettingTermsConditionsViewModel));

        private readonly SqlManager _sqlManager;

        [ObservableProperty]
        private Configuration _termsConditions;

        [ObservableProperty]
        private string _termsAndConditions;

        [ObservableProperty]
        private string _validateInstituteName;

        [ObservableProperty]
        private bool _isModify;

        private ICommand _modifyInstituteCommand;
        public ICommand ModifyInstituteCommand
        {
            get { return this._modifyInstituteCommand ?? (this._modifyInstituteCommand = new RelayCommand(ModifyInstitute)); }
        }

        public SettingTermsConditionsViewModel(SqlManager sqlManager)
        {
            _log.Debug("SettingTermsConditionsViewModel");

            _sqlManager = sqlManager;

            TermsConditions = new Configuration();
            TermsConditions.PropertyChanged += TermsConditions_PropertyChanged;

            TermsAndConditions = _l10n["$Terms and Conditions"];
        }

        private void TermsConditions_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (IsModify)
                ValidateInstituteName = "";
        }

        public override void OnNavigated(object sender, object navigatedEventArgs)
        {
            _log.Debug("OnNavigated");

            Init();
        }

        public override void OnNavigating(object sender, object navigationEventArgs)
        {
            _log.Debug("OnNavigating");
        }

        protected override void Okay()
        {
            _log.Debug("Okay");
            
            if (!Validate())
                return;

            Save();
            CloseDialog();
        }

        protected override void Apply()
        {
            _log.Debug("Apply");

            if (!Validate())
                return;

            Save();            
        }

        private void Init()
        {
            IList<Configuration> tnCs = _sqlManager.SelectConfigurationTnC();
            if(tnCs != null || tnCs.Count == 1)
            {
                TermsConditions.Value = tnCs[0].Value;
                TermsConditions.Buffer = tnCs[0].Buffer;
            }
        }

        private void ModifyInstitute()
        {
            IsModify = true;
        }

        private bool Validate()
        {
            if(string.IsNullOrEmpty(TermsConditions.Buffer))
            {
                ValidateInstituteName = _l10n["Enter Institute Name"];
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Save()
        {
            if (!IsModify)
                return;

            IsModify = false;

            Dictionary<string, object> sqlParameters = new Dictionary<string, object>();
            sqlParameters["value"] = TermsConditions.Value;
            sqlParameters["buffer"] = TermsConditions.Buffer;

            int res = _sqlManager.UpdateConfigurationTnC(sqlParameters);
            if(res != 1)
            {
                _log.Error("Insert Error");
            }
        }
    }
}
