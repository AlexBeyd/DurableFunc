using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net;

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
			log.LogInformation($"C# HTTP trigger function processed a request.");

			// parse query parameter
			string video = req.GetQueryParameterDictionary()
				.FirstOrDefault(q => string.Compare(q.Key, "video", true) == 0)
				.Value;

			// Get request body
			dynamic data = req.Body;

			// Set name to query string or body data
			video = video ?? data?.video;

			if (video == null)
			{
				return new HttpResponseMessage(HttpStatusCode.BadRequest);
				//"Please pass the video location the query string or in the request body");
			}

			log.LogInformation($"About to start orchestration for {video}");

			var orchestrationId = await starter.StartNewAsync("O_ProcessVideo", video);

			return starter.CreateCheckStatusResponse(new HttpRequestMessage(new HttpMethod(req.Method), req.Path), orchestrationId);
		}
	}
}
