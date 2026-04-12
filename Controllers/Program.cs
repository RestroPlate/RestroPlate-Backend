using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RestroPlate.Models.Interfaces;
using RestroPlate.Repository;
using RestroPlate.Repository.Database;
using Microsoft.OpenApi;
using RestroPlate.Services;

// Load .env file at the very beginning
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ── CORS ─────────────────────────────────────────────────────────────────────
// Origins are environment-driven:
//   Dev  → appsettings.Development.json  Cors:AllowedOrigins (localhost ports)
//   Prod → appsettings.json Cors:AllowedOrigins  (injected by CI pipeline via jq)
const string CorsPolicyName = "RestroPlatePolicy";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            // Fail-safe: no origins configured means nothing is allowed.
            // A deliberate open wildcard policy would require explicitly opting in.
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // needed when the frontend sends the Authorization header
        }
    });
});

// ── Controllers & API Docs ──────────────────────────────────────────────────
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

    c.OperationFilter<RestroPlate.Controllers.Swagger.AuthorizeCheckOperationFilter>();
});

// ── JWT Authentication ──────────────────────────────────────────────────────
// ASP.NET Core maps JWT__Secret (.env / env var) → JWT:Secret in IConfiguration,
// same way ConnectionStrings__DefaultConnection → ConnectionStrings:DefaultConnection.
var jwtSecret  = builder.Configuration["JWT:Secret"]
                 ?? throw new InvalidOperationException("JWT:Secret is not configured. Add JWT__Secret to .env for dev.");
var jwtIssuer   = builder.Configuration["JWT:Issuer"]   ?? "RestroPlate";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "RestroPlate";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// ── Dependency Injection ────────────────────────────────────────────────────
builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDonationRepository, DonationRepository>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IDonationRequestRepository, DonationRequestRepository>();
builder.Services.AddScoped<IDonationRequestService, DonationRequestService>();
// new — inventory log tracking for DC collect actions
builder.Services.AddScoped<IInventoryLogRepository, InventoryLogRepository>();
// new — donation claim feature
builder.Services.AddScoped<IDonationClaimRepository, DonationClaimRepository>();
builder.Services.AddScoped<IDonationClaimService, DonationClaimService>();
// new — donation image upload feature
builder.Services.AddScoped<IDonationImageRepository, DonationImageRepository>();
builder.Services.AddScoped<IDonationImageService, DonationImageService>();

// ── Build ───────────────────────────────────────────────────────────────────
var app = builder.Build();

// Serve uploaded files (e.g. /uploads/donations/...) as static content
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware order: CORS → Authentication → Authorization
// UseCors MUST come before UseAuthentication
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
