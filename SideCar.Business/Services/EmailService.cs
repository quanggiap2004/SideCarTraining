using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MimeKit;
using SideCar.Business.DTOs;
using SideCar.Business.Helpers.Settings;
using System.Text;


namespace SideCar.Business.Services
{
    public class EmailService(IOptions<AwsSettings> _awsSetting, IOptions<EmailSettings> _emailSettings, IAmazonS3 _s3Client, IDistributedCache _cache) : IEmailService
    {
        public async Task SendEmail(string email, string subject, string message)
        {
            Console.WriteLine("Sending email to " + email + " at " + DateTime.Now.ToString("HH:mm:ss"));
            await Task.Delay(5000);
            await SendEmailAsync(email, subject, message);
            Console.WriteLine("Finished send email to " + email + " at " + DateTime.Now.ToString("HH:mm:ss"));
        }

        public async Task SendEmailTemplate(TemplateEmailRequest request)
        {
            string emailMessage = await GetRenderedTemplate(request.TemplateName, request.Placeholders);
            await SendEmail(request.Email, request.Subject, emailMessage);
        }

        public async Task<string> GetRenderedTemplate(string templateName, Dictionary<string, string> placeholders)
        {
            var cacheKey = CacheKey(templateName);
            var cachedItem = await _cache.GetStringAsync(cacheKey);
            if(cachedItem is not null)
            {
                return RenderPlaceholders(cachedItem, placeholders);
            }

            var request = new GetObjectRequest
            {
                BucketName = _awsSetting.Value.BucketName,
                Key = S3Key(templateName)
            };
            using var response = await _s3Client.GetObjectAsync(request);
            using var reader = new StreamReader(response.ResponseStream);
            var templateHtml = await reader.ReadToEndAsync();

            await _cache.SetStringAsync(cacheKey, templateHtml, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(10)
            });

            return RenderPlaceholders(templateHtml, placeholders);
        }

        public async Task UploadEmailTemplateAsync(string templateName, Stream fileStream)
        {
            var request = new PutObjectRequest
            {
                BucketName = _awsSetting.Value.BucketName,
                Key = S3Key(templateName),
                InputStream = fileStream,
                ContentType = "text/html",
            };
            await _s3Client.PutObjectAsync(request);
            await _cache.RemoveAsync(CacheKey(templateName));
        }

        
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var smtp = _emailSettings.Value;

            var messageModel = new MimeMessage();
            messageModel.Subject = subject;
            messageModel.From.Add(new MailboxAddress("Sidecar", smtp.From));
            messageModel.To.Add(new MailboxAddress("User", email));
            messageModel.Body = new TextPart("html") { Text = message };

            using var client = new SmtpClient();
            client.Connect(smtp.Host, smtp.Port, false);
            if (!string.IsNullOrEmpty(smtp.Username))
                client.Authenticate(smtp.Username, smtp.Password);
            client.Send(messageModel);
            client.Disconnect(true);
        }

        private static string RenderPlaceholders(string template, Dictionary<string, string> placeholders)
        {
            foreach (var kvp in placeholders)
                template = template.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            return template;
        }

        private static string CacheKey(string templateName) => $"email-template:{templateName}";
        private static string S3Key(string templateName) => $"templates/{templateName}.html";
    }
}
