using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SideCar.Business.Data;
using SideCar.Business.Helpers.Exceptions;
using SideCar.Business.Helpers.Mappings;
using SideCar.Business.Helpers.Settings;
using SideCar.Business.Helpers.Utilities;
using SideCar.Business.Repositories;
using SideCar.Business.Repositories.Interfaces;
using SideCar.Business.Services;
using SideCar.Business.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var cs = config.GetConnectionString("DbConnection");

builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseSqlServer(cs));

builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(cs));

builder.Services.AddHttpClient("authen", client =>
{
    client.BaseAddress = new Uri(config["SidecarAuthen:BaseUrl"]!);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.Configure<AwsSettings>(config.GetSection(AwsSettings.SectionName));
builder.Services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
var awsSettings = config.GetSection(AwsSettings.SectionName).Get<AwsSettings>()!;
var credentials = new BasicAWSCredentials(awsSettings.AccessKey ?? "testing", awsSettings.SecretKey ?? "testing");
var s3Config = new AmazonS3Config
{
    RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region),
    ForcePathStyle = awsSettings.ForcePathStyle
};
if (!string.IsNullOrEmpty(awsSettings.ServiceUrl))
    s3Config.ServiceURL = awsSettings.ServiceUrl;
builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = config["Redis:ConnectionString"];
    options.InstanceName = "SideCar:";
});

builder.Services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailPublisher, HangfireEmailPublisher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new DevDashboardAuthorizationFilter() }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
