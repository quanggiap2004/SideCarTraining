using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using SideCar.Business;
using SideCar.Business.DTOs;
using SideCar.Business.Helpers.Constants;
using SideCar.Business.Helpers.Settings;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SideCar.Worker.Workers
{
    public class AccountCreationWorker(
        IAmazonSQS _sqs,
        IServiceScopeFactory _scopeFactory,
        IOptions<AwsSettings> _awsSettings,
        ILogger<AccountCreationWorker> _logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AccountCreationWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl            = _awsSettings.Value.AccountCreationQueueUrl,
                        MaxNumberOfMessages = ProjectConstant.MaxNumberOfMessages,
                        WaitTimeSeconds     = ProjectConstant.WaitTimeSeconds
                    }, stoppingToken);

                    if (response.Messages is null || response.Messages.Count <= 0)
                        continue;

                    var tasks = response.Messages
                        .Select(message => ProcessMessageAsync(message, stoppingToken));

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SQS polling failed, retrying in 10 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task ProcessMessageAsync(Message message, CancellationToken ct)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var authenService = scope.ServiceProvider.GetRequiredService<IAuthenService>();

            try
            {
                var envelope = JsonSerializer.Deserialize<SnsEnvelope>(message.Body)!;

                var request = JsonSerializer.Deserialize<RegisterRequest>(envelope.Message)!;

                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true))
                    throw new ValidationException(string.Join(", ", validationResults.Select(v => v.ErrorMessage)));

                //throw new Exception("Simulated DB connection failure");
                var created = await authenService.RegisterAsync(request);

                if (!created)
                    _logger.LogWarning("User {Username} already exists, skipping", request.Username);

                await _sqs.DeleteMessageAsync(_awsSettings.Value.AccountCreationQueueUrl, message.ReceiptHandle, ct);
            }
            catch (ValidationException valEx)
            {
                _logger.LogError(valEx, "Validation failed for message {MessageId}: {Errors}", message.MessageId, valEx.Message);
                await _sqs.DeleteMessageAsync(_awsSettings.Value.AccountCreationQueueUrl, message.ReceiptHandle, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId}, will be retried by SQS", message.MessageId);
            }
        }
    }
}
