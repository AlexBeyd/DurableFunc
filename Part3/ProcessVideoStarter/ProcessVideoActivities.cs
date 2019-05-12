using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessVideoStarter
{
    public static class ProcessVideoActivities
    {
        [FunctionName("A_TranscodeVideo")]
        public static async Task<string> TranscodeVideo(
            [ActivityTrigger] string inputVideo,
            ILogger log
            )
        {
            log.LogInformation($"Transcoding {inputVideo}");
            await Task.Delay(5000);

            return $"{Path.GetFileNameWithoutExtension(inputVideo)}";
        }

        [FunctionName("A_ExtractThumbnail")]
        public static async Task<string> ExtractThumbnail(
            [ActivityTrigger] string inputVideo,
            ILogger log
            )
        {
            log.LogInformation($"Extracting thumbnail {inputVideo}");

            if (inputVideo.Contains("error"))
            {
                throw new InvalidOperationException("Could not extract thumbnail.");
            }

            await Task.Delay(5000);

            return "thumbnail.png";
        }

        [FunctionName("A_PrependIntro")]
        public static async Task<string> PrependIntro(
            [ActivityTrigger] string inputVideo,
            ILogger log
            )
        {
            log.LogInformation($"Appending intro to video {inputVideo}");
            var introLocation = ConfigurationManager.AppSettings["IntroLocation"];
            await Task.Delay(5000);

            return "withIntro.mp4";
        }

        [FunctionName("A_Cleanup")]
        public static async Task<string> Cleanup(
            [ActivityTrigger] string[] filesToCleanup,
            ILogger log
            )
        {
            foreach (var file in filesToCleanup.Where(f => f != null))
            {
                log.LogInformation($"Cleaning up \"{file}\"...");
                var introLocation = ConfigurationManager.AppSettings["IntroLocation"];
                await Task.Delay(5000);
            }

            return "Clean up complete.";
        }
    }
}
