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

        /**
         * Code
         */
        public IList<Code> SelectCodeList()
        {
            _log.Debug("SelectCodeList");

            string commandText = SqlQuery.GetQuery("SelectCodeList");

            return _databaseService.GetDatas<Code>(commandText);
        }

        /**
         * L10n
         */
        public IList<L10n> SelectL10n()
        {
            _log.Debug("SelectL10n");

            string commandText = SqlQuery.GetQuery("SelectL10n");

            return _databaseService.GetDatas<L10n>(commandText);
        }

        public IList<L10n> SelectL10nList()
        {
            _log.Debug("SelectL10nList");

            string commandText = SqlQuery.GetQuery("SelectL10nList");

            return _databaseService.GetDatas<L10n>(commandText);
        }

        public int UpdateL10n(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdateL10n");

            string commandText = SqlQuery.GetQuery("UpdateL10n");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        /**
         * Patient
         */
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

        public IList<Patient> SelectPatientListByCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientListByCase");

            string commandText;

            if (sqlParameters == null)
            {
                commandText = SqlQuery.GetQuery("SelectPatientListByCase");
            }
            else
            {
                commandText = SqlQuery.GetQuery("SelectPatient");
            }
            

            return _databaseService.GetDatas<Patient>(commandText, sqlParameters);
        }

        public IList<Patient> SelectPatientByList(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientByList");

            string commandText;
            
            commandText = SqlQuery.GetQuery("SelectPatientByList");

            List<string> ids = (List<string>)sqlParameters["ids"];
            string commandTextExtra = "WHERE id IN (''";
            for (int i=0; i<ids.Count; i++)
            {
                commandTextExtra += ", '" + ids[i] + "'";
            }
            commandTextExtra += ") ";
            commandTextExtra += "ORDER BY id";

            commandText = commandText + commandTextExtra;

            return _databaseService.GetDatas<Patient>(commandText, sqlParameters);
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

        /**
         * Patient Case
         */
        public int PageCountPatientCaseByDate(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("PageCountPatientCaseByDate");

            string commandText = SqlQuery.GetCountQuery("SelectPatientCaseByDate");

            return _databaseService.GetDataCount(commandText, sqlParameters);
        }

        public IList<PatientCaseByDate> PageSelectPatientCaseByDate(Dictionary<string, Object> sqlParameters, Dictionary<string, Object> sqlAdditionalCondition)
        {
            _log.Debug("PageSelectPatientCaseByDate");

            string commandText = SqlQuery.GetQuery("SelectPatientCaseByDate");
            commandText += getAdditionalCondition(sqlAdditionalCondition);

            return _databaseService.GetDatas<PatientCaseByDate>(commandText, sqlParameters);
        }

        public IList<PatientCase> SelectPatientCaseListByDate(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientCaseListByDate");

            string commandText = SqlQuery.GetQuery("SelectPatientCaseListByDate");

            return _databaseService.GetDatas<PatientCase>(commandText, sqlParameters);
        }

        public IList<PatientCase> SelectPatientCaseList(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientCaseList");

            string commandText = SqlQuery.GetQuery("SelectPatientCaseList");

            if (sqlParameters.ContainsKey("procedure"))
            {
                commandText += "AND procedure=@procedure ";
            }
            commandText += "ORDER BY create_date DESC";

            return _databaseService.GetDatas<PatientCase>(commandText, sqlParameters);
        }

        public IList<PatientCase> SelectPatientCaseByList(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientCaseByList");

            string commandText;

            commandText = SqlQuery.GetQuery("SelectPatientCaseByList");

            List<string> ids = (List<string>)sqlParameters["ids"];
            string commandTextExtra = "WHERE id IN (''";
            for (int i = 0; i < ids.Count; i++)
            {
                commandTextExtra += ", '" + ids[i] + "'";
            }
            commandTextExtra += ") ";
            commandTextExtra += "ORDER BY patient_id, create_date DESC";

            commandText = commandText + commandTextExtra;

            return _databaseService.GetDatas<PatientCase>(commandText, sqlParameters);
        }

        public int DeletePatientCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("DeletePatientCase");

            string commandText = SqlQuery.GetQuery("DeletePatientCase");

            return _databaseService.DeleteData(commandText, sqlParameters);
        }

        public int InsertPatientCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertPatientCase");

            string commandText = SqlQuery.GetQuery("InsertPatientCase");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdatePatientCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCase");

            string commandText = SqlQuery.GetQuery("UpdatePatientCase");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int UpsertPatientCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpsertPatientCase");

            string commandText = SqlQuery.GetQuery("UpsertPatientCase");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public IList<StringModel> SelectPatientCaseAnnotation(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientCaseAnnotation");

            string commandText = SqlQuery.GetQuery("SelectPatientCaseAnnotation");

            return _databaseService.GetDatas<StringModel>(commandText, sqlParameters);
        }

        /**
         * Patient Case Preset
         */
        public IList<PatientCasePreset> SelectPatientCasePresetList()
        {
            _log.Debug("SelectPatientCasePresetList");

            string commandText = SqlQuery.GetQuery("SelectPatientCasePresetList");

            return _databaseService.GetDatas<PatientCasePreset>(commandText);
        }

        public int DeletePatientCasePreset(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("DeletePatientCasePreset");

            string commandText = SqlQuery.GetQuery("DeletePatientCasePreset");

            return _databaseService.DeleteData(commandText, sqlParameters);
        }

        public int InsertPatientCasePreset(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertPatientCasePreset");

            string commandText = SqlQuery.GetQuery("InsertPatientCasePreset");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdatePatientCasePreset(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCasePreset");

            string commandText = SqlQuery.GetQuery("UpdatePatientCasePreset");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        /**
         * Pysician
         */
        public IList<Physician> SelectPhysicianList()
        {
            _log.Debug("SelectPhysicianList");

            string commandText = SqlQuery.GetQuery("SelectPhysicianList");

            return _databaseService.GetDatas<Physician>(commandText);
        }

        public int InsertPhysician(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertPhysician");

            string commandText = SqlQuery.GetQuery("InsertPhysician");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int DeletePhysician()
        {
            _log.Debug("DeletePhysician");

            string commandText = SqlQuery.GetQuery("DeletePhysician");

            return _databaseService.DeleteData(commandText);
        }

        public int UpsertPatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpsertPatient");

            string commandText = SqlQuery.GetQuery("UpsertPatient");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        /**
         * Extra
         */
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
