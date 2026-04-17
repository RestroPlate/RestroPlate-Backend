using System.Reflection;
using System.Text;
using DbUp;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RestroPlate.InventoryService.Consumers;
using RestroPlate.InventoryService.Models.Interfaces;
using RestroPlate.InventoryService.Repository;
using RestroPlate.InventoryService.Repository.Database;
using RestroPlate.InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? "amqp://guest:guest@localhost:5672/";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

var jwtSecret = builder.Configuration["JWT:Secret"]
                ?? throw new InvalidOperationException("JWT:Secret is not configured.");
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "RestroPlate";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "RestroPlate";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumer<DonationCreatedConsumer>();

    configurator.UsingRabbitMq((context, busConfigurator) =>
    {
        busConfigurator.Host(new Uri(rabbitMqConnectionString));

        busConfigurator.ReceiveEndpoint("donation-created-events", endpointConfigurator =>
        {
            endpointConfigurator.ConfigureConsumer<DonationCreatedConsumer>(context);
        });
    });
});

builder.Services.AddTransient<IConnectionFactory, ConnectionFactory>();
builder.Services.AddScoped<IInventoryLogRepository, InventoryLogRepository>();
builder.Services.AddScoped<IDonationClaimRepository, DonationClaimRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDonationClaimService, DonationClaimService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var connectionString = builder.Configuration.GetConnectionString("InventoryDb")
                       ?? throw new InvalidOperationException("ConnectionStrings:InventoryDb is not configured.");

EnsureDatabase.For.SqlDatabase(connectionString);

var dbUpResult = DeployChanges.To.SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!dbUpResult.Successful)
    throw dbUpResult.Error ?? new Exception("DbUp migration failed.");

app.Run();
