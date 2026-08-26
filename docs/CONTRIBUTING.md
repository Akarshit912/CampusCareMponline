# CampusCare – Contributing & Coding Standards Guide

Thank you for contributing to **CampusCare**! This guide outlines the development standards, Git workflow, coding conventions, and pull request guidelines to ensure consistency and code quality across the team.

---

## 1. Git Workflow & Branching Strategy

We follow a structured **GitFlow-inspired branching strategy**:

```text
main (Production Release Ready)
 └── develop (Active Integration Branch)
      ├── feature/student-portal
      ├── feature/ai-triage-gemini
      ├── feature/staff-workdesk
      ├── fix/sqlite-locking-bug
      └── docs/api-swagger-update
```

### Branch Naming Conventions
- **Feature Branches**: `feature/<feature-name>` (e.g. `feature/manager-workload-chart`)
- **Bugfix Branches**: `fix/<bug-description>` (e.g. `fix/null-department-id`)
- **Documentation Branches**: `docs/<topic>` (e.g. `docs/n8n-setup-guide`)
- **Refactoring / Performance**: `refactor/<module-name>`

### Branch Rules
1. Never commit directly to `main`.
2. All feature branches must branch off `develop` and merge back into `develop` via Pull Requests.
3. Every PR requires at least **1 peer review** and a **100% green test run** before merging.

---

## 2. Commit Message Standards

We adopt the **Conventional Commits** format:

```text
<type>(<scope>): <short description in present tense>

[optional body explaining rationale]

[optional issue reference]
```

### Commit Types
- `feat`: A new feature or capability (e.g., `feat(student): add photo upload during complaint submission`)
- `fix`: A bug fix (e.g., `fix(api): handle null complaint history in details endpoint`)
- `docs`: Documentation updates (e.g., `docs(api): add cURL request examples`)
- `style`: Formatting, missing semi-colons, no code logic change
- `refactor`: Refactoring production code without changing behavior
- `test`: Adding or correcting tests (e.g., `test(workflow): add theory cases for invalid state transitions`)
- `chore`: Updating build scripts, package versions, or tooling

---

## 3. C# & .NET 10 Coding Standards

### 3.1 Naming Conventions
- **Classes, Enums, Methods, Properties**: `PascalCase`
  ```csharp
  public class ComplaintRepository : IComplaintRepository
  public async Task<Complaint?> GetByIdAsync(int id)
  ```
- **Local Variables & Parameters**: `camelCase`
  ```csharp
  var complaintRecord = await _context.Complaints.FindAsync(id);
  ```
- **Private Readonly Fields**: `_camelCase` with leading underscore
  ```csharp
  private readonly ApplicationDbContext _context;
  private readonly ILogger<ComplaintsApiController> _logger;
  ```
- **Interfaces**: Prefixed with `I` (e.g., `IComplaintRepository`, `IAIService`).

### 3.2 Asynchronous Programming
- Use `async` and `await` for all I/O operations (database calls, HTTP requests, file access).
- Async methods must end with the `Async` suffix (e.g., `GetAllAsync()`, `SaveComplaintAsync()`).
- Avoid blocking calls like `.Result` or `.Wait()`.

### 3.3 Dependency Injection & Clean Architecture
- Always register services via interfaces in `Program.cs` (`builder.Services.AddScoped<I..., ...>()`).
- Inject dependencies via constructor injection.
- Keep controllers thin by encapsulating domain and persistence logic within Repositories and Services.

---

## 4. Pull Request (PR) Checklist

Before submitting a Pull Request for review, verify that:

- [ ] The solution builds with **0 errors and 0 warnings** (`dotnet build`).
- [ ] All **23 xUnit tests pass** locally (`dotnet test`).
- [ ] New methods and endpoints are accompanied by corresponding unit tests.
- [ ] If database entities or schemas were modified:
  - [ ] Migration is created and tested.
  - [ ] `docs/DATABASE_SCHEMA.md` and `docs/schema.sql` are updated.
- [ ] No sensitive credentials, connection strings, or personal API keys are committed in code or configuration files.
