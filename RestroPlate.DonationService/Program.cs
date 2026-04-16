using System.Reflection;
using System.Text;
using DbUp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RestroPlate.DonationService.Models.Interfaces;
using RestroPlate.DonationService.Repository;
using RestroPlate.DonationService.Repository.Database;
using RestroPlate.DonationService.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddTransient<IConnectionFactory, ConnectionFactory>();
builder.Services.AddScoped<IDonationRepository, DonationRepository>();
builder.Services.AddScoped<IDonationRequestRepository, DonationRequestRepository>();
builder.Services.AddScoped<IDonationImageRepository, DonationImageRepository>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IDonationRequestService, DonationRequestService>();
builder.Services.AddScoped<IDonationImageService, DonationImageService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var connectionString = builder.Configuration.GetConnectionString("DonationDb")
                       ?? throw new InvalidOperationException("ConnectionStrings:DonationDb is not configured.");

EnsureDatabase.For.SqlDatabase(connectionString);

var dbUpResult = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build()
    .PerformUpgrade();

if (!dbUpResult.Successful)
{
    throw dbUpResult.Error ?? new Exception("DbUp migration failed.");
}

app.Run();
