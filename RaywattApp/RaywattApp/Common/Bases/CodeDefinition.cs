using log4net;
using RaywattApp.Models;
using RaywattApp.Services;
using RaywattApp.ViewModels;
using System;
using System.Collections.Generic;

namespace RaywattApp.Common.Bases
{
    public class CodeDefinition
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(CodeDefinition));

        private readonly IDatabaseService _databaseService;

        public static Dictionary<string, Dictionary<string, string>> Codes = new Dictionary<string, Dictionary<string, string>>();

        public CodeDefinition(IDatabaseService databaseService)
        {
            _log.Debug("CodeDefinition");

            _databaseService = databaseService;
        }

        public void GetCode()
        {
            _log.Debug("GetCode");

            string commandText =
                $"SELECT classification, key, value, buffer1, buffer2 " +
                $"FROM rv_schema.code " +
                $"ORDER BY classification, key";

            IList<Code> codeList = _databaseService.GetDatas<Code>(commandText, new Dictionary<string, Object>());

            Dictionary<string, string> addCode = new Dictionary<string, string>();
            string prevClassification = null;

            foreach (Code tempCode in codeList)
            {
                if (prevClassification != null && !prevClassification.Equals(tempCode.Classification))
                {
                    Codes[prevClassification] = addCode;
                    addCode = new Dictionary<string, string>();

                }
                addCode[tempCode.Key] = tempCode.Value;
                prevClassification = tempCode.Classification;
            }

            if (addCode.Count > 0)
            {
                Codes[prevClassification] = addCode;
            }
        }
    }
}
