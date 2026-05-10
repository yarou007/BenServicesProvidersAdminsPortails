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

var smtpSettings = new SmtpSettings
{
    Host = builder.Configuration["SMTP_HOST"] ?? builder.Configuration["Smtp:Host"] ?? string.Empty,
    Port = int.TryParse(builder.Configuration["SMTP_PORT"] ?? builder.Configuration["Smtp:Port"], out var smtpPort)
        ? smtpPort
        : 587,
    User = builder.Configuration["SMTP_USER"] ?? builder.Configuration["Smtp:User"] ?? string.Empty,
    Password = builder.Configuration["SMTP_PASSWORD"] ?? builder.Configuration["Smtp:Password"] ?? string.Empty,
    FromEmail = builder.Configuration["SMTP_FROM_EMAIL"] ?? builder.Configuration["Smtp:FromEmail"] ?? string.Empty,
    FromName = builder.Configuration["SMTP_FROM_NAME"] ?? builder.Configuration["Smtp:FromName"] ?? "Ben's Services",
    FrontendLoginUrl = builder.Configuration["FRONTEND_LOGIN_URL"]
        ?? builder.Configuration["Smtp:FrontendLoginUrl"]
        ?? "http://localhost:4200/login"
};

builder.Services.AddSingleton(smtpSettings);
builder.Services.AddScoped<IPasswordHasher<AdminEntity>, PasswordHasher<AdminEntity>>();
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
