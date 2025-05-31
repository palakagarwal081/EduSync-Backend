using EduSync.Backend.Data;
using EduSync.Backend.Models;
using EduSync.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Optional: Load local production config for local testing
builder.Configuration.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 32;
    });

// Configure EF Core with environment-based connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string not found.");
}
builder.Services.AddDbContext<ProjectDBContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    }));

// Configure Blob Storage Service
builder.Services.AddScoped<BlobStorageService>();

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.AspNetCore.Cors", LogLevel.Information);

// JWT Authentication from environment variables
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT configuration missing in environment variables.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// CORS setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", cors =>
    {
        cors.WithOrigins("https://lively-mud-088e4e700.6.azurestaticapps.net")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ProjectDBContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}

app.Run();

