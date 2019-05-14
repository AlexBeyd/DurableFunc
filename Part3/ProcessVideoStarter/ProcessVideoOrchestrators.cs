using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            string transcodedLocation = null;
            string thumbnailLocation = null;
            string withIntroLocation = null;
            bool result = false;

            try
            {
                var transcodeResults =
                    await ctx.CallSubOrchestratorAsync<VideoFileInfo[]>("O_TranscodeVideo", ctx.GetInput<string>());

                transcodedLocation = transcodeResults.OrderByDescending(res => res.Bitrate).First().Location;

                //if (!ctx.IsReplaying)
                //log.LogInformation($"About to call extract thumbnail...try {retryCounter++}");
                thumbnailLocation = await
                    ctx.CallActivityWithRetryAsync<string>(
                        "A_ExtractThumbnail",
                        new RetryOptions(TimeSpan.FromSeconds(5), 5)
                        { Handle = ex => ex is InvalidOperationException },
                        transcodedLocation
                    );

                //if (!ctx.IsReplaying)
                //    log.LogInformation("About to call Prepend Intro...");
                withIntroLocation = await ctx.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

                await ctx.CallActivityAsync("A_SendApprovalRequestEmail", new ApprovalInfo
                {
                    OrchestrationId = ctx.InstanceId,
                    VideoLocation = withIntroLocation
                });

                using (var cts = new CancellationTokenSource())
                {
                    var timeoutAt = ctx.CurrentUtcDateTime.AddSeconds(5);
                    var timeoutTask = ctx.CreateTimer(timeoutAt, cts.Token);
                    var approvalTask = ctx.WaitForExternalEvent<bool>("ApprovalResult");
                    var winner = await Task.WhenAny(timeoutTask, approvalTask);
                    if (winner == approvalTask)
                    {
                        result = approvalTask.Result;
                    }                    
                }

                if (result)
                {
                    await ctx.CallActivityAsync("A_PublishVideo", ctx.GetInput<string>());
                }
                else
                {
                    await ctx.CallActivityAsync("A_RejectVideo", ctx.GetInput<string>());
                }
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
                WithIntro = withIntroLocation,
                ApprovalResult = result
            };
        }

        [FunctionName("O_TranscodeVideo")]
        public static async Task<VideoFileInfo[]> TranscodeVideo(
            [OrchestrationTrigger] DurableOrchestrationContext ctx,
            ILogger log
            )
        {
            var videoLocation = ctx.GetInput<string>();

            var bitRates = await ctx.CallActivityAsync<List<int>>("A_GetBitrates", null);

            var transcodeTasks = new List<Task<VideoFileInfo>>();

            foreach (var bitRate in bitRates)
            {
                var info = new VideoFileInfo { Location = videoLocation, Bitrate = bitRate };
                var task = ctx.CallActivityAsync<VideoFileInfo>("A_TranscodeVideo", info);
                transcodeTasks.Add(task);
            }

            var transcodeResults = await Task.WhenAll(transcodeTasks);

            return transcodeResults;
        }
    }
}
