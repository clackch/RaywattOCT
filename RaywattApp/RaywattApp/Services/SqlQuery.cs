using log4net;
using System.Collections.Generic;

namespace RaywattApp.Services
{
    public static class SqlQuery
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SqlQuery));

        private static Dictionary<string, string> _query = new Dictionary<string, string>();

        static SqlQuery()
        {
            _log.Debug("SqlQuery");

            SetSelectQuery();
            SetInsertQuery();
            SetUpdateQuery();
            SetDeleteQuery();
        }

        public static string GetQuery(string key)
        {
            _log.Debug("GetQuery : " + key);

            return _query[key];
        }

        public static string GetCountQuery(string key)
        {
            _log.Debug("GetCountQuery : " + key);

            return $"SELECT count(*) FROM (" + _query[key] + ") t"; 
        }

        private static void SetSelectQuery()
        {
            _log.Debug("SetSelectQuery");

            //SelectPatientList
            _query["SelectPatientList"] =
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name" +
                        $", birthdate, to_char(birthdate,'YYYY-MM-DD') strbirthdate, rv_schema.fn_code('GEND', gender) gender" +
                        $", to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') update_date " +
                        $", rv_schema.fn_lastcase(id) last_case " +
                $"FROM rv_schema.patient " +
                $"WHERE id LIKE @id OR lastname LIKE @lastname OR firstname LIKE @firstname";

            //SelectPatientListByCase
            _query["SelectPatientListByCase"] =
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, rv_schema.fn_code('GEND', gender) gender" +
                        $", to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') update_date " +
                        $", rv_schema.fn_lastcase(id) last_case " +
                $"FROM rv_schema.patient p " +
                $"WHERE (SELECT count(*) FROM rv_schema.patient_case WHERE patient_id = p.id) > 0 " +
                $"ORDER BY id";

            //SelectPatient
            _query["SelectPatient"] = 
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, rv_schema.fn_code('GEND', gender) gender" +
                        $", to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') update_date " +
                $"FROM rv_schema.patient " +
                $"WHERE id = @id ";

            //SelectPatientByList
            _query["SelectPatientByList"] =
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, rv_schema.fn_code('GEND', gender) gender" +
                        $", to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') update_date " +
                $"FROM rv_schema.patient ";

            //SelectCodeList
            _query["SelectCodeList"] =
                $"SELECT classification, key, value, buffer1, buffer2 " +
                $"FROM rv_schema.code " +
                $"ORDER BY classification, sort_order, key";

            //SelectPatientCaseByDate - create_data 기준
            _query["SelectPatientCaseByDate"] =
                $"SELECT to_char(create_date, 'YYYY-MM-DD') key, concat(to_char(create_date, 'YYYY-MM-DD'), ' (', count(*), ')' ) value " +
                $"FROM rv_schema.patient_case " +
                $"WHERE patient_id = @id " +
                $"GROUP BY key ";

            //SelectPatientCaseListByDate - create_data 기준
            _query["SelectPatientCaseListByDate"] =
                $"SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name , physician_name, " +
                        $"accession_number, accession_name, comment, " +
                        $"rv_schema.fn_code('VESS', vessel) vessel, rv_schema.fn_code('PROC', procedure) procedure, " +
                        $"thumbnail_no, still_image_yn, image, " +
                        $"to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') update_date " +
                $"FROM rv_schema.patient_case " +
                $"WHERE patient_id = @id AND to_char(create_date, 'YYYY-MM-DD') = @date " +
                $"ORDER BY create_date DESC";

            //SelectPatientCaseList - create_data 기준
            _query["SelectPatientCaseList"] =
                $"SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name , physician_name, " +
                        $"accession_number, accession_name, comment, " +
                        $"rv_schema.fn_code('VESS', vessel) vessel, rv_schema.fn_code('PROC', procedure) procedure, " +
                        $"thumbnail_no, still_image_yn, image, " +
                        $"to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') update_date " +
                $"FROM rv_schema.patient_case " +
                $"WHERE patient_id = @id " +
                $"ORDER BY create_date DESC";

            //SelectPhysicianList
            _query["SelectPhysicianList"] =
                $"SELECT name, to_char(create_date,'YYYY-MM-DD HH24:MI:SS') create_date " +
                $"FROM rv_schema.physician " +
                $"ORDER BY name";
        }

        private static void SetInsertQuery()
        {
            _log.Debug("SetInsertQuery");

            //InsertPatient
            _query["InsertPatient"] =
                $"INSERT INTO rv_schema.patient(id, lastname, firstname, birthdate, gender, create_date, update_date) " +
                $"VALUES (@id, @lastname, @firstname, @birthdate, @gender, now(), now())";

            //InsertPatientCase
            _query["InsertPatientCase"] =
                $"INSERT INTO rv_schema.patient_case(id, patient_id, physician_name, accession_number, accession_name, comment, vessel, procedure, thumbnail_no, still_image_yn, create_date, update_date) " +
                $"VALUES (@id, @patient_id, @physician_name, @accession_number, @accession_name, @comment, @vessel, @procedure, @thumbnail_no, @still_image_yn, now(), now())";

            //InsertPhysician
            _query["InsertPhysician"] =
                $"INSERT INTO rv_schema.physician(name, create_date) " +
                $"VALUES (@name, now())";
        }

        private static void SetUpdateQuery()
        {
            _log.Debug("SetUpdateQuery");

            //UpdatePatient
            _query["UpdatePatient"] =
                $"UPDATE rv_schema.patient " +
                $"SET id=@id, lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, update_date=now() " +
                $"WHERE id=@originId";

            //UpdatePatientCase
            _query["UpdatePatientCase"] =
                $"UPDATE rv_schema.patient_case " +
                $"SET physician_name=@physician_name, accession_number=@accession_number, comment=@comment, vessel=@vessel, procedure=@procedure, update_date=now() " +
                $"WHERE id=@id";
        }

        private static void SetDeleteQuery()
        {
            _log.Debug("SetDeleteQuery");

            //DeletePatientCase
            _query["DeletePatientCase"] =
                $"DELETE FROM rv_schema.patient_case " +
                $"WHERE id=@id";

            //DeletePhysician
            _query["DeletePhysician"] =
                $"DELETE FROM rv_schema.physician";
        }
    }
}
