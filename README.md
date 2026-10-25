# PaperFlow

> A research paper management web application for discovering publications, organizing a personal library, and tracking reading progress.

![.NET Version](https://img.shields.io/badge/.NET-10-purple)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core_MVC-10-blue)
![Database](https://img.shields.io/badge/Database-SQL_Server-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

> **Project context:** Developed for the ASP.NET Fundamentals course. The clean-clone installation test is still pending; the setup steps below will be verified before submission.

## Table of Contents

- [About the Project](#about-the-project)
- [Technologies Used](#technologies-used)
- [Prerequisites](#prerequisites)
- [Clean Installation Verification](#clean-installation-verification)
- [Project Structure](#project-structure)
- [Architecture Overview](#architecture-overview)
- [Features](#features)
- [Usage](#usage)
- [Database Setup](#database-setup)
- [Configuration](#configuration)
- [License](#license)
- [Contact](#contact)

## About the Project

PaperFlow helps students and researchers keep track of scientific publications without maintaining a separate collection of bookmarks, PDFs, and reading notes. Users can search arXiv, import publications, manually create paper records, and manage a private research library. Reading status, page progress, deadlines, notes, ratings, and statistics make it easier to follow ongoing research work.

PaperFlow uses ASP.NET Core MVC, Entity Framework Core with SQL Server, and ASP.NET Core Identity. It includes a public landing page and information pages, while personal research functions require an account.

## Technologies Used

| Technology | Version / Details | Purpose |
|---|---|---|
| C# / .NET | .NET 10 (`net10.0`) | Application runtime and language |
| ASP.NET Core MVC | .NET 10 | Controllers and Razor Views |
| ASP.NET Core Identity | .NET 10 | Registration, authentication and account management |
| Entity Framework Core | 10.x | SQL Server ORM, relationships, and migrations |
| Microsoft SQL Server / SQL Server LocalDB | Local installation | Persistent relational database |
| Bootstrap | Bootstrap 5 | Responsive page layout and forms |
| Razor Views and Razor Pages | ASP.NET Core | MVC UI and Identity account pages |
| JavaScript / jQuery Validation | Bundled frontend assets | Interactive search and form validation |
| Chart.js | Statistics dashboard | Library and reading activity charts |
| Modernize | Customized Bootstrap-based theme | Application layout, sidebar and dark mode |
| arXiv API | External service | Publication search and import |

> Exact third-party frontend package versions and their license notices should be checked against the committed `wwwroot` assets before release.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with ASP.NET and web development workload, or VS Code with the C# extension
- SQL Server Express, Developer Edition, or SQL Server LocalDB (on Windows)
- [Git](https://git-scm.com/)
- An internet connection for restoring NuGet packages and searching arXiv
- `dotnet-ef` CLI tool if applying migrations using the command line

## Getting Started

### 1. Clone the repository

```powershell
git clone https://github.com/ninovvas/paperFlow-asp-net-core-app.git
cd paperFlow-asp-net-core-app
```

### 2. Restore dependencies

Run from the repository root:

```powershell
dotnet restore PaperFlow.Web/PaperFlow.Web.csproj
```

### 3. Configure SQL Server

The application reads the connection string named **`DefaultConnection`**. For a local Windows installation using SQL Server LocalDB, you can provide this configuration in `PaperFlow.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PaperFlowDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

The same configuration can be supplied through an environment variable named `ConnectionStrings__DefaultConnection`. Do not commit credentials or machine-specific private configuration.

> A valid default connection string should also exist in the tracked configuration so a reviewer can start the application using the documented local defaults. The current committed values still need to be verified during the clean installation test.

### 4. Apply the existing migrations

If the EF Core CLI tool is not installed:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
```

Then run from the repository root:

```powershell
dotnet ef database update --project PaperFlow.Data/PaperFlow.Data.csproj --startup-project PaperFlow.Web/PaperFlow.Web.csproj --context PowerFlowDbContext
```

The repository already includes migrations for ASP.NET Core Identity and the PaperFlow domain models. **Do not generate a new `InitialCreate` migration for a fresh installation.**

### 5. Run the application

```powershell
dotnet run --project PaperFlow.Web/PaperFlow.Web.csproj --launch-profile https
```

The configured development URLs are:

- **HTTPS:** `https://localhost:7184`
- **HTTP:** `http://localhost:5205`

If the local HTTPS development certificate is not trusted, use:

```powershell
dotnet dev-certs https --trust
```

On first launch, open the public landing page and register an account to access personal research features.

## Clean Installation Verification

This section describes a **fresh installation on Windows** using SQL Server LocalDB. It is intended to verify that the submitted GitHub repository can be downloaded and run without changing application source code. **The procedure is documented but has not yet been independently completed on a clean machine.**

### 1. Prepare a clean environment

Install the .NET 10 SDK, Git, and SQL Server LocalDB (or another supported SQL Server instance). The EF Core command-line tool is needed for database migrations. Check the installations:

```powershell
dotnet --version
git --version
sqllocaldb info
```

If `sqllocaldb` is not recognized, install SQL Server Express LocalDB or use a configured SQL Server instance. The database server must be available before applying migrations.

### 2. Clone a fresh copy

Do not use an existing Visual Studio working directory or copy a previously created database. Use a new folder:

```powershell
mkdir PaperFlow-Clean-Test
cd PaperFlow-Clean-Test
git clone https://github.com/ninovvas/paperFlow-asp-net-core-app.git
cd paperFlow-asp-net-core-app
```

### 3. Restore and build

```powershell
dotnet restore PaperFlow.Web/PaperFlow.Web.csproj
dotnet build PaperFlow.Web/PaperFlow.Web.csproj --no-incremental
```

The build should finish with no compilation errors. Review any NuGet security or analyzer warnings rather than suppressing them.

### 4. Configure the database connection

The application expects `ConnectionStrings:DefaultConnection`. When using the **LocalDB** example below, no SQL Server password is necessary because Windows authentication is used:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=PaperFlowDb;Trusted_Connection=True;TrustServerCertificate=True"
```

These environment variables apply only to the **current PowerShell session**. Keep using the same session for the migration command and application run command. For SQL Server Express or Developer Edition, substitute an appropriate connection string. Do not commit passwords or private connection strings.

A clone must provide a usable default configuration or follow the documented environment-variable setup; no C# source edits should be necessary.

### 5. Apply the existing EF Core migrations

If necessary, install the EF Core CLI version compatible with EF Core 10:

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
```

If the tool is already installed, verify it with `dotnet ef --version`. From the repository root run:

```powershell
dotnet ef database update --project PaperFlow.Data/PaperFlow.Data.csproj --startup-project PaperFlow.Web/PaperFlow.Web.csproj --context PowerFlowDbContext
```

This command creates or updates the SQL Server database using the **existing project migrations**. It does **not** require creating a new migration. Inspect the resulting database in SQL Server Management Studio if desired.

### 6. Start PaperFlow

```powershell
dotnet dev-certs https --trust
dotnet run --project PaperFlow.Web/PaperFlow.Web.csproj --launch-profile https
```

Open `https://localhost:7184` (as configured in the current development launch profile). Confirm that the app starts without modifying source files.

### 7. Troubleshooting

| Issue | What to check |
|---|---|
| `dotnet` not recognized | Install the .NET 10 SDK and restart the terminal. |
| `dotnet ef` not recognized | Install `dotnet-ef` and check the CLI tools path. |
| SQL Server connection error | Check LocalDB installation/instance name, Windows authentication, and `DefaultConnection`. |
| Migration cannot locate context | Execute the command from the repository root with the supplied `--project`, `--startup-project` and `--context` parameters. |
| HTTPS certificate warning | Run `dotnet dev-certs https --trust`. |
| Port already in use | Stop another PaperFlow instance or use the documented development profile settings. |
| arXiv rate limiting (HTTP 429) | Retry later; it is an external API limit, not a SQL Server installation failure. |


## Project Structure

```text
PaperFlow.Common/                 Shared constants and enums
PaperFlow.Data.Models/            Paper, Author, Category, ResearchSource and link entities
PaperFlow.Data/                   EF Core DbContext, SQL Server registration and migrations
PaperFlow.Services/               Research search, import, CRUD and statistics services
  Interfaces/                     Service contracts
  Models/                         Service-level data transfer models
  Providers/                      arXiv provider and request coordination
PaperFlow.Web/                    ASP.NET Core MVC presentation project
  Controllers/                    MVC actions and library search endpoints
  ViewModels/                     Form and view-specific models
  Views/                          Razor MVC views and partials
  Areas/Identity/                 ASP.NET Core Identity pages
  wwwroot/                        CSS, JavaScript, themes and static resources
  appsettings.json                Application configuration
  Program.cs                      Dependency injection, middleware and routes
  PaperFlow.Web.slnx              Visual Studio solution
```

## Architecture Overview

PaperFlow follows a layered **ASP.NET Core MVC** architecture with a clear separation between presentation, business logic, data access, and domain models. This structure keeps the project easier to maintain, extend, and explain during evaluation.

### Solution layers

- **PaperFlow.Web**: presentation layer with MVC controllers, Razor views, Identity pages, navigation, and client-side interaction.
- **PaperFlow.Services**: business logic for paper management, search integration, reading progress, validation, and statistics.
- **PaperFlow.Data**: data access layer with Entity Framework Core, `PowerFlowDbContext`, and migrations.
- **PaperFlow.Data.Models**: domain and database entities such as `Paper`, `Author`, `Category`, `ResearchSource`, and join entities.
- **PaperFlow.Common**: shared constants and reusable helper values used across the solution.

### Architecture diagram

The project architecture is showed in the diagram below:

![PaperFlow Architecture](PaperFlow.Web/docs/images/paperflow-architecture.png)


### Request and data flow

1. The user interacts with **PaperFlow.Web** through MVC pages and forms.
2. Controllers call **PaperFlow.Services** to execute application logic.
3. Services use **PaperFlow.Data** to read and write data with Entity Framework Core.
4. **PaperFlow.Data.Models** define the entities stored in SQL Server.
5. For external search, the services layer communicates with **arXiv**. Planned future integrations include **Crossref** and **OpenAlex**.

### Database schema

PaperFlow uses SQL Server together with Entity Framework Core Code-First. The main domain tables are:

- `Papers`
- `Authors`
- `Categories`
- `ResearchSources`
- `PaperAuthors`
- `PaperCategories`
- `AspNetUsers`

## Database Schema

![PaperFlow Database Schema](./PaperFlow.Web/docs/images/paperflow-database-schema.png)


### Supported and planned sources

- **arXiv**: currently integrated and used for paper search/import.
- **Crossref**: planned for future DOI and metadata integration.
- **OpenAlex**: planned for future works, authors, and citation metadata support.

## Features

### Available

- Account registration, login, logout, profile management and password change using ASP.NET Core Identity.
- Public landing page, About PaperFlow and Research Sources information pages.
- arXiv publication search and import, including controls for external API rate limits.
- Manual paper creation and complete paper CRUD (create, view, edit, delete).
- Personal library with search by title, DOI, authors, category and abstract; filtering, sorting and pagination.
- Header search with suggestions for saved papers.
- Author and category associations, DOI normalization and duplicate protection.
- Reading status, reading deadline, completed progress, current/total pages, ratings and personal notes.
- Research statistics such as monthly additions, completed papers, categories and sources.
- Server-side form validation and client-side validation, with responsive dark/light themes.
- Protected personal data using authorization and user-scoped database queries.

### Planned but not implemented

- Crossref integration for DOI and publication metadata.
- OpenAlex integration for works, author and citation metadata.

> The availability of external arXiv requests depends on its service limits and network connectivity.

## Usage

1. Visit `/` to read about PaperFlow, available features and planned research sources.
2. Register a user account and log in.
3. Use **Search Papers** to find publications on arXiv and import selected results.
4. Use **Add Paper Manually** to create a record without an external provider.
5. Open **My Papers** to filter, search, open, edit or delete papers.
6. Open an individual paper to update **Reading Information**, including progress, rating and notes.
7. Open **Statistics** to review personal research activity and library distributions.
8. Use the header search to locate papers already stored in your own library.

> Screenshots may be added under `docs/` after final UI review, for example `docs/dashboard.png` and `docs/library.png`.

## Database Setup

PaperFlow follows the Entity Framework Core Code-First approach and uses `PowerFlowDbContext` in `PaperFlow.Data`.

Main entities:

- `Paper`: publication metadata, user ownership, reading progress and notes.
- `Author` and `PaperAuthor`: authors and many-to-many relationships.
- `Category` and `PaperCategory`: research categories and many-to-many relationships.
- `ResearchSource`: origin of imported papers.
- ASP.NET Core Identity tables: accounts and authentication.

The `DefaultConnection` configuration key is required. Apply the **existing** migrations with:

```powershell
dotnet ef database update --project PaperFlow.Data/PaperFlow.Data.csproj --startup-project PaperFlow.Web/PaperFlow.Web.csproj --context PowerFlowDbContext
```

Database initialization has not yet been independently verified against a clean SQL Server instance. The project does not require a documented seed-data step unless one is added before submission.

## Configuration

| Setting | Required | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Yes | SQL Server or LocalDB connection string used by `PowerFlowDbContext` |
| `ASPNETCORE_ENVIRONMENT` | For development profile | `Development` for local developer settings |
| `ASPNETCORE_URLS` | Optional | Overrides listening URLs if not using a launch profile |
| `ConnectionStrings__DefaultConnection` | Optional alternative | Environment-variable representation of `DefaultConnection` |

Example using PowerShell environment variables for the current session:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=PaperFlowDb;Trusted_Connection=True;TrustServerCertificate=True"
```

No arXiv API key is required for the current arXiv integration. Avoid committing SQL credentials, tokens, passwords, User Secrets contents or personal data. Environment variables and ASP.NET Core User Secrets can be used for private development settings.

## License

PaperFlow is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for the full terms. Copyright (c) 2026 Vasil Ninov.

Third-party components and frontend assets retain their own licenses.

## Contact

- **Author:** Vasil Ninov
- **Project repository:** [PaperFlow on GitHub](https://github.com/ninovvas/paperFlow-asp-net-core-app)


