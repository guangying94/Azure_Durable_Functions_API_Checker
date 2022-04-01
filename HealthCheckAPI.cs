using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DeploymentGateCheck
{
    public static class HealthCheckAPI
    {
        public static string api_endpoint = Environment.GetEnvironmentVariable("API_ENDPOINT");
        public static string api_list = Environment.GetEnvironmentVariable("API_LIST");

        [FunctionName("Orchestrator")]
        public static async Task<List<APICheckObject>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {   
            var name = context.GetInput<List<APICheckObject>>();

            List<APICheckObject> apiChecks = new List<APICheckObject> { };
            List<string> api_endpoints = api_list.Split(",").ToList();

            foreach(string api_endpoint in api_endpoints)
            {
                int _res = await context.CallActivityAsync<int>("HealthCheck_Executor", api_endpoint);
                APICheckObject _apiOutput = new APICheckObject()
                {
                    ApiName = api_endpoint,
                    ApiStatus = _res
                };

                apiChecks.Add(_apiOutput);
            }

            return apiChecks;
                
        }

        [FunctionName("HealthCheck_Executor")]
        public static async Task<int> CallHealthEndpoint([ActivityTrigger] string apiName, ILogger log)
        {
            HttpClient client = new HttpClient();
            //sample test API to echo message
            //update this part here to your use cases
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            string randomString = rnd.Next().ToString();
            string check_url = api_endpoint + apiName + "?msg=" + randomString;
            var response = await client.GetAsync(check_url);

            //simple rule to ensure returned message is the same
            //update the criteria of success accordingly
            if(response.IsSuccessStatusCode)
            {
                string api_result = await response.Content.ReadAsStringAsync();
                JObject result = JObject.Parse(api_result);
                if((string)result["message"] == randomString)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        [FunctionName("HealthCheck_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("Orchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}