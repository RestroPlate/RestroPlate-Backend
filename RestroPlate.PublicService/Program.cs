using System.Reflection;
using DotNetEnv;
using DbUp;
using MassTransit;
using RestroPlate.EventContracts;
using RestroPlate.PublicService.Consumers;
using RestroPlate.PublicService.Models;

var localEnvPath = Path.Combine(Directory.GetCurrentDirectory(), "local.env");
if (File.Exists(localEnvPath))
{
    Env.Load(localEnvPath);
}

var builder = WebApplication.CreateBuilder(args);

var publicDbConnectionString = builder.Configuration.GetConnectionString("PublicDb")
    ?? throw new InvalidOperationException("Connection string 'PublicDb' is not configured.");

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? throw new InvalidOperationException("Connection string 'RabbitMQ' is not configured.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ConnectionFactory>();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<InventoryPublishedConsumer>();

    configurator.UsingRabbitMq((context, busConfigurator) =>
    {
        busConfigurator.Host(new Uri(rabbitMqConnectionString));

        busConfigurator.ReceiveEndpoint("inventory-published-events", endpointConfigurator =>
        {
            endpointConfigurator.ConfigureConsumer<InventoryPublishedConsumer>(context);
        });
    });
});

EnsureDatabase.For.SqlDatabase(publicDbConnectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(publicDbConnectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var migrationResult = upgrader.PerformUpgrade();
if (!migrationResult.Successful)
{
    throw migrationResult.Error;
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
