using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessVideoStarter
{
    public static class ProcessVideoOrchestrators
    {
        [FunctionName("O_ProcessVideo")]
        public static async Task<object> ProcessVideo(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            ILogger log
            )
        {
            var videoLocation = ctx.GetInput<string>();

            if (!ctx.IsReplaying)
                log.LogInformation("About to call Transcode Video...");
            var transcodedLocation = await ctx.CallActivityAsync<string>("A_TranscodeVideo", videoLocation);

            if (!ctx.IsReplaying)
                log.LogInformation("About to call extract thumbnail...");
            var thumbnailLocation = await ctx.CallActivityAsync<string>("A_ExtractThumbnail", transcodedLocation);


            if (!ctx.IsReplaying)
                log.LogInformation("About to call Prepend Intro...");

            var withIntroLocation = await ctx.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

            return new
            {
                Transcoded = transcodedLocation,
                Thumbnail = thumbnailLocation,
                WithIntro = withIntroLocation
            };
        }
    }
}
