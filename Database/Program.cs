using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DbUp;

class Program
{
    static int Main(string[] args)
    {
        string connectionString = "";

        // PRIORITY 1: Check if GitHub Actions passed a connection string
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            connectionString = args[0];
            Console.WriteLine("Mode: CI/CD (Using Argument)");
        }
        // PRIORITY 2: If no arg, try to read local ignored file
        else
        {
            // Look for the file in the same directory as the running application
            string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_connection.txt");

            // If running from source (dotnet run), sometimes we need to look up a few levels
            // This fallback checks the project root if the bin folder doesn't have it
            if (!File.Exists(localFile))
            {
                localFile = Path.Combine(Directory.GetCurrentDirectory(), "db_connection.txt");
            }

            if (File.Exists(localFile))
            {
                connectionString = File.ReadAllText(localFile).Trim();
                Console.WriteLine("Mode: Local Development (Using db_connection.txt)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CRITICAL ERROR: No connection string found!");
                Console.WriteLine("1. CI/CD: Pass connection string as an argument.");
                Console.WriteLine("2. Local: Create a 'db_connection.txt' file with your connection string.");
                Console.ResetColor();
                return -1; // Fail the process
            }
        }

        // --- Execute Migrations ---
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }
}