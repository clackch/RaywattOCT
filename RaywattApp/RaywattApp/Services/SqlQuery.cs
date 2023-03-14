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
            SetUpsertQuery();
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

            //SelectCodeList
            _query["SelectCodeList"] =
                $"SELECT classification, key, value, buffer1, buffer2 " +
                $"FROM rv_schema.code " +
                $"ORDER BY classification, sort_order, key";

            //SelectL10n
            _query["SelectL10n"] =
                $"SELECT lang, choice " +
                $"FROM rv_schema.l10n " +
                $"WHERE choice = TRUE";

            //SelectL10nList
            _query["SelectL10nList"] =
                $"SELECT lang, choice " +
                $"FROM rv_schema.l10n " +
                $"ORDER BY lang";

            //SelectDicomPropertyList
            _query["SelectDicomPropertyList"] =
                $"SELECT tag return_string, value return_string2 " +
                $"FROM rv_schema.dicom_property " +
                $"ORDER BY tag";

            //SelectPatientList
            _query["SelectPatientList"] =
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender" +
                        $", create_date, update_date " +
                        $", rv_schema.fn_lastcase(id) last_case, rv_schema.fn_displayLastcase(id) display_last_case " +
                $"FROM rv_schema.patient " +
                $"WHERE id LIKE @id OR lastname LIKE @lastname OR firstname LIKE @firstname";

            //SelectPatientListByCase
            _query["SelectPatientListByCase"] =
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender" +
                        $", create_date, update_date " +
                        $", rv_schema.fn_lastcase(id) last_case " +
                $"FROM rv_schema.patient p " +
                $"WHERE (SELECT count(*) FROM rv_schema.patient_case WHERE patient_id = p.id) > 0 " +
                $"ORDER BY id";

            //SelectPatient
            _query["SelectPatient"] = 
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender" +
                        $", create_date, update_date " +
                $"FROM rv_schema.patient " +
                $"WHERE LOWER(id) = LOWER(@id) ";

            //SelectPatientByList
            _query["SelectPatientByList"] =
                $"SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender" +
                        $", create_date, update_date " +
                $"FROM rv_schema.patient ";

            //SelectPatientCaseByDate - create_data 기준
            _query["SelectPatientCaseByDate"] =
                $"SELECT to_char(create_date, 'YYYY-MM-DD') key" +
                $", concat(rv_schema.fn_datel10n(TO_DATE(to_char(create_date, 'YYYY-MM-DD'), 'YYYY-MM-DD')), ' (', count(*), ')' ) value " +
                $"FROM rv_schema.patient_case " +
                $"WHERE patient_id = @id " +
                $"GROUP BY key ";

            //SelectPatientCaseListByDate - create_data 기준
            _query["SelectPatientCaseListByDate"] =
                $"SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name, physician_name" +
                        $", accession_number, accession_name, comment" +
                        $", vessel, procedure" +
                        $", thumbnail_no, still_image_yn, image" +
                        $", pullback_type, angio_co_registration, indicator_degree" +
                        $", preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold" +
                        $", create_date, update_date " +
                $"FROM rv_schema.patient_case " +
                $"WHERE patient_id = @id AND to_char(create_date, 'YYYY-MM-DD') = @date " +
                $"ORDER BY create_date DESC";

            //SelectPatientCaseList - create_data 기준
            _query["SelectPatientCaseList"] =
                $"SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name, physician_name" +
                        $", accession_number, accession_name, comment" +
                        $", vessel, procedure" +
                        $", thumbnail_no, still_image_yn, image" +
                        $", pullback_type, angio_co_registration, indicator_degree" +
                        $", preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold" +
                        $", create_date, update_date " +
                $"FROM rv_schema.patient_case " +
                $"WHERE patient_id = @id ";                

            //SelectPatientCaseByList
            _query["SelectPatientCaseByList"] =
                $"SELECT id, patient_id, physician_name, accession_number, accession_name, comment, vessel, procedure, thumbnail_no, still_image_yn, image" +
                        $", rv_schema.fn_patient(patient_id) patient_name" +
                        $", rv_schema.fn_patient_gender(patient_id) gender, rv_schema.fn_patient_birth(patient_id) birthdate" +
                        $", pullback_type, angio_co_registration, indicator_degree" +
                        $", preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold, measurements, bookmarks" +
                        $", create_date, update_date " +
                $"FROM rv_schema.patient_case ";

            //SelectPhysicianList
            _query["SelectPhysicianList"] =
                $"SELECT name, create_date " +
                $"FROM rv_schema.physician " +
                $"ORDER BY name";

            //SelectPatientCasePresetList
            _query["SelectPatientCasePresetList"] =
                $"SELECT id, preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold, default_set, create_date, update_date " +
                $"FROM rv_schema.patient_case_preset " +
                $"ORDER BY default_set DESC, preset_name";

            //SelectPatientCaseAnnotation
            _query["SelectPatientCaseAnnotation"] =
                $"SELECT measurements return_string, bookmarks return_string2 " +
                $"FROM rv_schema.patient_case " +
                $"WHERE id = @id ";
        }

        private static void SetInsertQuery()
        {
            _log.Debug("SetInsertQuery");

            //InsertPatient
            _query["InsertPatient"] =
                $"INSERT INTO rv_schema.patient(id, lastname, firstname, birthdate, gender, create_date, update_date) " +
                $"VALUES (@id, @lastname, @firstname, @birthdate, @gender, now(), now())";

            //InsertPatientWithoutBirth
            _query["InsertPatientWithoutBirth"] =
                $"INSERT INTO rv_schema.patient(id, lastname, firstname, gender, create_date, update_date) " +
                $"VALUES (@id, @lastname, @firstname, @gender, now(), now())";

            //InsertPatientCase
            _query["InsertPatientCase"] =
                $"INSERT INTO rv_schema.patient_case(id, patient_id, physician_name, accession_number, accession_name" +
                                                    $", comment, vessel, procedure, thumbnail_no, still_image_yn, image" +
                                                    $", pullback_type, angio_co_registration, indicator_degree" +
                                                    $", preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold" +
                                                    $", create_date, update_date) " +
                $"VALUES (@id, @patient_id, @physician_name, @accession_number, @accession_name" +
                        $", @comment, @vessel, @procedure, @thumbnail_no, @still_image_yn, @image" +
                        $", @pullback_type, @angio_co_registration, @indicator_degree" +
                        $", @preset_name, @calcium_threshold, @expansion_calculation, @expansion_threshold, @apposition_threshold" +
                        $", now(), now())";

            //InsertPhysician
            _query["InsertPhysician"] =
                $"INSERT INTO rv_schema.physician(name, create_date) " +
                $"VALUES (@name, now())";

            //InsertPatientCasePreset
            _query["InsertPatientCasePreset"] =
                $"INSERT INTO rv_schema.patient_case_preset(id, preset_name, calcium_threshold, expansion_calculation, expansion_threshold" +
                                                        $", apposition_threshold, default_set, create_date, update_date) " +
                $"VALUES (@id, @preset_name, @calcium_threshold, @expansion_calculation, @expansion_threshold, @apposition_threshold, FALSE, now(), now())";
        }

        private static void SetUpdateQuery()
        {
            _log.Debug("SetUpdateQuery");

            //UpdateL10n
            _query["UpdateL10n"] =
                $"UPDATE rv_schema.l10n " +
                $"SET choice = CASE WHEN lang = @lang THEN TRUE ELSE FALSE END";

            //UpdatePatient
            _query["UpdatePatient"] =
                $"UPDATE rv_schema.patient " +
                $"SET id=@id, lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, update_date=now() " +
                $"WHERE id=@originId";

            //UpdatePatientWithoutBirth
            _query["UpdatePatientWithoutBirth"] =
                $"UPDATE rv_schema.patient " +
                $"SET id=@id, lastname=@lastname, firstname=@firstname, gender=@gender, update_date=now() " +
                $"WHERE id=@originId";

            //UpdatePatientCase
            _query["UpdatePatientCase"] =
                $"UPDATE rv_schema.patient_case " +
                $"SET physician_name=@physician_name, accession_number=@accession_number" +
                    $", comment=@comment, vessel=@vessel, procedure=@procedure" +
                    $", angio_co_registration=@angio_co_registration, indicator_degree=@indicator_degree" +
                    $", preset_name=@preset_name, calcium_threshold=@calcium_threshold, expansion_calculation=@expansion_calculation" +
                    $", expansion_threshold=@expansion_threshold, apposition_threshold=@apposition_threshold" +
                    $", measurements=@measurements" +
                    $", bookmarks=@bookmarks" +
                    $", update_date=now() " +
                $"WHERE id=@id";

            //UpdatePatientCasePreset
            _query["UpdatePatientCasePreset"] =
                $"UPDATE rv_schema.patient_case_preset " +
                $"SET preset_name=@preset_name, calcium_threshold=@calcium_threshold, expansion_calculation=@expansion_calculation" +
                $", expansion_threshold=@expansion_threshold, apposition_threshold=@apposition_threshold, update_date=now() " +
                $"WHERE id=@id";
        }

        private static void SetDeleteQuery()
        {
            _log.Debug("SetDeleteQuery");

            //DeletePatient
            _query["DeletePatient"] =
                $"DELETE FROM rv_schema.patient " +
                $"WHERE id=@id";

            //DeletePatientCase
            _query["DeletePatientCase"] =
                $"DELETE FROM rv_schema.patient_case " +
                $"WHERE id=@id";

            //DeletePhysician
            _query["DeletePhysician"] =
                $"DELETE FROM rv_schema.physician";

            //DeletePatientCasePreset
            _query["DeletePatientCasePreset"] =
                $"DELETE FROM rv_schema.patient_case_preset " +
                $"WHERE id=@id";
        }

        private static void SetUpsertQuery()
        {
            _log.Debug("SetUpsertQuery");

            //UpsertPatient
            _query["UpsertPatient"] =
                $"INSERT INTO rv_schema.patient(id, lastname, firstname, birthdate, gender, create_date, update_date) " +
                $"VALUES (@id, @lastname, @firstname, @birthdate, @gender, @create_date, @update_date) " +
                $"ON CONFLICT (id) " +
                $"DO UPDATE " +
                $"SET lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, create_date=@create_date, update_date=@update_date";

            //UpsertPatientCase
            _query["UpsertPatientCase"] =
                $"INSERT INTO rv_schema.patient_case(id, patient_id, physician_name, accession_number, accession_name, comment, vessel, procedure" +
                                                    $", thumbnail_no, still_image_yn, image, pullback_type, angio_co_registration, indicator_degree" +
                                                    $", preset_name, calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold" +
                                                    $", measurements, bookmarks, create_date, update_date) " +
                $"VALUES (@id, @patient_id, @physician_name, @accession_number, @accession_name, @comment, @vessel, @procedure" +
                                                    $", @thumbnail_no, @still_image_yn, @image, @pullback_type, @angio_co_registration, @indicator_degree" +
                                                    $", @preset_name, @calcium_threshold, @expansion_calculation, @expansion_threshold, @apposition_threshold" +
                                                    $", @measurements, @bookmarks, @create_date, @update_date) " +
                $"ON CONFLICT (id) " +
                $"DO UPDATE " +
                $"SET patient_id=@patient_id, physician_name=@physician_name, accession_number=@accession_number, comment=@comment" +
                    $", vessel=@vessel, procedure=@procedure, thumbnail_no=@thumbnail_no, still_image_yn=@still_image_yn, image=@image" +
                    $", pullback_type=@pullback_type, angio_co_registration=@angio_co_registration, indicator_degree=@indicator_degree, preset_name=@preset_name" +
                    $", calcium_threshold=@calcium_threshold, expansion_calculation=@expansion_calculation, expansion_threshold=@expansion_threshold" +
                    $", apposition_threshold=@apposition_threshold, measurements=@measurements, bookmarks=@bookmarks" +
                    $", create_date=@create_date, update_date=@update_date";
        }
    }
}
