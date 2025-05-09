using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;
using Services;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
.GetCurrentClassLogger();
logger.Debug("start min service");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers();

builder.Services.AddSingleton<IUserDBService, UserMongoDBService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v0.9",
        Title = "UserService API",
        Description = "An ASP.NET Core Web API for managing Users",
        TermsOfService = new Uri("https://bilka.dk"),
        Contact = new OpenApiContact
        {
            Name = "Hvem ka?",
            Url = new Uri("https://bilka.dk")
        },
        License = new OpenApiLicense
        {
            Name = "Salling Group",
            Url = new Uri("https://sallinggroup.com/")
        }
    });
});


builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();