using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;

namespace ProcessVideoStarter
{
    public static class ProcessVideoStarter
    {
        [FunctionName("ProcessVideoStarter")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            //HttpRequestMessageFeature hreqmf = new HttpRequestMessageFeature(req.HttpContext);
            //HttpRequestMessage httpRequestMessage = hreqmf.HttpRequestMessage;

            log.LogInformation($"C# HTTP trigger function processed a request.");

            // parse query parameter
            string video = req.RequestUri.ParseQueryString()["video"];

            dynamic data = req;

            // Set name to query string or body data
            video = video ?? data?.video;

            if (video == null)
            {
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Please pass the video location the query string or in the request body")
                };
                return response;

            }

            log.LogInformation($"About to start orchestration for {video}");

            var orchestrationId = await starter.StartNewAsync("O_ProcessVideo", video);

            return starter.CreateCheckStatusResponse(req, orchestrationId);
        }

        [FunctionName("SubmitVideoApproval")]
        public static async Task<HttpResponseMessage> SubmitVideoApproval(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "SubmitVideoApproval/{id}")]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            [Table("Approvals", "Approval", "{id}", Connection = "AzureWebJobsStorage")] Approval approval,
            ILogger log)
        {
            //HttpRequestMessageFeature hreqmf = new HttpRequestMessageFeature(req.HttpContext);
            //HttpRequestMessage httpRequestMessage = hreqmf.HttpRequestMessage;

            // nb if the approval code doesn't exist, framework just returns a 404 before we get here
            var result = req.RequestUri.ParseQueryString()["result"] == "Approved";

            if (!result)
                return req.CreateResponse(HttpStatusCode.BadRequest, "Need an approval result");

            log.LogWarning($"Sending approval result to {approval.OrchestrationId} of {req.RequestUri.ParseQueryString()["result"]}");
            // send the ApprovalResult external event to this orchestration
            await client.RaiseEventAsync(approval.OrchestrationId, "ApprovalResult", result);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [FunctionName("StartPeriodicTask")]
        public static async Task<HttpResponseMessage> StartPeriodicTask(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            ILogger log)
        {
            var instanceId = await client.StartNewAsync("O_PeriodicTask", 0);
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
