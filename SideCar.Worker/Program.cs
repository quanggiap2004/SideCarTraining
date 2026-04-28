using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using SideCar.Business.Helpers.Exceptions;
using SideCar.Business.Helpers.Settings;
using SideCar.Business.Services;
using EmailService = SideCar.Business.Services.EmailService;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    var config = hostContext.Configuration;
    var cs = config.GetConnectionString("DbConnection");

    services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(cs));
    services.AddHangfireServer();

    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = config["Redis:ConnectionString"];
        options.InstanceName = "SideCar:";
    });

    services.Configure<AwsSettings>(config.GetSection(AwsSettings.SectionName));
    services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
    var awsSettings = config.GetSection(AwsSettings.SectionName).Get<AwsSettings>()!;
    var credentials = new BasicAWSCredentials(awsSettings.AccessKey ?? "testing", awsSettings.SecretKey ?? "testing");
    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region),
        ForcePathStyle = awsSettings.ForcePathStyle
    };
    if (!string.IsNullOrEmpty(awsSettings.ServiceUrl))
        s3Config.ServiceURL = awsSettings.ServiceUrl;
    services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));
    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails();
    services.AddScoped<IEmailService, EmailService>();
});

var host = builder.Build();
host.Run();
