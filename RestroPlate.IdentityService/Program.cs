using System.Text;
using DbUp;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RestroPlate.IdentityService.Models.Interfaces;
using RestroPlate.IdentityService.Repository;
using RestroPlate.IdentityService.Repository.Database;
using RestroPlate.IdentityService.Services;

var envCandidates = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../Controllers/.env"))
};

string? loadedEnvPath = null;
foreach (var candidate in envCandidates)
{
    if (!File.Exists(candidate))
        continue;

    Env.Load(candidate);
    loadedEnvPath = candidate;
    break;
}

if (loadedEnvPath is not null)
    Console.WriteLine($"[Config] Loaded environment variables from {loadedEnvPath}");
else
    Console.WriteLine("[Config] No .env file found. Using appsettings/environment variables.");

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

    c.OperationFilter<RestroPlate.IdentityService.Swagger.AuthorizeCheckOperationFilter>();
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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

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

var identityDbConnectionString = builder.Configuration.GetConnectionString("IdentityDb")
                                ?? builder.Configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Connection string is not configured. Set ConnectionStrings:IdentityDb or ConnectionStrings:DefaultConnection.");

Console.WriteLine("[DbUp] Ensuring Identity database exists...");
EnsureDatabase.For.SqlDatabase(identityDbConnectionString);

Console.WriteLine("[DbUp] Running schema migrations...");
var upgrader = DeployChanges.To
    .SqlDatabase(identityDbConnectionString)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly, scriptName => scriptName.Contains(".Scripts."))
    .LogToConsole()
    .Build();

var migrationResult = upgrader.PerformUpgrade();
if (!migrationResult.Successful)
{
    Console.WriteLine("[DbUp] Migration failed.");
    Console.WriteLine(migrationResult.Error);
    throw migrationResult.Error ?? new Exception("DbUp migration failed.");
}

Console.WriteLine("[DbUp] Migration completed successfully.");

app.Run();
