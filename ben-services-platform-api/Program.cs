using System.Text;
using BenServicesPlatform.Api.Data;
using BenServicesPlatform.Api.Entities;
using BenServicesPlatform.Api.Services;
using BenServicesPlatform.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BenServicesDatabase")
    ?? throw new InvalidOperationException("Connection string 'BenServicesDatabase' is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
});

var jwtSettings = new JwtSettings
{
    Secret = builder.Configuration["JWT_SECRET"] ?? builder.Configuration["Jwt:Secret"] ?? string.Empty,
    Issuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty,
    Audience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? string.Empty,
    ExpirationMinutes = int.TryParse(
        builder.Configuration["JWT_EXPIRATION_MINUTES"] ?? builder.Configuration["Jwt:ExpirationMinutes"],
        out var expiration)
        ? expiration
        : 60
};

jwtSettings.Validate();
builder.Services.AddSingleton(jwtSettings);

string ResolveConfigValue(params string[] keys)
{
    foreach (var key in keys)
    {
        var value = builder.Configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return string.Empty;
}

var smtpSettings = new SmtpSettings
{
    Host = ResolveConfigValue("SMTP_HOST", "MAIL_HOST", "Smtp:Host"),
    Port = int.TryParse(builder.Configuration["SMTP_PORT"] ?? builder.Configuration["Smtp:Port"], out var smtpPort)
        ? smtpPort
        : 587,
    User = ResolveConfigValue(
        "SMTP_USER",
        "SMTP_USERNAME",
        "SMTP_LOGIN",
        "MAIL_USER",
        "MAIL_USERNAME",
        "Smtp:User",
        "Smtp:Username"),
    Password = ResolveConfigValue(
        "SMTP_PASSWORD",
        "SMTP_PASS",
        "MAIL_PASSWORD",
        "BREVO_SMTP_KEY",
        "Smtp:Password"),
    FromEmail = ResolveConfigValue(
        "SMTP_FROM_EMAIL",
        "SMTP_FROM",
        "SMTP_SENDER",
        "MAIL_FROM",
        "Smtp:FromEmail",
        "Smtp:Sender"),
    FromName = ResolveConfigValue("SMTP_FROM_NAME", "MAIL_FROM_NAME", "Smtp:FromName", "Smtp:SenderName"),
    FrontendLoginUrl = ResolveConfigValue("FRONTEND_LOGIN_URL", "Smtp:FrontendLoginUrl")
};

if (string.IsNullOrWhiteSpace(smtpSettings.FromName))
{
    smtpSettings.FromName = "Ben's Services";
}

if (string.IsNullOrWhiteSpace(smtpSettings.FrontendLoginUrl))
{
    smtpSettings.FrontendLoginUrl = "http://localhost:4200/login";
}

builder.Services.AddSingleton(smtpSettings);
builder.Services.AddScoped<IPasswordHasher<AdminEntity>, PasswordHasher<AdminEntity>>();
builder.Services.AddScoped<IPasswordHasher<ProviderAccountEntity>, PasswordHasher<ProviderAccountEntity>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var corsOrigins = new List<string>
{
    "http://localhost:4200",
    "https://ben-services-providers-admins-portails-971pky84o.vercel.app",
    "https://ben-services-providers-admins-porta.vercel.app"
};

var configuredOrigins = builder.Configuration["CORS_ALLOWED_ORIGINS"];
if (!string.IsNullOrWhiteSpace(configuredOrigins))
{
    corsOrigins.AddRange(
        configuredOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

var allowedOrigins = corsOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AngularApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminEntity>>();

    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext, app.Configuration, environment, passwordHasher);
}

app.Run();
