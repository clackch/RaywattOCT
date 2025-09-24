using Npgsql;

namespace RaywattOCTFFR.Services
{
    /// <summary>
    /// PostgresQL 전용 서비스
    /// </summary>
    public class SqlService : DatabaseService
    {
        public SqlService(string connectionString) : base(connectionString)
        {
            //PostgresQL Connection 생성
            Connection = new NpgsqlConnection(ConnectionString);

            //PostgresQL Command 생성 
            Command = new NpgsqlCommand();
        }

        ~SqlService()
        {
            Command.Dispose();
            Connection.Dispose();
        }
    }
}
