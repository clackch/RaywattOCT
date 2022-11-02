using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaywattApp.Services
{
    /// <summary>
    /// IDatabaseService
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// ConnectionString
        /// </summary>
        string ConnectionString { get; }
        
        /// <summary>
        /// 비동기화 Select Query
        /// </summary>
        Task<IList<T>> GetDatasAsync<T>(string commandText, Dictionary<string, Object> commandParameters = null) where T : class;

        /// <summary>
        /// 동기화 Select Query
        /// </summary>
        IList<T> GetDatas<T>(string commandText, Dictionary<string, Object> commandParameters = null) where T : class;

        int GetDataCount(string commandText, Dictionary<string, Object> commandParameters = null);

        /// <summary>
        /// Data Insert
        /// </summary>
        int InsertData(string commandText, Dictionary<string, Object> commandParameters = null);

        /// <summary>
        /// Data Update
        /// </summary>
        int UpdateData(string commandText, Dictionary<string, Object> commandParameters = null);

        /// <summary>
        /// Data Delete
        /// </summary>
        int DeleteData(string commandText, Dictionary<string, Object> commandParameters = null);
    }
}
