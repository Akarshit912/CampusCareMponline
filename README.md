# CampusCare – Smart College Complaint Management System

![.NET Core](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET_Core-MVC-blue?style=for-the-badge&logo=aspnet)
![ASP.NET Core Web API](https://img.shields.io/badge/ASP.NET_Core-Web_API-green?style=for-the-badge&logo=swagger)
![EF Core](https://img.shields.io/badge/EF_Core-10.0-violet?style=for-the-badge)
![xUnit Test Suite](https://img.shields.io/badge/Tests-23_Passing-brightgreen?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-orange?style=for-the-badge)

CampusCare is an enterprise-grade academic project built using **C# 13**, **ASP.NET Core 10.0**, **Entity Framework Core**, **SQL Server / SQLite**, **xUnit**, **AI Rule Triage Engine**, and **n8n Webhook Automation**.

---

## 📖 Complete Documentation Index

| Document | Description |
| :--- | :--- |
| 🏛️ **[System Architecture](docs/ARCHITECTURE.md)** | Clean 3-tier architecture, component design, domain model, workflow state machine, and failover mechanics. |
| 🗄️ **[Database Schema & ERD](docs/DATABASE_SCHEMA.md)** | Complete ER diagram, data dictionary, foreign key constraints, indexes, and deletion strategies. |
| 🌐 **[REST API Reference](docs/API_DOCUMENTATION.md)** | OpenAPI / Swagger endpoints, request/response models, cURL examples, query parameters, and status codes. |
| 🚀 **[Setup & Deployment Guide](docs/SETUP_GUIDE.md)** | Prerequisites, local installation, database configuration (SQL Server & SQLite fallback), and Docker deployment. |
| 👨‍💼 **[Role-Based User Manual](docs/USER_MANUAL.md)** | Step-by-step guides for Students, Staff Technicians, Department Managers, and Administrators. |
| ⚡ **[n8n Workflow Automation Guide](docs/N8N_AUTOMATION_GUIDE.md)** | Multi-channel notifications (Slack/Discord/Email), webhook payloads, and automated SLA escalation cron jobs. |
| 🧪 **[Testing & QA Guide](docs/TESTING_GUIDE.md)** | 23 passing xUnit tests, state machine tests, Moq unit testing, and test coverage strategies. |
| 👥 **[Contributing & Coding Standards](docs/CONTRIBUTING.md)** | Git branching workflow, Conventional Commits, C# coding conventions, and PR review checklist. |
| 📊 **[Comprehensive Project Report](docs/PROJECT_REPORT.md)** | Full academic and engineering report covering requirements, feasibility, design, testing, and future work. |
| 📋 **[Agile Sprint Plan](docs/AGILE_SPRINT_PLAN.md)** | 10-day Agile execution plan, Scrum ownership matrix for team of 7, story points, and velocity charts. |
| 💾 **[Database DDL Script (schema.sql)](docs/schema.sql)** | Raw MS SQL Server DDL creation script for tables, foreign keys, and indexes. |

---

## 🏛️ Project Architecture

CampusCare follows clean 3-tier architecture with decoupled Web Presentation and Web API layers:

```text
CampusCare
│
├── CampusCare.sln
│
├── src
│   ├── CampusCare.Core               # Domain Entities, Enums, Interfaces, DTOs
│   │
│   ├── CampusCare.Infrastructure     # ApplicationDbContext, Repositories, Migrations, Services
│   │
│   ├── CampusCare.Mvc                # ASP.NET Core MVC Web Application (Razor Views, UI)
│   │   ├── Controllers               # Account, Admin, Home, Manager, Staff, Student Controllers
│   │   ├── ViewModels                # UI Data Binding ViewModels
│   │   ├── Views                     # Razor Views (_Layout, Dashboards, Forms)
│   │   └── wwwroot                   # Static CSS, JS, Bootstrap 5 Assets
│   │
│   └── CampusCare.WebAPI             # Standalone RESTful Web API Project
│       ├── Controllers               # ComplaintsApiController (REST Endpoints)
│       └── Program.cs                # Swagger UI OpenAPI, CORS & Web API Pipeline
│
├── tests
│   └── CampusCare.Tests              # 23 Passing xUnit Unit & Integration Tests
│
└── docs                              # Complete Project Documentation Suite
    ├── ARCHITECTURE.md               # Software Architecture & State Machine Design
    ├── DATABASE_SCHEMA.md            # ER Diagram, Data Dictionary & FK Cascade Rules
    ├── API_DOCUMENTATION.md          # REST API Endpoints, cURL & Swagger Reference
    ├── SETUP_GUIDE.md                # Developer Setup & Deployment Instructions
    ├── USER_MANUAL.md                # Operations Manual for Student/Staff/Manager/Admin
    ├── N8N_AUTOMATION_GUIDE.md       # n8n Automation & Webhook Integration
    ├── TESTING_GUIDE.md              # xUnit Test Suite & Mocking Guidelines
    ├── CONTRIBUTING.md               # Git Workflow, C# Coding Standards & PR Rules
    ├── PROJECT_REPORT.md             # Academic / Enterprise Comprehensive Project Report
    ├── AGILE_SPRINT_PLAN.md          # 10-Day Agile Sprint Plan for Team of 7
    └── schema.sql                    # Production MS SQL Server DDL Script
```

---

## ✨ Features by System Role

### 👨‍🎓 1. Student Portal
- **Complaint Submission**: File complaints with title, location, category selection, and photo attachments.
- **Auto-Generated Tracking ID**: Unique format `CMP-YYYY-00001` generated via atomic sequence logic.
- **AI Triage Integration**: Suggests optimal issue category, department, and priority level.
- **Student Dashboard**: Live status counters, audit history timeline, internal staff communication, and 1–5 star post-resolution rating system.

### 👷 2. Staff Workdesk
- **Workdesk Dashboard**: Filter complaints assigned to the logged-in staff member.
- **Workflow State Machine**: Strictly validated transitions (`Submitted` $\rightarrow$ `Assigned` $\rightarrow$ `InProgress` $\rightarrow$ `Resolved` $\rightarrow$ `Closed` / `Escalated` / `Rejected`).
- **Resolution Capture**: Record technical fix notes and resolution timestamp.

### 👔 3. Manager Console
- **Workload Tracking**: Real-time staff assignment metrics across department technicians.
- **Task Directive & Assignment**: Manual staff assignment, priority level overrides, and department directives.

### ⚙️ 4. Admin Console
- **Executive Analytics**: KPI cards, average resolution time (hours), feedback rating index, and department workload charts.
- **User Directory & Management**: Create staff members or department managers, toggle account status (Active/Deactivated).
- **Master Data Management**: Department & Category CRUD management.
- **Record Management**: Single complaint deletion and configurable bulk purge of past closed records older than 0, 7, 30, 90, or 180 days.

---

## 🤖 AI Engine, SLA Worker & n8n Automation

1. **AI Analysis Engine (`AIService`)**: Fallback rule engine that automatically analyzes complaint title and description text to route issues to 7 campus departments (`Facility Maintenance`, `Information Technology`, `Hostel Administration`, `Campus Security`, `Transport & Fleet`, `Library Services`, `General Administration`).
2. **SLA Escalation Worker (`EscalationBackgroundService`)**: Hosted background service running hourly checks. Complaints unresolved after 48 hours are automatically flagged `IsEscalated` and logged in audit history.
3. **n8n Automation Integration**: Web API exposes endpoints (`/api/complaints/escalate-overdue` and `/api/complaints/n8n/webhook-callback`) for external webhook dispatchers and automated notifications.

---

## 🔑 Default Seed Demo Accounts

All accounts are pre-seeded with the password: `Password123!`

| Role | Email Address | Assigned Department | Primary Access |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@college.com` | System Wide | Executive Analytics & User Management |
| **Manager** | `manager@college.com` | Information Technology | Staff Workload & Directives |
| **Staff (IT)** | `staff1@college.com` | Information Technology | IT Workdesk & Issue Resolution |
| **Staff (Maintenance)**| `staff2@college.com` | Facility Maintenance | Maintenance Workdesk |
| **Student 1** | `student1@college.com` | N/A | Submit Complaints & Provide Ratings |
| **Student 2** | `student2@college.com` | N/A | Submit Complaints & Track Issues |

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server / LocalDB or SQLite (built-in fallback)

### 1. Build the Solution
```powershell
dotnet build
```

### 2. Run the Unit Test Suite
```powershell
dotnet test
```
*(Runs 23 passing xUnit tests covering workflow rules, unique ID sequence generator, AI engine, and Web API endpoints)*

### 3. Launch the MVC Web Application
```powershell
dotnet run --project src/CampusCare.Mvc
```
- Open browser to **`http://localhost:5000`**

### 4. Launch the Standalone Web API & Swagger UI
```powershell
dotnet run --project src/CampusCare.WebAPI
```
- Open browser to **`http://localhost:5001`** to access **Swagger UI**

---

## 🌐 Web API Quick Reference

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/complaints` | Returns JSON list of complaints (supports `?status=` and `?departmentId=`) |
| `GET` | `/api/complaints/{id}` | Returns complaint details, AI triage output, and audit history |
| `POST` | `/api/complaints/escalate-overdue` | Triggers SLA escalation check (> 48h) for n8n cron jobs |
| `POST` | `/api/complaints/n8n/webhook-callback` | Receives n8n webhook callback payloads |
| `DELETE` | `/api/complaints/{id}` | Permanently deletes a complaint record by ID |

*For complete API documentation with payloads and cURL examples, see **[API_DOCUMENTATION.md](docs/API_DOCUMENTATION.md)**.*
