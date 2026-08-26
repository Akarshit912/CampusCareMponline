# CampusCare – Testing & Quality Assurance Guide

This document details the testing architecture, test suites, test execution instructions, mocking strategies, and quality assurance standards for the **CampusCare** project.

---

## 1. Test Suite Overview

CampusCare includes an automated **xUnit** test project located at `tests/CampusCare.Tests/`. The suite provides **23 passing unit and integration tests** validating domain rules, state machines, AI fallback logic, SLA background services, and Web API controllers.

| Metric | Value |
| :--- | :--- |
| **Test Framework** | `xUnit 2.9+` |
| **Mocking Framework** | `Moq 4.20+` |
| **In-Memory Database** | `Microsoft.EntityFrameworkCore.InMemory` |
| **Total Test Count** | **23 Passing Tests** (0 Failed, 0 Skipped) |
| **Execution Duration** | $\approx 1.0$ second |

---

## 2. Test Structure & Class Breakdown

```text
tests/CampusCare.Tests
│
├── Unit
│   ├── ComplaintWorkflowTests.cs   # Domain logic, AI triage, SLA rules & state machine tests
│   └── WebAPITests.cs              # REST API controller unit tests with mocked dependencies
│
└── CampusCare.Tests.csproj         # Project configuration & package references
```

---

## 3. Test Cases Catalog

### 3.1 `ComplaintWorkflowTests` (Domain Logic & Services)

| Test Name | Test Type | Focus Area | Description |
| :--- | :--- | :--- | :--- |
| `UniqueComplaintNumberFormat_ShouldMatchPattern` | `[Fact]` | ID Generator | Verifies complaint number matches standard formatting `CMP-{Year}-{Sequence:D5}`. |
| `WorkflowStateTransition_ValidationRules` | `[Theory]` (8 cases) | State Machine | Tests all valid and invalid state transitions (`Submitted` $\rightarrow$ `Assigned`, `InProgress` $\rightarrow$ `Resolved`, `Closed` $\rightarrow$ `InProgress` forbidden, etc.). |
| `AIService_ShouldFallbackToRuleEngine_WhenAPIKeyIsEmpty` | `[Fact]` | AI Triage | Verifies that when external LLM API key is empty, the system falls back to the internal rule engine and correctly tags IT Wi-Fi issues. |
| `AIService_ShouldRouteToCorrectNonITDepartment` | `[Theory]` (4 cases) | AI Triage | Tests deterministic classification of non-IT issues (Hostel, Maintenance, Security, Transportation). |
| `EscalationService_ShouldMarkOverdueComplaintsAsEscalated` | `[Fact]` | SLA Worker | Verifies that complaints older than 48 hours are escalated, audit histories are appended, and notification events are dispatched. |

---

### 3.2 `WebAPITests` (REST API Controller Endpoints)

| Test Name | Test Type | Endpoint Tested | Description |
| :--- | :--- | :--- | :--- |
| `GetComplaints_ShouldReturnOkResult_WithListOfComplaints` | `[Fact]` | `GET /api/complaints` | Verifies `200 OK` return code and JSON complaint list mapping. |
| `GetComplaintDetails_ShouldReturnOkResult_WhenComplaintExists` | `[Fact]` | `GET /api/complaints/{id}` | Verifies complete details payload including AI summary and history timeline. |
| `GetComplaintDetails_ShouldReturnNotFound_WhenComplaintDoesNotExist` | `[Fact]` | `GET /api/complaints/{id}` | Verifies `404 Not Found` response when passing non-existent complaint ID. |
| `TriggerEscalationCheck_ShouldReturnOkResult_WithEscalatedCount` | `[Fact]` | `POST /api/complaints/escalate-overdue` | Tests triggering the SLA scan and validating the returned escalation count. |
| `DeleteComplaint_ShouldReturnOkResult_WhenComplaintExists` | `[Fact]` | `DELETE /api/complaints/{id}` | Tests successful deletion of a complaint record. |
| `DeleteComplaint_ShouldReturnNotFound_WhenComplaintDoesNotExist` | `[Fact]` | `DELETE /api/complaints/{id}` | Verifies `404 Not Found` response when attempting to delete invalid ID. |
| `ReceiveN8nWebhookCallback_ShouldReturnOkResult` | `[Fact]` | `POST /api/complaints/n8n/webhook-callback` | Validates receiving and acknowledging n8n webhook callback JSON payload. |

---

## 4. Running the Tests

### Option 1: Using the .NET CLI
Run all tests in the solution:
```powershell
dotnet test
```

Run tests with detailed output:
```powershell
dotnet test --logger "console;verbosity=detailed"
```

Run only specific test class:
```powershell
dotnet test --filter "FullyQualifiedName~ComplaintWorkflowTests"
```

### Option 2: Using Visual Studio Test Explorer
1. Open **Test** menu in Visual Studio $\rightarrow$ **Test Explorer** (`Ctrl+E, T`).
2. Click **Run All Tests in View** (`Ctrl+R, A`).
3. View green checkmarks and individual execution times for each test.

---

## 5. Testing Best Practices & Mocking Guidelines

### 5.1 The Arrange-Act-Assert (AAA) Pattern
All tests in CampusCare follow the AAA convention:
```csharp
[Fact]
public async Task SampleTest_ShouldDemonstrateAAA()
{
    // 1. Arrange: Setup mocks and test data
    var mockRepo = new Mock<IComplaintRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Complaint { Id = 1 });

    // 2. Act: Call the method under test
    var result = await mockRepo.Object.GetByIdAsync(1);

    // 3. Assert: Verify the outcome
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
}
```

### 5.2 In-Memory EF Core Database for Testing
When testing database operations, use unique In-Memory DbContext instances to avoid state pollution between test runs:
```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

using var dbContext = new ApplicationDbContext(options);
```

### 5.3 Mocking External Dependencies with Moq
Always mock external interfaces (`IConfiguration`, `INotificationService`, `IComplaintRepository`) to ensure tests remain isolated, fast, and deterministic without relying on external network connectivity or cloud APIs.
