using RestroPlate.Models.Interfaces;
using RestroPlate.Repository.Database;
using RestroPlate.Repository;

var builder = WebApplication.CreateBuilder(args);

// Register services.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Register connections
builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Add Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();