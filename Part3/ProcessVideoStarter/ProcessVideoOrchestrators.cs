using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessVideoStarter
{
	public static class  ProcessVideoOrchestrators
	{
		[FunctionName("O_ProcessVideo")]
		public static async Task<object> ProcessVideo(
			[OrchestrationTrigger] DurableOrchestrationContext ctx,
			ILogger log
			)
		{
			var videoLocation = ctx.GetInput<string>();

			var transcodedLocation = await ctx.CallActivityAsync<string>("A_TranscodeVideo", videoLocation);
		}
	}
}
