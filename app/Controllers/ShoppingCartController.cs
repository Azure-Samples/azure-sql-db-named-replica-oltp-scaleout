using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using AzureSamples.AzureSQL.Services;

namespace AzureSamples.AzureSQL.Controllers
{
    [ApiController]
    [Route("shopping_cart")]
    public class ShoppingCartController : ControllerQuery
    {
        public ShoppingCartController(IConfiguration config, ILogger<ShoppingCartController> logger, IScaleOut scaleOut):
            base(config, logger, scaleOut, "shopping_cart") {}

        [HttpGet("{id}")]
        public async Task<JsonDocument> Get(int id)
        {            
            var (result, replica) = await this.Query(Verb.Get, id);
            HttpContext.Response.Headers.Add("Used-Replica-Name", replica);
            return result;
        }

        [HttpGet("package/{id}")]
        public async Task<JsonDocument> GetByPackage(int id)
        {
            var (result, replica) = await this.Query(Verb.Get, id, extension: "by_package", tag: "Search");
            HttpContext.Response.Headers.Add("Used-Replica-Name", replica);
            return result;
        }

        [HttpGet("search/{term}")]
        public async Task<JsonDocument> SearchByTerm(String term)
        {            
            var payload = JsonDocument.Parse(JsonSerializer.Serialize(new { term = term }));

            var (result, replica) = await this.Query(Verb.Get, payload: payload.RootElement, extension: "by_search", tag: "Search");
            HttpContext.Response.Headers.Add("Used-Replica-Name", replica);
            return result;
        }

        [HttpPut]
        public async Task<JsonDocument> Put([FromBody]JsonElement payload)
        {
            var (result, replica) = await this.Query(Verb.Put, payload: payload);
            HttpContext.Response.Headers.Add("Used-Replica-Name", replica);
            return result;
        }
    }
}
