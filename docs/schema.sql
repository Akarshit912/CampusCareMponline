-- ===============================================================================
-- CampusCare Database Schema DDL Script
-- Target Database Engine: Microsoft SQL Server 2019+ / LocalDB / Azure SQL
-- Generated for CampusCare - Smart College Complaint Management System
-- ===============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'CampusCareDb')
BEGIN
    CREATE DATABASE CampusCareDb;
END
GO

USE CampusCareDb;
GO

-- 1. Departments Table
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(10) NOT NULL,
        Description NVARCHAR(300) NULL,
        CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (Id ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Departments_Code ON dbo.Departments(Code ASC);
END
GO

-- 2. Complaint Categories Table
IF OBJECT_ID(N'dbo.ComplaintCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComplaintCategories (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(300) NULL,
        DefaultDepartmentId INT NOT NULL,
        CONSTRAINT PK_ComplaintCategories PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_ComplaintCategories_Departments FOREIGN KEY (DefaultDepartmentId) 
            REFERENCES dbo.Departments(Id) ON DELETE NO ACTION
    );
END
GO

-- 3. Identity Users Table (AspNetUsers)
IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id NVARCHAR(450) NOT NULL,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL,
        TwoFactorEnabled BIT NOT NULL,
        LockoutEnd DATETIMEOFFSET(7) NULL,
        LockoutEnabled BIT NOT NULL,
        AccessFailedCount INT NOT NULL,
        FullName NVARCHAR(100) NOT NULL DEFAULT N'',
        DepartmentId INT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT PK_AspNetUsers PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_AspNetUsers_Departments FOREIGN KEY (DepartmentId) 
            REFERENCES dbo.Departments(Id) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX IX_AspNetUsers_Email ON dbo.AspNetUsers(NormalizedEmail ASC);
END
GO

-- 4. Complaints Table
IF OBJECT_ID(N'dbo.Complaints', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Complaints (
        Id INT IDENTITY(1,1) NOT NULL,
        ComplaintNumber NVARCHAR(20) NOT NULL,
        Title NVARCHAR(150) NOT NULL,
        Description NVARCHAR(2000) NOT NULL,
        Location NVARCHAR(100) NOT NULL,
        Status INT NOT NULL DEFAULT 1,
        Priority INT NOT NULL DEFAULT 2,
        CategoryId INT NOT NULL,
        DepartmentId INT NOT NULL,
        StudentId NVARCHAR(450) NOT NULL,
        AssignedStaffId NVARCHAR(450) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NULL,
        ResolvedAt DATETIME2 NULL,
        ClosedAt DATETIME2 NULL,
        IsEscalated BIT NOT NULL DEFAULT 0,
        EscalatedAt DATETIME2 NULL,
        EscalationReason NVARCHAR(300) NULL,
        ResolutionDetails NVARCHAR(2000) NULL,
        CONSTRAINT PK_Complaints PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_Complaints_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.ComplaintCategories(Id),
        CONSTRAINT FK_Complaints_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
        CONSTRAINT FK_Complaints_Student FOREIGN KEY (StudentId) REFERENCES dbo.AspNetUsers(Id),
        CONSTRAINT FK_Complaints_AssignedStaff FOREIGN KEY (AssignedStaffId) REFERENCES dbo.AspNetUsers(Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Complaints_ComplaintNumber ON dbo.Complaints(ComplaintNumber ASC);
    CREATE NONCLUSTERED INDEX IX_Complaints_Status ON dbo.Complaints(Status ASC);
    CREATE NONCLUSTERED INDEX IX_Complaints_StudentId ON dbo.Complaints(StudentId ASC);
    CREATE NONCLUSTERED INDEX IX_Complaints_DepartmentId ON dbo.Complaints(DepartmentId ASC);
END
GO

-- 5. AI Analyses Table
IF OBJECT_ID(N'dbo.AIAnalyses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIAnalyses (
        Id INT IDENTITY(1,1) NOT NULL,
        ComplaintId INT NOT NULL,
        SuggestedCategory NVARCHAR(100) NOT NULL,
        SuggestedPriority INT NOT NULL,
        SuggestedDepartment NVARCHAR(100) NOT NULL,
        GeneratedSummary NVARCHAR(300) NOT NULL,
        AnalyzedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModelUsed NVARCHAR(100) NOT NULL,
        ConfidenceScore FLOAT NOT NULL DEFAULT 0.85,
        CONSTRAINT PK_AIAnalyses PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_AIAnalyses_Complaints FOREIGN KEY (ComplaintId) 
            REFERENCES dbo.Complaints(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_AIAnalyses_ComplaintId ON dbo.AIAnalyses(ComplaintId ASC);
END
GO

-- 6. Feedbacks Table
IF OBJECT_ID(N'dbo.Feedbacks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Feedbacks (
        Id INT IDENTITY(1,1) NOT NULL,
        ComplaintId INT NOT NULL,
        StudentId NVARCHAR(450) NOT NULL,
        Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        Comment NVARCHAR(500) NULL,
        SubmittedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_Feedbacks PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_Feedbacks_Complaints FOREIGN KEY (ComplaintId) 
            REFERENCES dbo.Complaints(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Feedbacks_Student FOREIGN KEY (StudentId) 
            REFERENCES dbo.AspNetUsers(Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX IX_Feedbacks_ComplaintId ON dbo.Feedbacks(ComplaintId ASC);
END
GO

-- 7. Complaint Comments Table
IF OBJECT_ID(N'dbo.ComplaintComments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComplaintComments (
        Id INT IDENTITY(1,1) NOT NULL,
        ComplaintId INT NOT NULL,
        UserId NVARCHAR(450) NOT NULL,
        CommentText NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IsInternalOnly BIT NOT NULL DEFAULT 0,
        CONSTRAINT PK_ComplaintComments PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_ComplaintComments_Complaints FOREIGN KEY (ComplaintId) 
            REFERENCES dbo.Complaints(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ComplaintComments_Users FOREIGN KEY (UserId) 
            REFERENCES dbo.AspNetUsers(Id)
    );
END
GO

-- 8. Complaint Histories Table
IF OBJECT_ID(N'dbo.ComplaintHistories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComplaintHistories (
        Id INT IDENTITY(1,1) NOT NULL,
        ComplaintId INT NOT NULL,
        ChangedByUserId NVARCHAR(450) NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        OldStatus INT NULL,
        NewStatus INT NOT NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Notes NVARCHAR(1000) NULL,
        CONSTRAINT PK_ComplaintHistories PRIMARY KEY CLUSTERED (Id ASC),
        CONSTRAINT FK_ComplaintHistories_Complaints FOREIGN KEY (ComplaintId) 
            ... REFERENCES dbo.Complaints(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ComplaintHistories_Users FOREIGN KEY (ChangedByUserId) 
            REFERENCES dbo.AspNetUsers(Id)
    );
END
GO
