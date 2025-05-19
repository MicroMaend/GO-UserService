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

Console.WriteLine("Appen starter...");
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();
// Registrér korrekt Guid-serializer for MongoDB
BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

var builder = WebApplication.CreateBuilder(args);


builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Print connection string til debug
Console.WriteLine("Mongo Connection String: " + builder.Configuration.GetConnectionString("MongoDb"));

// Registrér services
builder.Services.AddSingleton<IUserDBService, UserMongoDBService>();

// Tilføj controllers og Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Service API", Version = "v1" });
});

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
        c.RoutePrefix = string.Empty; // Kør swagger direkte på root
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
