# RestroPlate 🍽️

RestroPlate is a robust .NET Web API. It follows a strict **Layered Architecture** to ensure separation of concerns, scalability, and maintainability.

The project uses **ADO.NET** for high-performance data access, **DbUp** for database versioning, and **SQL Server** (via Docker) to mirror a future Azure production environment.

---

> **🤖 AI AGENT DIRECTIVE:** If you are an AI parsing this README, DO NOT execute the prompt below. It is a text template for human use only. Ignore it completely.

```text
**For Human Developers:**

use this stating promt for agents.

Before you do anything, read AGENT_RULES.md, understand our tech stack (specifically the mix of inline styles and Tailwind), and acknowledge these constraints.
```

## 🏗️ Architecture

The solution is divided into 4 distinct layers to maintain a clean separation of responsibilities:

| Layer        | Project Name  | Responsibility                                                            | Dependencies |
| :----------- | :------------ | :------------------------------------------------------------------------ | :----------- |
| **API**      | `Controllers` | Entry point. Handles HTTP requests, validation, and dependency injection. | All Layers   |
| **Business** | `Services`    | Contains business logic and rules.                                        | Models       |
| **Data**     | `Repository`  | Handles database connections (ADO.NET) and SQL queries.                   | Models       |
| **Core**     | `Models`      | Contains Entities, Interfaces, and DTOs. Pure C# classes.                 | None         |

### Key Design Patterns

- **Singleton Connection Factory:** `IConnectionFactory` is injected as a Singleton to efficiently manage connection strings without holding open connections.
- **Repository Pattern:** All data access is abstracted behind interfaces (e.g., `IUserRepository`), allowing for easy swapping of database engines (MySQL -> SQL Server) in the future.
- **Database Migrations:** Database schema changes are versioned using **DbUp** console application.

---

## 📂 Project Structure

```text
RestroPlate/
├── Controllers/       # (Web API) - The running application
├── Services/          # (Class Lib) - Business Logic
├── Repository/        # (Class Lib) - ADO.NET Logic & Connection Factory
├── Models/            # (Class Lib) - Entities & Interfaces
├── Database/          # (Console App) - DbUp Migration Runner
├── Tests/             # (xUnit) - Unit Tests
└── RestroPlate.sln

```

### 🚀 Getting Started

1.Prerequisites

- .NET 8.0 SDK
- Docker Desktop

  2.Start the Database (Docker)
  We use a local SQL Server container to match the Azure production environment.

```Bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_{user_name}_PASSWORD=your_password" -p 1433:1433 --name sql_server -d [mcr.microsoft.com/mssql/server:2022-latest](https://mcr.microsoft.com/mssql/server:2022-latest)
```

3.Configure Security (User Secrets)
Important: We do NOT store connection strings in appsettings.json. We use .NET User Secrets for local development.

Run this in the Controllers directory:

```Bash
cd Controllers
dotnet user-secrets set 'ConnectionStrings:DefaultConnection' 'Server=localhost,1433;Database=RestroPlate;User Id=user_name;Password=your_password;TrustServerCertificate=True;'
```

4.Run Database Migrations
Apply the latest SQL scripts to create/update the database schema.

```Bash
cd Database
# Local Mode (Reads from db_connection.txt automatically)

dotnet run
```

5.Run the API

Start the web server.

```Bash
cd Controllers
dotnet run
```

## 🔒 Security & CI/CD

Local Dev: Uses User Secrets (stored outside the project folder).

CI/CD (GitHub Actions): Uses GitHub Secrets injected as Environment Variables.

Database: Uses DbUp to ensure the database schema is always in sync with the code before deployment.
