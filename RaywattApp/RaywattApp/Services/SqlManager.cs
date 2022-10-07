using System.Collections.Generic;
using System;
using log4net;
using RaywattApp.Models;

namespace RaywattApp.Services
{
    public class SqlManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SqlManager));

        private readonly IDatabaseService _databaseService;

        public SqlManager(IDatabaseService databaseService)
        {
            _log.Debug("SqlManager");

            _databaseService = databaseService;
        }

        public int PageCountPatientList(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("PageCountPatientList");

            string commandText = SqlQuery.GetCountQuery("SelectPatientList");

            Dictionary<string, Object> commandParameters = new Dictionary<string, Object>();
            commandParameters["id"] = "%" + sqlParameters["SearchKeyword"] + "%";
            commandParameters["lastname"] = "%" + sqlParameters["SearchKeyword"] + "%";
            commandParameters["firstname"] = "%" + sqlParameters["SearchKeyword"] + "%";

            return _databaseService.GetDataCount(commandText, commandParameters);
        }

        public IList<Patient> PageSelectPatientList(Dictionary<string, Object> sqlParameters, Dictionary<string, Object> sqlAdditionalCondition)
        {
            _log.Debug("PageSelectPatientList");

            string commandText = SqlQuery.GetQuery("SelectPatientList");
            commandText += getAdditionalCondition(sqlAdditionalCondition);

            Dictionary<string, Object> commandParameters = new Dictionary<string, Object>();
            commandParameters["id"] = "%" + sqlParameters["SearchKeyword"] + "%";
            commandParameters["lastname"] = "%" + sqlParameters["SearchKeyword"] + "%";
            commandParameters["firstname"] = "%" + sqlParameters["SearchKeyword"] + "%";

            return _databaseService.GetDatas<Patient>(commandText, commandParameters);
        }

        public int CountPatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("CountPatient");

            string commandText = SqlQuery.GetCountQuery("SelectPatient");

            return _databaseService.GetDataCount(commandText, sqlParameters);
        }

        public int InsertPatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertPatient");

            string commandText = SqlQuery.GetQuery("InsertPatient");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdatePatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatient");

            string commandText = SqlQuery.GetQuery("UpdatePatient");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public IList<Code> SelectCodeList()
        {
            _log.Debug("SelectCodeList");

            string commandText = SqlQuery.GetQuery("SelectCodeList");

            return _databaseService.GetDatas<Code>(commandText);
        }

        private string getAdditionalCondition(Dictionary<string, Object> sqlAdditionalCondition)
        {
            _log.Debug("getAddtionalCondition");

            string order = (string)sqlAdditionalCondition["ORDER"];
            int limit = (int)sqlAdditionalCondition["LIMIT"];
            int offset = (int)sqlAdditionalCondition["OFFSET"];

            string orderCommand = order == "" ? "" : " ORDER BY " + order;
            string limitCommand = limit > 0 ? " LIMIT " + limit : "";
            string offsetCommand = offset > 0 ? " OFFSET " + offset : "";

            return orderCommand + limitCommand + offsetCommand;
        }
    }
}
