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
                $"SELECT id, lastname, firstname, birthdate, rv_schema.fn_code('GEND', gender) gender, to_char(create_date,'YYYY-MM-DD HH24:MI:SS') createdate, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') updatedate " +
                $"FROM rv_schema.patient " +
                $"WHERE id LIKE @id OR lastname LIKE @lastname OR firstname LIKE @firstname";

            //SelectPatient
            _query["SelectPatient"] = 
                $"SELECT id, lastname, firstname, birthdate, rv_schema.fn_code('GEND', gender) gender, to_char(create_date,'YYYY-MM-DD HH24:MI:SS') createdate, to_char(update_date,'YYYY-MM-DD HH24:MI:SS') updatedate " +
                $"FROM rv_schema.patient " +
                $"WHERE id = @id "; ;

            //SelectCodeList
            _query["SelectCodeList"] =
                $"SELECT classification, key, value, buffer1, buffer2 " +
                $"FROM rv_schema.code " +
                $"ORDER BY classification, key";
        }

        private static void SetInsertQuery()
        {
            _log.Debug("SetInsertQuery");

            //InsertPatient
            _query["InsertPatient"] =
                $"INSERT INTO rv_schema.patient(id, lastname, firstname, birthdate, gender, create_date, update_date) " +
                $"VALUES (@id, @lastname, @firstname, @birthdate, @gender, now(), now())";
        }

        private static void SetUpdateQuery()
        {
            _log.Debug("SetUpdateQuery");

            //UpdatePatient
            _query["UpdatePatient"] =
                $"UPDATE rv_schema.patient " +
                $"SET id=@id, lastname=@lastname, firstname=@firstname, birthdate=@birthdate, gender=@gender, update_date=now() " +
                $"WHERE id=@id";
        }

        private static void SetDeleteQuery()
        {
            _log.Debug("SetDeleteQuery");
        }
    }
}
