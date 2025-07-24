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
            _query["SelectCodeList"] = @$"
                SELECT classification, key, value
                FROM rv_schema.code
                ORDER BY classification, sort_order
                ";

            //SelectCode
            _query["SelectCode"] = @$"
                SELECT classification, key, value, buffer1, buffer2
                FROM rv_schema.code
                WHERE classification=@classification
                ORDER BY sort_order
                ";

            //SelectConfigurationL10n
            _query["SelectConfigurationL10n"] = @$"
                SELECT key
                FROM rv_schema.configuration
                WHERE classification = 'L10N' AND value = 'Y'
                ";

            //SelectConfiguration
            _query["SelectConfiguration"] = @$"
                SELECT classification, key, value, buffer
                FROM rv_schema.configuration
                WHERE classification = @classification
                ORDER BY key
                ";

            //SelectDicomPropertyList
            _query["SelectDicomPropertyList"] = @$"
                SELECT tag return_string, value return_string2
                FROM rv_schema.dicom_property
                ORDER BY tag
                ";

            //SelectPatientList
            _query["SelectPatientList"] = @$"
                SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender
                , physician_id, rv_schema.fn_physician(physician_id) physician_name
                , create_date, update_date
                , rv_schema.fn_lastcase(id) last_case, rv_schema.fn_displayLastcase(id) display_last_case
                FROM rv_schema.patient
                WHERE id LIKE @id OR lastname LIKE @lastname OR firstname LIKE @firstname
                ";

            //SelectPatientListByCase
            _query["SelectPatientListByCase"] = @$"
                SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender, FALSE is_checked, create_date, update_date
                , rv_schema.fn_lastcase(id) last_case
                FROM rv_schema.patient p
                WHERE (SELECT count(*) FROM rv_schema.patient_case WHERE patient_id = p.id) > 0
                ORDER BY id
                ";

            //SelectPatient
            _query["SelectPatient"] =  @$"
                SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender, create_date, update_date
                FROM rv_schema.patient
                WHERE LOWER(id) = LOWER(@id)
                ";

            //SelectPatientByList
            _query["SelectPatientByList"] = @$"
                SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name, birthdate, gender, create_date, update_date
                FROM rv_schema.patient
                ";

            //SelectPatientCaseByDate - create_data 기준
            _query["SelectPatientCaseByDate"] = @$"
                SELECT to_char(create_date, 'YYYY-MM-DD') key
                , concat(rv_schema.fn_datel10n(TO_DATE(to_char(create_date, 'YYYY-MM-DD'), 'YYYY-MM-DD')), ' (', count(*), ')' ) value
                FROM rv_schema.patient_case
                WHERE patient_id = @id
                GROUP BY key
                ";

            //SelectPatientCaseListByDate - create_data 기준  Radius
            _query["SelectPatientCaseListByDate"] = @$"
                SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name, physician_name
                , accession_number, comment
                , vessel, location, procedure
                , num_of_frames, image, image_resolution, z_offset, field_of_view
                , pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                , flush_media, pullback_trigger, colormap, guidewire_radius
                , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                , brightness, contrast, sheath_diameter, section_proximal, section_distal
                , create_date, update_date
                FROM rv_schema.patient_case
                WHERE patient_id = @id AND to_char(create_date, 'YYYY-MM-DD') = @date
                ORDER BY create_date DESC
                ";

            //SelectPatientCaseList - create_data 기준
            _query["SelectPatientCaseList"] = @$"
                SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name, physician_name
                , accession_number, comment
                , vessel, location, procedure
                , num_of_frames, image, image_resolution, z_offset, field_of_view
                , pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                , flush_media, pullback_trigger, colormap, guidewire_radius
                , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                , brightness, contrast, sheath_diameter, section_proximal, section_distal
                , create_date, update_date
                FROM rv_schema.patient_case
                WHERE patient_id = @id
                ";

            //SelectPatientCaseByList
            _query["SelectPatientCaseByList"] = @$"
                SELECT T1.id, patient_id, physician_name, accession_number, comment, vessel, location, procedure, num_of_frames, image, image_resolution, z_offset, field_of_view
                , rv_schema.fn_patient(patient_id) patient_name
                , rv_schema.fn_patient_gender(patient_id) gender, rv_schema.fn_patient_birth(patient_id) birthdate
                , pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                , flush_media, pullback_trigger, colormap, guidewire_radius
                , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                , brightness, contrast, sheath_diameter, section_proximal, section_distal
                , T1.create_date, T1.update_date
                , bookmark, longitude, cross_section
                , lumen_contour as str_lumen_contour, lumen_sidebranch as str_lumen_sidebranch, lumen_stent as str_lumen_stent, lumen_guidewire as str_lumen_guidewire
                , ffr_plaque, co_registration as str_co_registration
                FROM rv_schema.patient_case T1 LEFT JOIN rv_schema.patient_case_annotation T2 ON T1.id = T2.id
                ";

            //SelectPrePatientCase
            _query["SelectPrePatientCase"] = @$"
                SELECT A.* 
                FROM (
                    SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name, physician_name
                    , accession_number, comment
                    , vessel, location, procedure
                    , num_of_frames, image, image_resolution, z_offset, field_of_view
                    , pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                    , flush_media, pullback_trigger, colormap
                    , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                    , brightness, contrast, sheath_diameter, section_proximal, section_distal
                    , create_date, update_date
                    FROM rv_schema.patient_case
                    WHERE patient_id = @id
                    AND procedure='$001'
                    AND create_date < @create_date
                    ORDER BY create_date DESC) A
                UNION ALL
                SELECT B.* 
                FROM (
                    SELECT id, patient_id, rv_schema.fn_patient(patient_id) patient_name, physician_name
                    , accession_number, comment
                    , vessel, location, procedure
                    , num_of_frames, image, image_resolution, z_offset, field_of_view
                    , pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                    , flush_media, pullback_trigger, colormap
                    , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                    , brightness, contrast, sheath_diameter, section_proximal, section_distal
                    , create_date, update_date
                    FROM rv_schema.patient_case
                    WHERE patient_id = @id
                    AND procedure='$001'
                    AND create_date > @create_date
                    ORDER BY create_date ASC) B
                LIMIT 1
                ";

            //SelectPhysician
            _query["SelectPhysician"] = @$"
                SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name
                , flush_media, pullback_trigger, pullback_type, colormap
                , calcium_threshold, expansion_threshold, apposition_threshold
                , create_date, update_date
                FROM rv_schema.physician
                WHERE id=@id
                ";

            //SelectPhysicianList
            _query["SelectPhysicianList"] = @$"
                SELECT id, lastname, firstname, concat(firstname, ', ', lastname) name
                , flush_media, pullback_trigger, pullback_type, colormap
                , calcium_threshold, expansion_threshold, apposition_threshold
                , create_date, update_date
                FROM rv_schema.physician
                WHERE LOWER(lastname) LIKE LOWER(@lastname) OR LOWER(firstname) LIKE LOWER(@firstname)
                ORDER BY name
                ";

            //SelectPatientCaseAnnotation
            _query["SelectPatientCaseAnnotation"] = @$"
                SELECT id, bookmark, longitude, cross_section, lumen_contour, lumen_sidebranch, lumen_stent, lumen_guidewire, co_registration
                FROM rv_schema.patient_case_annotation
                WHERE id = @id
                ";

            //SelectPatientCaseFfrPlaque
            _query["SelectPatientCaseFfrPlaque"] = @$"
                SELECT ffr_plaque return_string
                FROM rv_schema.patient_case_annotation
                WHERE id = @id
                ";

            //SelectCathRoomList
            _query["SelectCathRoomList"] = @$"
                SELECT id, name, setup_chp, app_chp, rect_left, rect_top, rect_right, rect_bottom, description, create_date, update_date
                FROM rv_schema.cath_room
                ORDER BY name;
                ";

            //SelectCoRegistration
            _query["SelectCoRegistration"] = @$"
                SELECT id,  co_registration
                FROM rv_schema.patient_case_annotation
                WHERE id = @id
                ";

            //SelectDicomServer
            _query["SelectDicomServer"] = @$"
                SELECT id, ae_title, hostname, specify_ip_address, ip_address, port, tls_yn, server_type, comment, ca_file_path, create_date, update_date
                FROM rv_schema.dicom_server
                ORDER BY ae_title
                ";

            //SelectDicomServerByType
            _query["SelectDicomServerByType"] = @$"
                SELECT id, ae_title, hostname, specify_ip_address, ip_address, port, tls_yn, server_type, comment, ca_file_path, create_date, update_date
                FROM rv_schema.dicom_server
                WHERE server_type = @server_type
                ORDER BY ae_title
                ";

            //SelectUserList
            _query["SelectUserList"] = @$"
                SELECT id, password, comment, password_changed_at, password_reset, terms_agreed_at, create_date, update_date
                FROM rv_schema.users
                ORDER BY id
                ";

            //SelectUserList
            _query["SelectUserById"] = @$"
                SELECT id, admin, password, comment, password_changed_at, password_reset, terms_agreed_at, create_date, update_date
                FROM rv_schema.user
                WHERE id = @id and admin = @admin
                ";
        }

        private static void SetInsertQuery()
        {
            _log.Debug("SetInsertQuery");

            //InsertPatient
            _query["InsertPatient"] = @$"
                INSERT INTO rv_schema.patient(id, lastname, firstname, birthdate, gender, physician_id, create_date, update_date)
                VALUES (@id, @lastname, @firstname, @birthdate, @gender, @physician_id, now(), now())
                ";

            //InsertPatientWithoutBirth
            _query["InsertPatientWithoutBirth"] = @$"
                INSERT INTO rv_schema.patient(id, lastname, firstname, gender, physician_id, create_date, update_date)
                VALUES (@id, @lastname, @firstname, @gender, @physician_id, now(), now())
                ";

            //InsertPatientCase
            _query["InsertPatientCase"] = @$"
                INSERT INTO rv_schema.patient_case(id, patient_id, physician_name, accession_number
                , comment, vessel, location, procedure, num_of_frames, image, image_resolution, z_offset, field_of_view
                , pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                , flush_media, pullback_trigger, colormap, guidewire_radius
                , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                , brightness, contrast, sheath_diameter, section_proximal, section_distal
                , create_date, update_date)
                VALUES (@id, @patient_id, @physician_name, @accession_number
                , @comment, @vessel, @location, @procedure, @num_of_frames, @image, @image_resolution, @z_offset, @field_of_view
                , @pullback_type, @pullback_length, @angio_yn, @angio_co_registration, @indicator_degree
                , @flush_media, @pullback_trigger, @colormap, @guidewire_radius
                , @calcium_threshold, @expansion_calculation, @expansion_threshold, @apposition_threshold
                , @brightness, @contrast, @sheath_diameter, @section_proximal, @section_distal
                , now(), now())
                ";

            //InsertPhysician
            _query["InsertPhysician"] = @$"
                INSERT INTO rv_schema.physician(lastname, firstname
	            , flush_media, pullback_trigger, pullback_type, colormap
	            , calcium_threshold, expansion_threshold, apposition_threshold
	            , create_date, update_date)
	            VALUES (@lastname, @firstname
	            , @flush_media, @pullback_trigger, @pullback_type, @colormap
	            , @calcium_threshold, @expansion_threshold, @apposition_threshold
	            , now(), now())
                ";

            //InsertDicomServer
            _query["InsertDicomServer"] = @$"
                INSERT INTO rv_schema.dicom_server(ae_title, hostname, specify_ip_address
                , ip_address, port, tls_yn, server_type, comment, ca_file_path, create_date, update_date)
	            VALUES (@ae_title, @hostname, @specify_ip_address
                , @ip_address, @port, @tls_yn, @server_type, @comment, @ca_file_path
	            , now(), now())
                ";
        }

        private static void SetUpdateQuery()
        {
            _log.Debug("SetUpdateQuery");

            //UpdateConfigurationL10n
            _query["UpdateConfigurationL10n"] = @$"
                UPDATE rv_schema.configuration
                SET value = CASE WHEN key = @key THEN 'Y' ELSE 'N' END
                WHERE classification = 'L10N'
                ";

            //UpdateConfiguration
            _query["UpdateConfiguration"] = @$"
                UPDATE rv_schema.configuration
                SET value = @value, buffer = @buffer
                WHERE classification = @classification and key = @key
                ";

            //UpdatePatient
            _query["UpdatePatient"] = @$"
                UPDATE rv_schema.patient
                SET id=@id, lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, physician_id=@physician_id, update_date=now()
                WHERE id=@originId
                ";

            //UpdatePatientWithoutBirth
            _query["UpdatePatientWithoutBirth"] = @$"
                UPDATE rv_schema.patient
                SET id=@id, lastname=@lastname, firstname=@firstname, gender=@gender, physician_id=@physician_id, update_date=now()
                WHERE id=@originId
                ";

            //UpdatePatientCase
            _query["UpdatePatientCase"] = @$"
                UPDATE rv_schema.patient_case
                SET physician_name=@physician_name, accession_number=@accession_number
                , comment=@comment, vessel=@vessel, location=@location, procedure=@procedure
                , indicator_degree=@indicator_degree, guidewire_radius=@guidewire_radius
                , colormap=@colormap, z_offset=@z_offset, field_of_view=@field_of_view
                , calcium_threshold=@calcium_threshold, expansion_calculation=@expansion_calculation
                , expansion_threshold=@expansion_threshold, apposition_threshold=@apposition_threshold
                , brightness=@brightness, contrast=@contrast, section_proximal=@section_proximal, section_distal=@section_distal
                , update_date=now()
                WHERE id=@id
                ";

            //UpdatePatientCaseId
            _query["UpdatePatientCaseId"] = @$"
                UPDATE rv_schema.patient_case
                SET id = REGEXP_REPLACE(id, @originId, @id)
                WHERE patient_id = @id;
                ";

            //UpdatePatientCaseAnnotationLumenContour
            _query["UpdatePatientCaseAnnotationLumenContour"] = @$"
                UPDATE rv_schema.patient_case_annotation
                SET lumen_contour=@lumen_contour, lumen_stent=@lumen_stent
                WHERE id = @id
                ";

            //UpdatePatientCaseId
            _query["UpdatePatientCaseId"] = @$"
                UPDATE rv_schema.patient_case
                SET id = REGEXP_REPLACE(id, @originId, @id)
                WHERE patient_id = @id;
                ";

            //UpdatePatientCaseAnnotationWithoutLumenContour
            _query["UpdatePatientCaseAnnotationWithoutLumenContour"] = @$"
                UPDATE rv_schema.patient_case_annotation
                SET bookmark=@bookmark, longitude=@longitude, cross_section=@cross_section
                WHERE id = @id
                ";

            //UpdatePatientCaseFfrPlaque
            _query["UpdatePatientCaseFfrPlaque"] = @$"
                UPDATE rv_schema.patient_case_annotation
                SET ffr_plaque=@ffr_plaque
                WHERE id = @id
                ";

            //UpdatePhysician
            _query["UpdatePhysician"] = @$"
                UPDATE rv_schema.physician
	            SET lastname=@lastname, firstname=@firstname
	            , flush_media=@flush_media, pullback_trigger=@pullback_trigger, pullback_type=@pullback_type, colormap=@colormap
	            , calcium_threshold=@calcium_threshold, expansion_threshold=@expansion_threshold, apposition_threshold=@apposition_threshold
	            , update_date=now()
	            WHERE id=@id
                ";
              
            //UpdatePatientCaseAngioCoRegistration
            _query["UpdatePatientCaseAngioCoRegistration"] = @$"
                UPDATE rv_schema.patient_case
                SET angio_co_registration=@angio_co_registration
                WHERE id=@id
                ";

            //UpdateDicomServer
            _query["UpdateDicomServer"] = @$"
                UPDATE rv_schema.dicom_server
                SET ae_title=@ae_title, hostname=@hostname, specify_ip_address=@specify_ip_address
                , ip_address=@ip_address, port=@port, tls_yn=@tls_yn, server_type=@server_type, comment=@comment, ca_file_path=@ca_file_path, update_date=now()
                WHERE id=@id
                ";

            //UpdateUser
            _query["UpdateTermsAgreedDateUser"] = @$"
                UPDATE rv_schema.user
                SET terms_agreed_at=now()
                WHERE id=@id And password=@password and admin=@admin
                ";

            // UpdatePasswordReset
            _query["UpdatePasswordReset"] = @$"
                UPDATE rv_schema.user
                SET password_reset=@reset, password=@password, password_changed_at=now(), update_date=now()
                WHERE id=@id AND password=@before_password and admin=@admin
                ";
        }

        private static void SetDeleteQuery()
        {
            _log.Debug("SetDeleteQuery");

            //DeletePatient
            _query["DeletePatient"] = @$"
                DELETE FROM rv_schema.patient
                WHERE id=@id
                ";

            //DeletePatientCase
            _query["DeletePatientCase"] = @$"
                DELETE FROM rv_schema.patient_case
                WHERE id=@id
                ";

            //DeletePhysician
            _query["DeletePhysician"] = @$"
                DELETE FROM rv_schema.physician
                WHERE id=@id
                ";

            //DeleteDicomServer
            _query["DeleteDicomServer"] = @$"
                DELETE FROM rv_schema.dicom_server
                WHERE id=@id
                ";
        }

        private static void SetUpsertQuery()
        {
            _log.Debug("SetUpsertQuery");

            //UpsertPatient
            _query["UpsertPatient"] = @$"
                INSERT INTO rv_schema.patient(id, lastname, firstname, birthdate, gender, create_date, update_date)
                VALUES (@id, @lastname, @firstname, @birthdate, @gender, @create_date, @update_date)
                ON CONFLICT (id)
                DO UPDATE
                SET lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, create_date=@create_date, update_date=@update_date
                ";

            //UpsertPatientCase
            _query["UpsertPatientCase"] = @$"
                INSERT INTO rv_schema.patient_case(id, patient_id, physician_name, accession_number, comment, vessel, location, procedure
                , num_of_frames, image, image_resolution, z_offset, field_of_view, pullback_type, pullback_length, angio_yn, angio_co_registration, indicator_degree
                , flush_media, pullback_trigger, colormap, guidewire_radius
                , calcium_threshold, expansion_calculation, expansion_threshold, apposition_threshold
                , brightness, contrast, sheath_diameter, section_proximal, section_distal, create_date, update_date)
                VALUES (@id, @patient_id, @physician_name, @accession_number, @comment, @vessel, @location, @procedure
                , @num_of_frames, @image, @image_resolution, @z_offset, @field_of_view, @pullback_type, @pullback_length, @angio_yn, @angio_co_registration, @indicator_degree
                , @flush_media, @pullback_trigger, @colormap, @guidewire_radius
                , @calcium_threshold, @expansion_calculation, @expansion_threshold, @apposition_threshold
                , @brightness, @contrast, @sheath_diameter, @section_proximal, @section_distal, @create_date, @update_date)
                ON CONFLICT (id)
                DO UPDATE
                SET patient_id=@patient_id, physician_name=@physician_name, accession_number=@accession_number, comment=@comment
                , vessel=@vessel, location=@location, procedure=@procedure, num_of_frames=@num_of_frames, image=@image, image_resolution=@image_resolution, z_offset=@z_offset, field_of_view=@field_of_view
                , pullback_type=@pullback_type, pullback_length=@pullback_length, angio_yn=@angio_yn, angio_co_registration=@angio_co_registration
                , indicator_degree=@indicator_degree, guidewire_radius=@guidewire_radius
                , flush_media=@flush_media, pullback_trigger=@pullback_trigger, colormap=@colormap
                , calcium_threshold=@calcium_threshold, expansion_calculation=@expansion_calculation, expansion_threshold=@expansion_threshold
                , apposition_threshold=@apposition_threshold, brightness=@brightness, contrast=@contrast, sheath_diameter=@sheath_diameter
                , section_proximal=@section_proximal, section_distal=@section_distal
                , create_date=@create_date, update_date=@update_date
                ";

            //UpsertPatientCaseAnnotation
            _query["UpsertPatientCaseAnnotation"] = @$"
                INSERT INTO rv_schema.patient_case_annotation(id, bookmark, longitude, cross_section, lumen_contour, lumen_sidebranch, lumen_stent, lumen_guidewire, ffr_plaque, co_registration, create_date, update_date)
                VALUES (@id, @bookmark, @longitude, @cross_section, @lumen_contour, @lumen_sidebranch, @lumen_stent, @lumen_guidewire, @ffr_plaque, @co_registration, now(), now())
                ON CONFLICT (id)
                DO UPDATE
                SET bookmark=@bookmark, longitude=@longitude, cross_section=@cross_section
                , lumen_contour=@lumen_contour, lumen_sidebranch=@lumen_sidebranch, lumen_stent=@lumen_stent, lumen_guidewire=@lumen_guidewire, ffr_plaque=@ffr_plaque, co_registration=@co_registration, update_date=now()
                ";

            //UpsertCoRegistration
            _query["UpsertCoRegistration"] = @$"
                INSERT INTO rv_schema.patient_case_annotation (id, co_registration)
                VALUES (@id, @co_registration)
                ON CONFLICT (id)
                DO UPDATE
                SET co_registration = @co_registration
                ";
        }
    }
}
