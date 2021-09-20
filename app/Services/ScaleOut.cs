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
        string GetConnectionString(ConnectionIntent connectionIntent, string tag);
    }

    public enum ConnectionIntent
    {
        Read,
        Write
    }

    public class ScaleOut : IScaleOut
    {
        private class ReplicaInfo
        {
            public string Tag = default(string);
            public string DatabaseName = default(string);
        }

        private readonly IConfiguration _config;
        private readonly ILogger<ScaleOut> _logger;
        private DateTime _lastCall = DateTime.Now;
        private Random _rnd = new Random();
        private Dictionary<string, List<String>> _replicaConnectionString = new Dictionary<string, List<String>>();

        public ScaleOut(IConfiguration config, ILogger<ScaleOut> logger)
        {
            _logger = logger;
            _config = config;                       
        }

        public string GetConnectionString(ConnectionIntent connectionIntent, string tag = default(string))
        {
            if (connectionIntent == ConnectionIntent.Write) 
                return _config.GetConnectionString("AzureSQLConnection");

            if (tag == default(string)) 
                tag = "GenericRead";

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
                    var replicaInfoList = conn.Query<ReplicaInfo>(
                        sql: "api.get_available_scale_out_replicas",
                        commandType: CommandType.StoredProcedure
                    ).AsList();
                    
                    _replicaConnectionString = new Dictionary<string, List<string>>();

                    foreach(var ri in replicaInfoList)
                    {
                        var csb = new SqlConnectionStringBuilder(connString);
                        if (!string.IsNullOrEmpty(ri.DatabaseName))
                            csb.InitialCatalog = ri.DatabaseName;
                        
                        if (!_replicaConnectionString.ContainsKey(ri.Tag))
                            _replicaConnectionString.Add(ri.Tag, new List<string>());

                        _replicaConnectionString[ri.Tag].Add(csb.ToString());                        
                    }

                    _lastCall = DateTime.Now;
                    _logger.LogDebug($"Got {replicaInfoList.Count} replicas over {_replicaConnectionString.Count} tags.");
                }            
            }
                
            // Get the list of available connection strings based on requested tag
            List<string> connectionStringList = null;
            _replicaConnectionString.TryGetValue(tag, out connectionStringList);

            // Fall back to GenericRead tag if requested tag is not found
            if (connectionStringList == null)
                _replicaConnectionString.TryGetValue("GenericRead", out connectionStringList);
            
            // Get a connection string randomly
            if (connectionStringList != null && connectionStringList.Count > 0)
            {
                var i = _rnd.Next(connectionStringList.Count);
                result = connectionStringList[i];
            }
            else {
                result = _config.GetConnectionString("AzureSQLConnection");
            }                          

            return result;
        }
    }
}