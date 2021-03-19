using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using AzureSamples.AzureSQL.Services;
using System.Diagnostics;

namespace AzureSamples.AzureSQL.Controllers
{
    public class ControllerQuery : ControllerBase
    {
        private readonly ILogger<ControllerQuery> _logger;
        private readonly IConfiguration _config;
        private readonly IScaleOut _scaleOut;

        public ControllerQuery(IConfiguration config, ILogger<ControllerQuery> logger, IScaleOut scaleOut)
        {
            _logger = logger;
            _config = config;
            _scaleOut = scaleOut;
        }

        protected async Task<List<int>> ExecuteRead(string procedure, object parameters)
        {
            return await Execute(procedure, parameters, "ReaderConnection");
        }

        protected async Task<List<int>> ExecuteWrite(string procedure, object parameters)
        {
            return await Execute(procedure, parameters, "WriterConnection");
        }

        private async Task<List<int>> Execute(string procedure, object parameters, string connectionType)
        {
            var connStr = _scaleOut.GetConnectionString(connectionType);            
            var sb = new SqlConnectionStringBuilder(connStr);
            var sqlLogin = sb.UserID;
            var sqlDatabase = sb.InitialCatalog;            

            using (var conn = new SqlConnection(connStr))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var LogExecution = new Action<Dictionary<string, object>>((d) => {
                        var l = new List<string>();
                        foreach (var kv in d)
                        {
                            l.Add($"{kv.Key}={kv.Value}");
                        }
                        var ps = string.Join(", ", l.ToArray());
                        _logger.LogInformation($"{sqlLogin}@{sqlDatabase}: dbo.[CDB_{procedure}] {ps}");
                    });

                    if (parameters is Dictionary<string, object>)
                    {
                        var d = parameters as Dictionary<string, object>;
                        LogExecution(d);
                    }

                    if (parameters is List<Dictionary<string, object>>)
                    {
                        var l = parameters as List<Dictionary<string, object>>;
                        l.ForEach(d => LogExecution(d));
                    }
                }

                var sw = new Stopwatch();
                
                sw.Start();
                var gr = await conn.ExecuteAsync(
                    sql: $"dbo.[CDB_{procedure}]",
                    param: parameters,
                    commandType: CommandType.StoredProcedure
                );
                sw.Stop();

                var result = new List<int>();
                result.Add((int)(sw.ElapsedMilliseconds));
                /*
                while(!gr.IsConsumed)
                {
                    var s = gr.Read();
                    result.Add(s.AsList().Count());
                }
                */
                return result;
            };
        }
    }
}
