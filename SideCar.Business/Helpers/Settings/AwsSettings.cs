namespace SideCar.Business.Helpers.Settings
{
    public class AwsSettings
    {
        public const string SectionName = "Aws";
        public string? ServiceUrl { get; set; }
        public string BucketName { get; set; } = "email-templates";
        public string Region { get; set; } = "us-east-1";
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public bool ForcePathStyle { get; set; }
        public string AccountCreationQueueUrl { get; set; } = string.Empty;
        public string DlqUrl { get; set; } = string.Empty;
    }
}
