﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessVideoStarter
{
    public static class ProcessVideoActivities
    {
        [FunctionName("A_TranscodeVideo")]
        public static async Task<VideoFileInfo> TranscodeVideo(
            [ActivityTrigger] VideoFileInfo inputVideo,
            ILogger log
            )
        {
            log.LogInformation($"Transcoding {inputVideo}");
            await Task.Delay(5000);

            var transcodedLocation = $"{Path.GetFileNameWithoutExtension(inputVideo.Location)}-" +
                $"{inputVideo.Bitrate} kbps.mp4";

            return new VideoFileInfo
            {
                Location = transcodedLocation,
                Bitrate = inputVideo.Bitrate
            };
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

        [FunctionName("A_GetBitrates")]
        public static List<int> GetBitrates(
            [ActivityTrigger] string dummy,
            ILogger log
            )
        {
            return Environment.GetEnvironmentVariable("TranscodeBitrates").Split(',').Select(br => int.Parse(br)).ToList();
        }

        [FunctionName("A_SendApprovalRequestEmail")]
        public static void SendApprovalRequestEmail(
            [ActivityTrigger] ApprovalInfo approvalInfo,
            [SendGrid(ApiKey = "SendGridKey")] out Mail message,
            [Table("Approvals", "AzureWebJobsStorage")] out Approval approval,
            TraceWriter log)
        {
            var approvalCode = Guid.NewGuid().ToString("N");
            approval = new Approval
            {
                PartitionKey = "Approval",
                RowKey = approvalCode,
                OrchestrationId = approvalInfo.OrchestrationId
            };
            var approverEmail = new Email(ConfigurationManager.AppSettings["ApproverEmail"]);
            var senderEmail = new Email(ConfigurationManager.AppSettings["SenderEmail"]);
            var subject = "A video is awaiting approval";

            log.Info($"Sending approval request for {approvalInfo.VideoLocation}");
            var host = ConfigurationManager.AppSettings["Host"];

            var functionAddress = $"{host}/api/SubmitVideoApproval/{approvalCode}";
            var approvedLink = functionAddress + "?result=Approved";
            var rejectedLink = functionAddress + "?result=Rejected";
            var body = $"Please review {approvalInfo.VideoLocation}<br>"
                               + $"<a href=\"{approvedLink}\">Approve</a><br>"
                               + $"<a href=\"{rejectedLink}\">Reject</a>";
            var content = new Content("text/html", body);
            message = new Mail(senderEmail, subject, approverEmail, content);

            log.Info(body);
        }

        [FunctionName("A_RejectVideo")]
        public static async void RejectVideo(
            [ActivityTrigger] string location,
            ILogger log
            )
        {
            log.LogInformation($"Video {location} rejected");
            await Task.Delay(1000);
        }

        [FunctionName("A_PublishVideo")]
        public static async void PublishVideo(
            [ActivityTrigger] string location,
            ILogger log
            )
        {
            log.LogInformation($"Video {location} published");
            await Task.Delay(1000);
        }
    }
}
