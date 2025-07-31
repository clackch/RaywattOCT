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

        public IList<Code> SelectCode(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectCode");

            string commandText = SqlQuery.GetQuery("SelectCode");

            return _databaseService.GetDatas<Code>(commandText, sqlParameters);
        }

        /**
         * Configuration
         */
        public IList<Configuration> SelectConfigurationL10n()
        {
            _log.Debug("SelectConfigurationL10n");

            string commandText = SqlQuery.GetQuery("SelectConfigurationL10n");

            return _databaseService.GetDatas<Configuration>(commandText);
        }

        public IList<Configuration> SelectConfiguration(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectConfiguration");

            string commandText = SqlQuery.GetQuery("SelectConfiguration");

            return _databaseService.GetDatas<Configuration>(commandText, sqlParameters);
        }

        public int UpdateConfigurationL10n(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdateConfigurationL10n");

            string commandText = SqlQuery.GetQuery("UpdateConfigurationL10n");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int UpdateConfiguration(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdateConfiguration");

            string commandText = SqlQuery.GetQuery("UpdateConfiguration");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        /**
         * Dicom Property
         */
        public IList<StringModel> SelectDicomPropertyList()
        {
            _log.Debug("SelectDicomPropertyList");

            string commandText = SqlQuery.GetQuery("SelectDicomPropertyList");

            return _databaseService.GetDatas<StringModel>(commandText);
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

            string commandText;

            if (sqlParameters["birthdate"] == null)
            {
                commandText = SqlQuery.GetQuery("InsertPatientWithoutBirth");
            }
            else
            {
                commandText = SqlQuery.GetQuery("InsertPatient");
            }            

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdatePatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatient");

            string commandText;

            if (sqlParameters["birthdate"] == null)
            {
                commandText = SqlQuery.GetQuery("UpdatePatientWithoutBirth");
            }
            else
            {
                commandText = SqlQuery.GetQuery("UpdatePatient");
            }

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int DeletePatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("DeletePatient");

            string commandText = SqlQuery.GetQuery("DeletePatient");

            return _databaseService.DeleteData(commandText, sqlParameters);
        }

        public int UpsertPatient(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpsertPatient");

            string commandText = SqlQuery.GetQuery("UpsertPatient");

            return _databaseService.InsertData(commandText, sqlParameters);
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
            string commandTextExtra = "WHERE T1.id IN (''";
            for (int i = 0; i < ids.Count; i++)
            {
                commandTextExtra += ", '" + ids[i] + "'";
            }
            commandTextExtra += ") ";
            commandTextExtra += "ORDER BY patient_id, T1.create_date DESC";

            commandText = commandText + commandTextExtra;

            return _databaseService.GetDatas<PatientCase>(commandText, sqlParameters);
        }

        public IList<PatientCase> SelectPrePatientCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPrePatientCase");

            string commandText = SqlQuery.GetQuery("SelectPrePatientCase");

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

        public int UpdatePatientCaseId(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCaseId");

            string commandText = SqlQuery.GetQuery("UpdatePatientCaseId");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int UpsertPatientCase(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpsertPatientCase");

            string commandText = SqlQuery.GetQuery("UpsertPatientCase");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        /**
         * Patient Case Annotation
         */
        public IList<PatientCaseAnnotation> SelectPatientCaseAnnotation(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientCaseAnnotation");

            string commandText = SqlQuery.GetQuery("SelectPatientCaseAnnotation");

            return _databaseService.GetDatas<PatientCaseAnnotation>(commandText, sqlParameters);
        }

        public int UpdatePatientCaseAnnotationLumenContour(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCaseAnnotationLumenContour");

            string commandText = SqlQuery.GetQuery("UpdatePatientCaseAnnotationLumenContour");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int UpdatePatientCaseAnnotationWithoutLumenContour(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCaseAnnotationWithoutLumenContour");

            string commandText = SqlQuery.GetQuery("UpdatePatientCaseAnnotationWithoutLumenContour");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int UpsertPatientCaseAnnotation(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpsertPatientCaseAnnotation");

            string commandText = SqlQuery.GetQuery("UpsertPatientCaseAnnotation");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public IList<StringModel> SelectPatientCaseFfrPlaque(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPatientCaseFfrPlaque");

            string commandText = SqlQuery.GetQuery("SelectPatientCaseFfrPlaque");

            return _databaseService.GetDatas<StringModel>(commandText, sqlParameters);
        }

        public int UpdatePatientCaseFfrPlaque(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCaseFfrPlaque");

            string commandText = SqlQuery.GetQuery("UpdatePatientCaseFfrPlaque");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        /**
         * Pysician
         */
        public IList<Physician> SelectPhysician(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPhysician");

            string commandText = SqlQuery.GetQuery("SelectPhysician");

            return _databaseService.GetDatas<Physician>(commandText, sqlParameters);
        }

        public IList<Physician> SelectPhysicianList(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectPhysicianList");

            string commandText = SqlQuery.GetQuery("SelectPhysicianList");

            Dictionary<string, Object> commandParameters = new Dictionary<string, Object>();
            commandParameters["lastname"] = "%" + sqlParameters["SearchKeyword"] + "%";
            commandParameters["firstname"] = "%" + sqlParameters["SearchKeyword"] + "%";

            return _databaseService.GetDatas<Physician>(commandText, commandParameters);
        }

        public int InsertPhysician(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertPhysician");

            string commandText = SqlQuery.GetQuery("InsertPhysician");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdatePhysician(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePhysician");

            string commandText = SqlQuery.GetQuery("UpdatePhysician");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int DeletePhysician(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("DeletePhysician");

            string commandText = SqlQuery.GetQuery("DeletePhysician");

            return _databaseService.DeleteData(commandText, sqlParameters);
        }

        /**
         * Cath Room
         */
        public IList<CathRoom> SelectCathRoomList()
        {
            _log.Debug("SelectCathRoomList");

            string commandText = SqlQuery.GetQuery("SelectCathRoomList");

            return _databaseService.GetDatas<CathRoom>(commandText);
        }

        /**
         * CoRegistration
         */
        public int UpsertCoRegistration(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpsertCoRegistration");

            string commandText = SqlQuery.GetQuery("UpsertCoRegistration");

            return _databaseService.InsertData(commandText, sqlParameters);
        }        

        public IList<PatientCaseAnnotation> SelectCoRegistration(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectCoRegistration");

            string commandText = SqlQuery.GetQuery("SelectCoRegistration");

            return _databaseService.GetDatas<PatientCaseAnnotation>(commandText, sqlParameters);
        }

        public int UpdatePatientCaseAngioCoRegistration(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePatientCaseAngioCoRegistration");

            string commandText = SqlQuery.GetQuery("UpdatePatientCaseAngioCoRegistration");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        /**
         * Dicom Server
         */
        public IList<DicomServer> SelectDicomServer(Dictionary<string, Object> sqlParameters = null)
        {
            _log.Debug("SelectDicomServer");

            string commandText;

            if (sqlParameters == null)
            {
                commandText = SqlQuery.GetQuery("SelectDicomServer");
            }
            else
            {
                if (sqlParameters.ContainsKey("server_type"))
                    commandText = SqlQuery.GetQuery("SelectDicomServerByType");

                else if (sqlParameters.ContainsKey("id"))
                    commandText = SqlQuery.GetQuery("SelectDicomServerExcludeId");

                else
                    commandText = SqlQuery.GetQuery("SelectDicomServer");
            }                

            return _databaseService.GetDatas<DicomServer>(commandText, sqlParameters);
        }

        public int InsertDicomServer(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertDicomServer");

            string commandText = SqlQuery.GetQuery("InsertDicomServer");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdateDicomServer(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdateDicomServer");

            string commandText = SqlQuery.GetQuery("UpdateDicomServer");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int DeleteDicomServer(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("DeleteDicomServer");

            string commandText = SqlQuery.GetQuery("DeleteDicomServer");

            return _databaseService.DeleteData(commandText, sqlParameters);
        }

        /**
         * User
         */
        public IList<User> SelectUserList()
        {
            _log.Debug("SelectUserList");

            string commandText = SqlQuery.GetQuery("SelectUserList");

            return _databaseService.GetDatas<User>(commandText);
        }

        public IList<User> SelectAdmin(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectAdmin");

            string commandText = SqlQuery.GetQuery("SelectAdmin");

            return _databaseService.GetDatas<User>(commandText, sqlParameters);
        }

        public IList<User> SelectUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("SelectUser");

            string commandText = SqlQuery.GetQuery("SelectUser");

            return _databaseService.GetDatas<User>(commandText, sqlParameters);
        }

        public int UpdateTermsAgreedDateUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdateTermsAgreedDateUser");

            string commandText = SqlQuery.GetQuery("UpdateTermsAgreedDateUser");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int CountUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("CountUser");

            string commandText = SqlQuery.GetCountQuery("SelectUser");

            return _databaseService.GetDataCount(commandText, sqlParameters);
        }

        public int InsertUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("InsertUser");

            string commandText = SqlQuery.GetQuery("InsertUser");

            return _databaseService.InsertData(commandText, sqlParameters);
        }

        public int UpdateUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdateUser");

            string commandText = SqlQuery.GetQuery("UpdateUser");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int ResetPasswordUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("ResetPasswordUser");

            string commandText = SqlQuery.GetQuery("ResetPasswordUser");

            return _databaseService.UpdateData(commandText, sqlParameters);
        }

        public int DeleteUser(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("DeleteUser");

            string commandText = SqlQuery.GetQuery("DeleteUser");

            return _databaseService.DeleteData(commandText, sqlParameters);
        }

        /*
        Password
        */
        public bool UpdatePasswordReset(Dictionary<string, Object> sqlParameters)
        {
            _log.Debug("UpdatePasswordReset");

            var commandText = SqlQuery.GetQuery("UpdatePasswordReset");

            _databaseService.UpdateData(commandText, sqlParameters);

            return true;
        }

        /**
         * Extra
         */
        private static string getAdditionalCondition(Dictionary<string, Object> sqlAdditionalCondition)
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
