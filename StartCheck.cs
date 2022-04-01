using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Dynamitey.DynamicObjects;
using System.Collections.Generic;
using System.Linq;

namespace DeploymentGateCheck
{
    public static class StartCheck
    {
        public static string orchestrator_url = Environment.GetEnvironmentVariable("ORCHESTRATOR_URL");

        [FunctionName("StartCheck")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            HttpClient client = new HttpClient();
            var response = await client.PostAsync(orchestrator_url);
            var _result = await response.Content.ReadAsStringAsync();

            dynamic data = JsonConvert.DeserializeObject(_result);
            string check_status_url = data?.statusQueryGetUri;
            string orchestrator_output = "";

            //check output from Azure Durable Functions
            //loop till orchestrator completes job
            bool completed = false;
            while(!completed)
            {
                var _res = await client.GetAsync(check_status_url);
                var _checkResult = await _res.Content.ReadAsStringAsync();

                dynamic outcome = JsonConvert.DeserializeObject(_checkResult);
                if(outcome?.runtimeStatus == "Completed")
                {
                    completed = true;
                    Console.WriteLine(JsonConvert.SerializeObject(outcome?.output));
                    orchestrator_output = JsonConvert.SerializeObject(outcome?.output);
                }
                Task.Delay(250).Wait();
            }

            APICheckObject[] output = JsonConvert.DeserializeObject<APICheckObject[]>(orchestrator_output);

            DeploymentResult allTest = new DeploymentResult()
            {
                details = output,
                totalSuccess = output.Select(x => x.ApiStatus).Sum()
            };

            return new OkObjectResult(allTest);
        }
    }
}
