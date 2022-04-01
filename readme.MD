# Azure DevOps Release Gate - Sample on using Azure Durable Functions to validate REST API
This sample repository is an implemention of Azure Durable Functions, to check APIs in batches, and return the status of all APIs. The aggregated result is served as release indicator for Azure DevOps release pipeline.

## List of Azure Functions
This sample contains one HTTP Trigger function, and one durable function.

### StartCheck.cs
This is a HTTP trigger function, which will invoke the orchestrator function, looping on the results until the orchestrator completed the job, and then return the result as HTTP response.

In this example, it will aggregate the total success call, as well as the status of all API calls to allow developers to identify malfunctioned APIs in the result.

### HealthCheckAPI.cs
This is an orchestrator, which reads environment variable to retrieve the endpoint, as well as list of APIs to validate. The environment variable of API list is represented as text:
```
api1,api2,api3
```
In this example, the backend API is just an echo. This orchestrator will call the REST api with random string (random integer) and check the return result to make sure the API is working as intended. It will then return boolean (0 or 1 for ease of aggregation) on the outcome.

## Medium Post
Full technical details and use case scenario can be found here.