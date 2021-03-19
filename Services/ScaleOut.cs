using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AzureSamples.AzureSQL.Services
{
    public interface IScaleOut
    {
        string GetConnectionString(string connectionType);
    }

    public class ScaleOut : IScaleOut
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ScaleOut> _logger;
        private DateTime _lastCall = DateTime.Now;
        private string _lastConnectionString = string.Empty;

        public ScaleOut(IConfiguration config, ILogger<ScaleOut> logger)
        {
            _logger = logger;
            _config = config;           
            _lastConnectionString = _config.GetConnectionString("ReaderConnection");             
        }

        public string GetConnectionString(string connectionType)
        {
            if (connectionType == "WriterConnection") 
                return _config.GetConnectionString("WriterConnection");

            string result = string.Empty;
            var elapsed = DateTime.Now - _lastCall;

            var rnd = new Random();

            if (elapsed.TotalMilliseconds > rnd.Next(3500, 5500))
            {
                var database = string.Empty;
                var connString = _config.GetConnectionString("ReaderConnection");

                using (var conn = new SqlConnection(connString))
                {
                    var databases = conn.Query<string>(
                        sql: "api.GetAvailableScaleOutReplicas",
                        commandType: CommandType.StoredProcedure
                    ).AsList();
                    
                    if (databases.Count > 0)
                    {
                        var i = rnd.Next(databases.Count);
                        database = databases[i];
                        _logger.LogDebug(database);
                    }
                }            

                var csb = new SqlConnectionStringBuilder(connString);
                if (!string.IsNullOrEmpty(database))
                    csb.InitialCatalog = database;
                csb.ApplicationIntent = ApplicationIntent.ReadOnly;
                result = csb.ConnectionString;

                _lastCall = DateTime.Now;
                _lastConnectionString = result;
            } else {
                result = _lastConnectionString;
            }

            return result;
        }
    }
}