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
    public enum Verb
    {
        Get,
        Put
    }

    public class ControllerQuery : ControllerBase
    {
        private readonly ILogger<ControllerQuery> _logger;
        private readonly IConfiguration _config;
        private readonly IScaleOut _scaleOut;
        private readonly string _entityName = string.Empty ;

        public ControllerQuery(IConfiguration config, ILogger<ControllerQuery> logger, IScaleOut scaleOut, string entityName)
        {
            _logger = logger;
            _config = config;
            _scaleOut = scaleOut;
            _entityName = entityName;
        }

        protected async Task<(JsonDocument, String)> Query(Verb verb, int? id = null, JsonElement payload = default(JsonElement), string extension = default(string))
        {
            JsonDocument result = null;

            var connectionIntent = (verb == Verb.Get) ? ConnectionIntent.Read : ConnectionIntent.Write;

            extension = (extension == default(string)) ? string.Empty : "_" + extension;
            string procedure = $"api.{verb.ToString().ToLower()}_{_entityName}{extension}";
            _logger.LogDebug($"Executing {procedure}");

            string databaseName = string.Empty;

            using(var conn = new SqlConnection(_scaleOut.GetConnectionString(connectionIntent))) {
                databaseName = conn.Database;

                DynamicParameters parameters = new DynamicParameters();
                
                if (payload.ValueKind != default(JsonValueKind))
                {
                    var json = JsonSerializer.Serialize(payload);
                    parameters.Add("Payload", json);
                }

                if (id.HasValue)
                    parameters.Add("Id", id.Value);

                var esr = await conn.ExecuteScalarAsync<string>(
                    sql: procedure, 
                    param: parameters, 
                    commandType: CommandType.StoredProcedure
                );
                
                if (esr != null)
                    result = JsonDocument.Parse(esr);
            };

            if (result == null) 
                result = JsonDocument.Parse("[]");
                        
            return (result, databaseName);
        }        
    }
}
