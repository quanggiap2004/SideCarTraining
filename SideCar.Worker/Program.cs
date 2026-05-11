using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using SideCar.Business.Data;
using SideCar.Business.Helpers.Exceptions;
using SideCar.Business.Helpers.Settings;
using SideCar.Business.Repositories;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;
using SideCar.Business.Services.Interfaces;
using SideCar.Worker.Workers;
using SideCar.Business.Jobs;
using SideCar.Business;
using EmailService = SideCar.Business.Services.EmailService;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    var config = hostContext.Configuration;
    var cs = config.GetConnectionString("DbConnection");

    services.AddDbContext<ProjectDbContext>(options => options.UseSqlServer(cs));

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

    var sqsConfig = new AmazonSQSConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region)
    };
    if (!string.IsNullOrEmpty(awsSettings.ServiceUrl))
        sqsConfig.ServiceURL = awsSettings.ServiceUrl;
    services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(credentials, sqsConfig));

    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IEmailPublisher, HangfireEmailPublisher>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IAuthenService, AuthenService>();
    services.AddScoped<DeactivateInactiveAccountsJob>();
    services.AddScoped<WarnInactiveAccountsJob>();

    services.AddHostedService<AccountCreationWorker>();
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<WarnInactiveAccountsJob>(
        recurringJobId: "warn-inactive-accounts",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: Cron.Daily(1));

    recurringJobs.AddOrUpdate<DeactivateInactiveAccountsJob>(
        recurringJobId: "deactivate-inactive-accounts",
        methodCall: job => job.ExecuteAsync(),
        cronExpression: Cron.Daily(2));
}

host.Run();
