using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using Services;
using GOCore;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;

Console.WriteLine("UserService starter...");
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();
// Registrér korrekt Guid-serializer for MongoDB
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Async Vault secret loader med retry (kopieret fra AuthService)
async Task<Dictionary<string, string>> LoadVaultSecretsAsync()
{
    var retryCount = 0;
    while (true)
    {
        try
        {
            var vaultAddress = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? "http://vault:8200";
            var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? "wopwopwop123";

            Console.WriteLine($"Henter secrets fra Vault på {vaultAddress} med token...");

            var vaultClientSettings = new VaultClientSettings(vaultAddress, new TokenAuthMethodInfo(vaultToken));
            var vaultClient = new VaultClient(vaultClientSettings);

            var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: "go-authservice", // Brug samme sti som i AuthService for konsistens
                mountPoint: "secret"
            );

            Console.WriteLine("Secrets hentet fra Vault!");

            return secret.Data.Data.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.ToString() ?? ""
            );
        }
        catch (Exception ex)
        {
            retryCount++;
            if (retryCount > 5)
            {
                Console.WriteLine($"Fejl ved indlæsning af Vault secrets efter 5 forsøg: {ex.Message}");
                throw;
            }
            Console.WriteLine($"Vault ikke klar endnu, prøver igen om 3 sek... ({retryCount}/5): {ex.Message}");
            await Task.Delay(3000);
        }
    }
}

// Indlæs secrets fra Vault
var vaultSecrets = await LoadVaultSecretsAsync();
builder.Configuration.AddInMemoryCollection(vaultSecrets);

// Hent JWT konfiguration fra Vault
var secretKey = builder.Configuration["Jwt__Secret"];
var issuer = builder.Configuration["Jwt__Issuer"];
var audience = builder.Configuration["Jwt__Audience"];

// Print connection string til debug
Console.WriteLine("Mongo Connection String: " + builder.Configuration.GetConnectionString("MongoDb"));
Console.WriteLine($"Jwt__Secret fra Vault i UserService: '{secretKey}' (Length: {secretKey?.Length ?? 0})");
Console.WriteLine($"Jwt__Issuer fra Vault i UserService: '{issuer}'");
Console.WriteLine($"Jwt__Audience fra Vault i UserService: '{audience}'");


// Registrér services
builder.Services.AddSingleton<IUserDBService, UserMongoDBService>();

// Tilføj autentificering
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
        {
            throw new Exception("JWT konfiguration mangler fra Vault!");
        }
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// Tilføj autorisering
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});


// Tilføj controllers og Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UserService API", Version = "v1" });

    // Konfigurer Swagger til at inkludere JWT Bearer token autentificering
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
