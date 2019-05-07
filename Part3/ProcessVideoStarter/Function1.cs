using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ProcessVideoStarter
{
    public static class Function1
    {
        [FunctionName("ProcessVideoStarter")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
			[OrchestrationClient] DurableOrchestrationClient starter,
			ILogger log)
        {
			// parse query parameter
			string video = req.GetQueryNameValuePairs()
				.FirstOrDefault(q => string.Compare(q.Key, "video", true) == 0)
				.Value;

			// Get request body
			dynamic data = await req();

			// Set name to query string or body data
			video = video ?? data?.video;

			if (video == null)
			{
				return req.CreateResponse(HttpStatusCode.BadRequest,
				   "Please pass the video location the query string or in the request body");
			}

			log.LogInformation($"About to start orchestration for {video}");

			var orchestrationId = await starter.StartNewAsync("O_ProcessVideo", video);

			return starter.CreateCheckStatusResponse(req, orchestrationId);
		}
	}
}
