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
        static int retryCounter = 1;

        [FunctionName("O_ProcessVideo")]
        public static async Task<object> ProcessVideo(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            ILogger log
            )
        {
            var videoLocation = ctx.GetInput<string>();
            string transcodedLocation = null;
            string thumbnailLocation = null;
            string withIntroLocation = null;            

            try
            {
                if (!ctx.IsReplaying)
                    log.LogInformation("About to call Transcode Video...");
                transcodedLocation = await ctx.CallActivityAsync<string>("A_TranscodeVideo", videoLocation);

                //if (!ctx.IsReplaying)
                    log.LogInformation($"About to call extract thumbnail...try {retryCounter++}");
                thumbnailLocation = await
                    ctx.CallActivityWithRetryAsync<string>(
                        "A_ExtractThumbnail",
                        new RetryOptions(TimeSpan.FromSeconds(5), 5)
                        { Handle = ex => ex is InvalidOperationException },
                        transcodedLocation
                    );

                if (!ctx.IsReplaying)
                    log.LogInformation("About to call Prepend Intro...");
                withIntroLocation = await ctx.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);
            }
            catch (Exception ex)
            {
                if (!ctx.IsReplaying)
                {
                    log.LogInformation($"Caught an error from an activity: {ex.Message}");
                }

                await ctx.CallActivityAsync<string>
                    ("A_Cleanup", new[] { transcodedLocation, thumbnailLocation, withIntroLocation });

                return new
                {
                    Error = "Failed to process uploaded video",
                    Message = ex.Message
                };
            }

            return new
            {
                Transcoded = transcodedLocation,
                Thumbnail = thumbnailLocation,
                WithIntro = withIntroLocation
            };
        }
    }
}
