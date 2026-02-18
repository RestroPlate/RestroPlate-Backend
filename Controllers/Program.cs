using RestroPlate.Models.Interfaces;
using RestroPlate.Repository.Database;
using RestroPlate.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// REGISTER THE CONNECTION FACTORY
builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>();

// REGISTER THE REPOSITORY
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// --- OPTIONAL: Add Swagger UI (If you want the visual test page) ---
if (app.Environment.IsDevelopment())
{
    // If you are using .NET 9 Preview "AddOpenApi", you might need:
    // app.MapOpenApi(); 
    // BUT for standard development, we usually just rely on direct URLs first.
}

// app.UseHttpsRedirection();

// --- 2. ADD THIS LINE ---
// This tells the app to actually USE the controllers it found to handle requests
app.MapControllers();

app.Run();