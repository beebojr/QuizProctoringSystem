using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QPS.API.Json;
using QPS.API.Middleware;
using QPS.Application;
using QPS.Application.Common.Interfaces;
using QPS.Data;
using QPS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new LenientTimeOnlyConverter());
        o.JsonSerializerOptions.Converters.Add(new LenientDateOnlyConverter());
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        // Same error shape as the rest of the API: { message, errors[], fieldErrors{} }.
        o.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key.TrimStart('$', '.'),
                    e => e.Value!.Errors.Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToArray());
            var messages = fieldErrors.Where(f => f.Key != "command" && f.Key != "body").SelectMany(f => f.Value).ToArray();
            if (messages.Length == 0) messages = fieldErrors.SelectMany(f => f.Value).ToArray();
            return new BadRequestObjectResult(new
            {
                status = 400,
                isSuccess = false,
                message = messages.FirstOrDefault() ?? "The request was not valid.",
                errors = messages,
                fieldErrors
            });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Quiz Proctoring System API", Version = "v1" });
    c.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the accessToken from /api/auth/login",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var origins = builder.Configuration.GetSection("App:ClientOrigins").Get<string[]>() ?? new[] { "http://localhost:3001" };
builder.Services.AddCors(options =>
    options.AddPolicy("Client", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()
        .WithExposedHeaders("Content-Disposition")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// A relative SQLite path is resolved against the API folder, wherever "dotnet run" is started from.
var connection = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=quizproctoring.db");
if (!Path.IsPathRooted(connection.DataSource) && connection.DataSource != ":memory:")
    connection.DataSource = Path.Combine(builder.Environment.ContentRootPath, connection.DataSource);
builder.Configuration["ConnectionStrings:DefaultConnection"] = connection.ToString();

builder.Services.AddApplicationServices();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Create/upgrade the database and add demo data on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    if (app.Configuration.GetValue("App:SeedDemoData", true))
    {
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        await SeedData.InitializeAsync(db, clock.Today);
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

/// <summary>Exposed for integration tests.</summary>
public partial class Program { }
