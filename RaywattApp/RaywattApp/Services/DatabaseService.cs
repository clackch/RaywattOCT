using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Npgsql;

namespace RaywattApp.Services
{
    /// <summary>
    /// DatabaseService 
    /// </summary>
    public abstract class DatabaseService : IDatabaseService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DatabaseService));

        private string _connectionString;
        /// <summary>
        /// ConnectionString
        /// </summary>
        public string ConnectionString => _connectionString;

        /// <summary>
        /// 기본 connection
        /// </summary>
        protected NpgsqlConnection Connection { get; set; }

        /// <summary>
        /// 기본 command
        /// </summary>
        protected NpgsqlCommand Command { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public DatabaseService(string connectionString)
        {
            _log.Debug("DatabaseService");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Query 실행 후 결과 반환(비동기 방식)
        /// </summary>
        public virtual async Task<IList<T>> GetDatasAsync<T>(string commandText, Dictionary<string, Object> commandParameters) where T : class
        {
            _log.Debug("GetDatasAsync");

            //null 체크
            if (Connection == null || Command == null || string.IsNullOrEmpty(commandText))
            {
                return null;
            }

            var returnDatas = new List<T>();

            try
            {
                //Connection 열기
                await Connection.OpenAsync();
                //Query 입력
                Command.CommandText = commandText;
                //Parameter 입력
                Command.Parameters.Clear();
                if(commandParameters != null)
                {
                    foreach (KeyValuePair<string, Object> parameter in commandParameters)
                    {
                        Command.Parameters.AddWithValue(parameter.Key, (parameter.Value == null ? "" : parameter.Value));
                    }
                }
                //Connection 입력
                Command.Connection = Connection;
                //Command 실행하고 Reader 반환
                using var reader = await Command.ExecuteReaderAsync();
                //Reader를 이용해서 한줄 읽음
                while (await reader.ReadAsync())
                {
                    IDataReader row = reader;
                    //T를 이용해서 인스턴스 생성
                    var model = Activator.CreateInstance(typeof(T));
                    //결과 목록에 추가
                    returnDatas.Add(model as T);
                    //이 아래 부분은 프로퍼티 한개씩 하드코딩 하지 않고, 값을 입력하기 위해서 사용하는 부분입니다.
                    //모델에서 프로퍼티 추출
                    var properties = model.GetType().GetProperties();
                    //프로퍼티 중 HasErrors라는 이름의 프로퍼티 빼고 나머지 데이터 입력
                    foreach (var prop in properties.Where(p => p.Name != "HasErrors"))
                    {
                        var value = GetDbValue(prop.Name, row);
                        if (value is DBNull == false)
                        {
                            prop.SetValue(model, value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }
            finally
            {
                //Connection 닫기
                await Connection.CloseAsync();

                PrintLog(commandText, commandParameters);
            }
            //결과 반환
            return returnDatas;
        }

        /// <summary>
        /// 쿼리 실행 후 결과 반환(동기 방식)
        /// </summary>
        public virtual IList<T> GetDatas<T>(string commandText, Dictionary<string, Object> commandParameters) where T : class
        {
            _log.Debug("GetDatas");

            //null 체크
            if (Connection == null || Command == null || string.IsNullOrEmpty(commandText))
            {
                return null;
            }

            var returnDatas = new List<T>();

            try
            {
                //Connection 열기
                Connection.Open();
                //Query 입력
                Command.CommandText = commandText;
                //Parameter 입력
                Command.Parameters.Clear();
                if(commandParameters != null)
                {
                    foreach (KeyValuePair<string, Object> parameter in commandParameters)
                    {
                        Command.Parameters.AddWithValue(parameter.Key, (parameter.Value == null ? "" : parameter.Value));
                    }
                }
                //Connection 입력
                Command.Connection = Connection;
                //Command 실행하고 Reader 반환
                using var reader = Command.ExecuteReader();
                //Reader를 이용해서 한줄 읽음
                while (reader.Read())
                {
                    IDataReader row = reader;
                    //T를 이용해서 인스턴스 생성
                    var model = Activator.CreateInstance(typeof(T));
                    //결과 목록에 추가
                    returnDatas.Add(model as T);
                    //이 아래 부분은 프로퍼티 한개씩 하드코딩 하지 않고, 값을 입력하기 위해서 사용하는 부분입니다.
                    //모델에서 프로퍼티 추출
                    var properties = model.GetType().GetProperties();

                    //프로퍼티 중 HasErrors라는 이름의 프로퍼티 빼고 나머지 데이터 입력
                    foreach (var prop in properties.Where(p => p.Name != "HasErrors"))
                    {
                        var value = GetDbValue(prop.Name, row);
                        if (value is DBNull == false)
                        {
                            prop.SetValue(model, value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }
            finally
            {
                //Connection 닫기
                Connection.Close();

                PrintLog(commandText, commandParameters);
            }
            //결과 반환
            return returnDatas;
        }

        public int GetDataCount(string commandText, Dictionary<string, Object> commandParameters)
        {
            _log.Debug("GetDataCount");

            //null 체크
            if (Connection == null || Command == null || string.IsNullOrEmpty(commandText))
            {
                return -1;
            }

            int cnt = 0;

            try
            {
                //Connection 열기
                Connection.Open();
                //Query 입력
                Command.CommandText = commandText;
                //Parameter 입력
                Command.Parameters.Clear();
                if (commandParameters != null)
                {
                    foreach (KeyValuePair<string, Object> parameter in commandParameters)
                    {
                        Command.Parameters.AddWithValue(parameter.Key, (parameter.Value == null ? "" : parameter.Value));
                    }
                }
                //Connection 입력
                Command.Connection = Connection;
                //Command 실행하고 Reader 반환
                using var reader = Command.ExecuteReader();
                //Reader를 이용해서 한줄 읽음
                while (reader.Read())
                {
                    cnt = (int)(long)reader.GetValue(0);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }
            finally
            {
                //Connection 닫기
                Connection.Close();

                PrintLog(commandText, commandParameters);
            }
            //결과 반환
            return cnt;
        }

        public int InsertData(string commandText, Dictionary<string, Object> commandParameters)
        {
            _log.Debug("InsertData");

            //null 체크
            if (Connection == null || Command == null || string.IsNullOrEmpty(commandText))
            {
                return -1;
            }

            int nRows = 0;

            try
            {
                //Connection 열기
                Connection.Open();
                //Query 입력
                Command.CommandText = commandText;
                //Parameter 입력
                Command.Parameters.Clear();
                if (commandParameters != null)
                {
                    foreach (KeyValuePair<string, Object> parameter in commandParameters)
                    {
                        Command.Parameters.AddWithValue(parameter.Key, (parameter.Value == null? "" : parameter.Value));
                    }
                }
                //Connection 입력
                Command.Connection = Connection;
                //Execute Query
                nRows = Command.ExecuteNonQuery();
            }
            catch (Exception e) 
            {
                _log.Error(e.ToString());
            }
            finally
            {
                //Connection 닫기
                Connection.Close();

                PrintLog(commandText, commandParameters);
            }
            //결과 반환
            return nRows;
        }

        public int UpdateData(string commandText, Dictionary<string, Object> commandParameters)
        {
            _log.Debug("UpdateData");

            //null 체크
            if (Connection == null || Command == null || string.IsNullOrEmpty(commandText))
            {
                return -1;
            }

            int nRows = 0;

            try
            {
                //Connection 열기
                Connection.Open();
                //Query 입력
                Command.CommandText = commandText;
                //Parameter 입력
                Command.Parameters.Clear();
                if (commandParameters != null)
                {
                    foreach (KeyValuePair<string, Object> parameter in commandParameters)
                    {
                        Command.Parameters.AddWithValue(parameter.Key, (parameter.Value == null ? "" : parameter.Value));
                    }
                }
                //Connection 입력
                Command.Connection = Connection;
                //Execute Query
                nRows = Command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }
            finally
            {
                //Connection 닫기
                Connection.Close();

                PrintLog(commandText, commandParameters);
            }
            //결과 반환
            return nRows;
        }

        public int DeleteData(string commandText, Dictionary<string, Object> commandParameters)
        {
            _log.Debug("DeleteData");

            //null 체크
            if (Connection == null || Command == null || string.IsNullOrEmpty(commandText))
            {
                return -1;
            }

            int nRows = 0;

            try
            {
                //Connection 열기
                Connection.Open();
                //Query 입력
                Command.CommandText = commandText;
                //Parameter 입력
                Command.Parameters.Clear();
                if (commandParameters != null)
                {
                    foreach (KeyValuePair<string, Object> parameter in commandParameters)
                    {
                        Command.Parameters.AddWithValue(parameter.Key, (parameter.Value == null ? "" : parameter.Value));
                    }
                }
                //Connection 입력
                Command.Connection = Connection;
                //Execute Query
                nRows = Command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                _log.Error(e.ToString());
            }
            finally
            {
                //Connection 닫기
                Connection.Close();

                PrintLog(commandText, commandParameters);
            }
            //결과 반환
            return nRows;
        }

        private object GetDbValue(string field, IDataReader row)
        {
            for (int i = 0; i < row.FieldCount; i++)
            {
                string dbField = row.GetName(i);
                if (dbField.ToLower().Replace("_", "").Equals(field.ToLower()))
                    return row[dbField];
            }
            return null;
        }

        private void PrintLog(string commandText, Dictionary<string, Object> commandParameters)
        {
            _log.Debug("Query : " + commandText.Replace("\r\n", " ").Replace("  ", ""));
            if (commandParameters != null)
            {
                string str = "Parameters : ";
                foreach (var param in commandParameters)
                {
                    if (param.Key != "cross_section" && param.Key != "longitude" && param.Key != "lumen_contour" && param.Key != "bookmark" && param.Key != "track_point")
                        str += param.Key + " = " + param.Value + ", ";
                }
                _log.Debug(str);
            }
        }
    }
}
