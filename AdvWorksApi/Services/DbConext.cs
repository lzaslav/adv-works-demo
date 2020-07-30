using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace AdvWorksApi.Services
{
    /// <summary>
    /// A helper interface for abstracting all operations with database using Dapper.
    /// </summary>
    public interface IDbContext
    {
        IDbConnection GetConnection();
        Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null);
        Task<T> ExecuteScalarAsync<T>(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null);
        Task<int> ExecuteAsync(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null);
        IDbTransaction BeginTransaction(IDbConnection connection);
    }

    /// <summary>
    /// A thin wrapper around Dapper for db-related operations.
    /// </summary>
    public class DbConext : IDbContext
    {
        private readonly IConfiguration _config;

        public DbConext(IConfiguration config)
        {
            _config = config;

        }

        public IDbTransaction BeginTransaction(IDbConnection connection)
        {
            return connection.BeginTransaction();
        }

        public Task<int> ExecuteAsync(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return cnn.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
        }

        public Task<T> ExecuteScalarAsync<T>(IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            return cnn.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
        }

        public IDbConnection GetConnection()
        {
            return new SqlConnection(_config.GetConnectionString("SalesDbConnectionString"));
        }

        public Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            return cnn.QueryAsync<TFirst, TSecond, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }
    }
}
