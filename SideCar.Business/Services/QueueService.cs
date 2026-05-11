using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SideCar.Business.Helpers.Settings;
using SideCar.Business.Services.Interfaces;

namespace SideCar.Business.Services
{
    public class QueueService(
        IAmazonSQS sqs,
        IOptions<AwsSettings> awsSettings,
        ILogger<QueueService> logger) : IQueueService
    {
        public async Task<int> RedriveDlqAsync()
        {
            var dlqUrl = awsSettings.Value.DlqUrl;
            var mainQueueUrl = awsSettings.Value.AccountCreationQueueUrl;

            var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = dlqUrl,
                MaxNumberOfMessages = 10
            });

            var redriven = 0;
            foreach (var message in response.Messages)
            {
                try
                {
                    await sqs.SendMessageAsync(mainQueueUrl, message.Body);
                    await sqs.DeleteMessageAsync(dlqUrl, message.ReceiptHandle);
                    redriven++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to redrive message {MessageId}", message.MessageId);
                }
            }

            return redriven;
        }
    }
}
