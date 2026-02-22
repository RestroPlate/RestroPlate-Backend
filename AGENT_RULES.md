# AGENT INSTRUCTIONS FOR RESTROPLATE BACKEND

## 1. Project Context & Tech Stack

- **Language:** C#
- **Framework:** .NET (ASP.NET Core Web API)
- **Architecture:** N-Tier / Clean Architecture Pattern
- **Database/Migrations:** DbUp (SQL Scripts) & `Microsoft.Data.SqlClient`
- **Testing:** xUnit

## 2. Global Directives (The Brakes)

- **DO NOT** assume or invent project structure. Always verify the current directory tree.
- **DO NOT** combine architectural layers. Never write SQL queries in Controllers or Services.
- **DO NOT** install Entity Framework (EF Core) or other large ORMs unless explicitly requested. We rely on the existing `IConnectionFactory` and lightweight data access.
- **NEVER** overwrite GitHub Actions workflows (`.github/workflows/azure-deploy.yml`) without explicit permission.

## 3. Architecture & Dependency Flow (STRICT)

The solution is divided into specific projects. Dependencies flow strictly in one direction.

- **`/Controllers`**: The API Layer. Handles HTTP requests and responses. Injects and calls `Services`. NO business logic. NO data access.
- **`/Services`**: The Business Logic Layer. Injects and calls `Repository` interfaces. Handles validation and business rules.
- **`/Repository`**: The Data Access Layer. Implements interfaces from `Models`. Contains actual SQL queries and database interaction using `IConnectionFactory`.
- **`/Models`**: The Domain Layer. Contains plain C# classes (Entities, DTOs) and all Interfaces (e.g., `IUserRepository`, `IConnectionFactory`). This project has NO dependencies on other projects.
- **`/Database`**: Database migration project using DbUp. All new database schema changes MUST be added here as `.sql` files in the `/scripts` folder (e.g., `002_AddTable.sql`).
- **`/Tests`**: xUnit testing project.

## 4. Coding Style & Formatting

- **Naming Conventions:** Use `PascalCase` for classes, interfaces (prefix with `I`), methods, and properties. Use `camelCase` for local variables and method parameters. Prefix private fields with an underscore (e.g., `_userRepository`).
- **Dependency Injection:** Always use constructor injection. Register new Services and Repositories in `Program.cs` or the appropriate dependency injection setup.
- **Async/Await:** Always use asynchronous programming (`Task<T>`, `async`, `await`) for database calls and I/O operations.

## 5. Refactoring Limits

- If a user request requires modifying more than 3 files simultaneously, pause and provide a short bulleted architectural summary of your plan before generating code.
