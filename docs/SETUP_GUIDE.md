# CampusCare – Developer Setup & Deployment Guide

This guide provides step-by-step instructions to configure, build, run, test, and deploy the **CampusCare** solution in local development and production environments.

---

## 1. Prerequisites

Ensure your development machine meets the following requirements:

| Tool | Minimum Version | Required For | Download / Link |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `10.0+` | Core Runtime & Compiler | [.NET 10 Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **IDE / Editor** | Visual Studio 2026 / VS Code / Rider | Code Editing & Debugging | [Visual Studio](https://visualstudio.microsoft.com/) |
| **Database** | SQL Server 2022 / LocalDB / SQLite | Persistence | LocalDB comes with Visual Studio |
| **Git** | `2.40+` | Version Control | [Git SCM](https://git-scm.com/) |
| **cURL / Postman** | Any | Testing REST API | [Postman](https://www.postman.com/) |

> [!NOTE]
> CampusCare has **automatic SQLite fallback**. If MS SQL Server or LocalDB is not running on your machine, the system will automatically initialize and connect to a local `campuscare.db` SQLite file without requiring manual intervention!

---

## 2. Quick Local Setup

### Step 1: Clone or Open the Repository
```powershell
cd c:\Users\LENOVO\Desktop\CampusCare\CampusCare
```

### Step 2: Restore Dependencies & Build Solution
```powershell
dotnet restore
dotnet build
```

### Step 3: Run the Automated Unit Tests
```powershell
dotnet test
```
Expected output:
```text
Passed! - Failed: 0, Passed: 23, Skipped: 0, Total: 23
```

---

## 3. Running the Applications

CampusCare consists of two runnable host applications:

### Option A: Running the MVC Web Application
The user-facing web portal with Razor Views and Bootstrap 5 UI:
```powershell
dotnet run --project src/CampusCare.Mvc
```
- Open your browser to: **`http://localhost:5000`** or **`https://localhost:5001`**

### Option B: Running the Standalone Web API & Swagger UI
The RESTful API providing JSON endpoints:
```powershell
dotnet run --project src/CampusCare.WebAPI
```
- Open your browser to: **`http://localhost:5001`** (Swagger UI is mounted at root `/`)

### Option C: Running Both Simultaneously in Visual Studio
- Right click `CampusCare.sln` $\rightarrow$ **Set Startup Projects...**
- Select **Multiple startup projects**:
  - `CampusCare.Mvc` $\rightarrow$ **Start**
  - `CampusCare.WebAPI` $\rightarrow$ **Start**
- Press `F5` to start debugging both simultaneously.

---

## 4. Configuration & AppSettings

Configuration settings are stored in `appsettings.json` within `CampusCare.Mvc` and `CampusCare.WebAPI`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CampusCareDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
    "SqliteConnection": "Data Source=campuscare.db"
  },
  "AISettings": {
    "ApiKey": "",
    "Endpoint": "",
    "Model": "gemini-1.5-flash"
  },
  "n8nSettings": {
    "GeneralWebhookUrl": "",
    "NewComplaintWebhookUrl": "",
    "ResolvedWebhookUrl": "",
    "EscalatedWebhookUrl": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Configuring External AI (Google Gemini / OpenAI)
To enable external LLM categorization:
1. Obtain an API key from [Google AI Studio](https://aistudio.google.com/) or OpenAI.
2. Update `AISettings:ApiKey` with your key.
3. Update `AISettings:Endpoint` with the endpoint URL.
4. *If left empty, CampusCare automatically uses its built-in rule-based keyword triage engine.*

### Configuring n8n Webhook Notifications
To dispatch event webhooks to n8n workflows:
1. Create a Webhook trigger node in n8n.
2. Copy the Webhook test/production URL.
3. Paste the URL into `n8nSettings:NewComplaintWebhookUrl` or `EscalatedWebhookUrl`.

---

## 5. Seed Data & Demo Accounts

On application startup, `DbInitializer.SeedAsync()` automatically executes:
- Creates all database tables if they do not exist.
- Seeds 7 operational departments (`IT`, `MAINT`, `HOSTEL`, `LIB`, `SEC`, `TRANS`, `ADMIN`).
- Seeds 10 standard complaint categories.
- Seeds 6 test user accounts.

### Default Credentials

All accounts share the password: **`Password123!`**

| Email | Role | Department | Default View |
| :--- | :--- | :--- | :--- |
| `admin@college.com` | `Admin` | System-Wide | Executive Analytics & User Management |
| `manager@college.com` | `Manager` | Information Technology | Staff Workload & Directives |
| `staff1@college.com` | `Staff` | Information Technology | IT Tech Workdesk |
| `staff2@college.com` | `Staff` | Facility Maintenance | Maintenance Workdesk |
| `student1@college.com` | `Student` | None | File Complaint & My Complaints |
| `student2@college.com` | `Student` | None | File Complaint & My Complaints |

---

## 6. Database Migrations & Resetting

### Adding a New Migration
```powershell
dotnet ef migrations add <MigrationName> --project src/CampusCare.Infrastructure --startup-project src/CampusCare.Mvc
```

### Applying Migrations to Database
```powershell
dotnet ef database update --project src/CampusCare.Infrastructure --startup-project src/CampusCare.Mvc
```

### Resetting SQLite Database
To reset the SQLite database during testing:
1. Stop the application.
2. Delete the `campuscare.db` file from `src/CampusCare.Mvc/`.
3. Re-run `dotnet run --project src/CampusCare.Mvc`. The database will automatically recreate and re-seed.

---

## 7. Production Deployment Guidelines

### Publishing the MVC Project
```powershell
dotnet publish src/CampusCare.Mvc -c Release -o ./publish/mvc
```

### Publishing the Web API Project
```powershell
dotnet publish src/CampusCare.WebAPI -c Release -o ./publish/api
```

### Docker Containerization (Optional)
To package either project as a Docker container, use a standard multi-stage .NET 10 Dockerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CampusCare.Mvc/CampusCare.Mvc.csproj", "CampusCare.Mvc/"]
COPY ["src/CampusCare.Infrastructure/CampusCare.Infrastructure.csproj", "CampusCare.Infrastructure/"]
COPY ["src/CampusCare.Core/CampusCare.Core.csproj", "CampusCare.Core/"]
RUN dotnet restore "CampusCare.Mvc/CampusCare.Mvc.csproj"
COPY src/ .
WORKDIR "/src/CampusCare.Mvc"
RUN dotnet build "CampusCare.Mvc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CampusCare.Mvc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CampusCare.Mvc.dll"]
```
