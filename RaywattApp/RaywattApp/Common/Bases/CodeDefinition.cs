using log4net;
using RaywattApp.Common.Localization;
using RaywattApp.Models;
using RaywattApp.Services;
using System.Collections.Generic;

namespace RaywattApp.Common.Bases
{
    public class CodeDefinition
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CodeDefinition));

        protected readonly DynamicResource _l10n;

        private readonly SqlManager _sqlManager;

        public static Dictionary<string, Dictionary<string, string>> Codes = new Dictionary<string, Dictionary<string, string>>();

        public CodeDefinition(SqlManager sqlManager)
        {
            _log.Debug("CodeDefinition");

            _l10n = (DynamicResource)App.Current.Resources["L10N"];

            _sqlManager = sqlManager;
        }

        public void GetCode()
        {
            _log.Debug("GetCode");

            IList<Code> codeList = _sqlManager.SelectCodeList();

            Dictionary<string, string> addCode = new Dictionary<string, string>();
            string prevClassification = null;

            foreach (Code tempCode in codeList)
            {
                if (prevClassification != null && !prevClassification.Equals(tempCode.Classification))
                {
                    Codes[prevClassification] = addCode;
                    addCode = new Dictionary<string, string>();

                }
                addCode[tempCode.Key] = _l10n[tempCode.Value];
                prevClassification = tempCode.Classification;
            }

            if (addCode.Count > 0)
            {
                Codes[prevClassification] = addCode;
            }
        }
    }
}
