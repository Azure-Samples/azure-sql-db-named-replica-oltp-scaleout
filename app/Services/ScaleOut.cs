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
        string GetConnectionString(ConnectionIntent connectionIntent);
    }

    public enum ConnectionIntent
    {
        Read,
        Write
    }

    public class ScaleOut : IScaleOut
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ScaleOut> _logger;
        private DateTime _lastCall = DateTime.Now;
        private Random _rnd = new Random();
        private List<String> _replicaConnectionString = new List<String>();

        public ScaleOut(IConfiguration config, ILogger<ScaleOut> logger)
        {
            _logger = logger;
            _config = config;                       
        }

        public string GetConnectionString(ConnectionIntent connectionIntent)
        {
            if (connectionIntent == ConnectionIntent.Write) 
                return _config.GetConnectionString("AzureSQLConnection");

            string result = string.Empty;
            var elapsed = DateTime.Now - _lastCall;            

            // Get the list of available named replica from the primary replica
            // Add some randomness to avoid the "thundering herd" problem
            // in case there are many concurrent instances of this web app running
            if (elapsed.TotalMilliseconds > _rnd.Next(3500, 5500))
            {
                _logger.LogDebug($"Loading available replicas...");

                var database = string.Empty;
                var connString = _config.GetConnectionString("AzureSQLConnection");

                using (var conn = new SqlConnection(connString))
                {
                    var databases = conn.Query<string>(
                        sql: "api.get_available_scale_out_replicas",
                        commandType: CommandType.StoredProcedure
                    ).AsList();
                    
                    _replicaConnectionString = new List<string>();                    

                    foreach(var d in databases)
                    {
                        var csb = new SqlConnectionStringBuilder(connString);
                        if (!string.IsNullOrEmpty(d))
                            csb.InitialCatalog = d;
                        
                        _replicaConnectionString.Add(csb.ToString());
                    }

                    _lastCall = DateTime.Now;
                    _logger.LogDebug($"Got {_replicaConnectionString.Count} replicas.");
                }            
            }
                
            // Get a connection string randomly
            if (_replicaConnectionString.Count > 0)
            {                
                var i = _rnd.Next(_replicaConnectionString.Count);
                result = _replicaConnectionString[i];
            } else {
                result = _config.GetConnectionString("AzureSQLConnection");
            }                          

            return result;
        }
    }
}