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
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            HttpRequestMessageFeature hreqmf = new HttpRequestMessageFeature(req.HttpContext);
            HttpRequestMessage httpRequestMessage = hreqmf.HttpRequestMessage;

            log.LogInformation($"C# HTTP trigger function processed a request.");

            // parse query parameter
            string video = httpRequestMessage.RequestUri.ParseQueryString()["video"];

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

            return starter.CreateCheckStatusResponse(httpRequestMessage, orchestrationId);
        }
    }
}
