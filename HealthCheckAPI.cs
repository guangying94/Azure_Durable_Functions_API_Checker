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
            List<APICheckObject> apiChecks = new List<APICheckObject> { };
            List<string> api_endpoints = api_list.Split(",").ToList();
           
           //using fan out/fan in patterns
            var parrallel_tasks = new List<Task<APICheckObject>>();

            for(int i = 0; i < api_endpoints.Count; i++)
            {
                Task<APICheckObject> task = context.CallActivityAsync<APICheckObject>(
                    "HealthCheck_Executor",api_endpoints[i]);
                parrallel_tasks.Add(task);
            }
            await Task.WhenAll(parrallel_tasks);

            apiChecks = parrallel_tasks.Select(task => task.Result).ToList();

            return apiChecks;
            
            //an example of using function chaining
            /*
            foreach(string api_endpoint in api_endpoints)
            {
                APICheckObject _apiOutput = await context.CallActivityAsync<APICheckObject>("HealthCheck_Executor", api_endpoint);


                apiChecks.Add(_apiOutput);
            }

            return apiChecks;
            */  
        }

        [FunctionName("HealthCheck_Executor")]
        public static async Task<APICheckObject> CallHealthEndpoint([ActivityTrigger] string apiName, ILogger log)
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
                    APICheckObject apiOutput = new APICheckObject()
                    {
                        ApiName = apiName,
                        ApiStatus = 1
                    };
                    return apiOutput;
                }
                else
                {
                    APICheckObject apiOutput = new APICheckObject()
                    {
                        ApiName = apiName,
                        ApiStatus = 0
                    };
                    return apiOutput;
                }
            }
            else
            {
                APICheckObject apiOutput = new APICheckObject()
                {
                    ApiName = apiName,
                    ApiStatus = 0
                };
                return apiOutput;
            }
        }

        [FunctionName("HealthCheck_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("Orchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}