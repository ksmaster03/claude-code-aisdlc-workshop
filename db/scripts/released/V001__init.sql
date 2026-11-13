-- V001__init.sql
-- Released with OpsTrack 1.0 on 2026-03-02. DO NOT EDIT: this script has already run on production.
-- New changes go to db/scripts/pending/ as V003__..., V004__...
--
-- Illustrative SQL Server script for the lab. The lab application itself runs on SQLite and
-- creates its schema with EF Core (Database.EnsureCreated), so this file is not executed by the app.

CREATE TABLE dbo.Lines (
    Id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Lines PRIMARY KEY,
    Code    NVARCHAR(20)  NOT NULL,
    Name    NVARCHAR(100) NOT NULL
);
CREATE UNIQUE INDEX IX_Lines_Code ON dbo.Lines (Code);

CREATE TABLE dbo.Equipment (
    Id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Equipment PRIMARY KEY,
    Code    NVARCHAR(30)  NOT NULL,
    Name    NVARCHAR(100) NOT NULL,
    LineId  INT NOT NULL CONSTRAINT FK_Equipment_Lines_LineId REFERENCES dbo.Lines (Id)
);
CREATE UNIQUE INDEX IX_Equipment_Code ON dbo.Equipment (Code);
CREATE INDEX IX_Equipment_LineId ON dbo.Equipment (LineId);

CREATE TABLE dbo.WorkRequests (
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkRequests PRIMARY KEY,
    Number       NVARCHAR(20)   NOT NULL,
    EquipmentId  INT NOT NULL CONSTRAINT FK_WorkRequests_Equipment_EquipmentId REFERENCES dbo.Equipment (Id),
    Title        NVARCHAR(200)  NOT NULL,
    Description  NVARCHAR(4000) NULL,
    Priority     NVARCHAR(2)    NOT NULL,
    Status       NVARCHAR(20)   NOT NULL,
    ReportedBy   NVARCHAR(50)   NOT NULL,
    AssignedTo   NVARCHAR(50)   NULL,
    CreatedAt    DATETIME2      NOT NULL,
    ClosedAt     DATETIME2      NULL,
    ClosedBy     NVARCHAR(50)   NULL
);
CREATE INDEX IX_WorkRequests_EquipmentId ON dbo.WorkRequests (EquipmentId);
CREATE INDEX IX_WorkRequests_Number ON dbo.WorkRequests (Number);
CREATE INDEX IX_WorkRequests_Status_CreatedAt ON dbo.WorkRequests (Status, CreatedAt);

CREATE TABLE dbo.RunningNumbers (
    [Key]   NVARCHAR(20) NOT NULL CONSTRAINT PK_RunningNumbers PRIMARY KEY,
    Value   INT NOT NULL
);

CREATE TABLE dbo.AuditLogs (
    Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    Entity    NVARCHAR(50)   NOT NULL,
    EntityId  NVARCHAR(50)   NOT NULL,
    Action    NVARCHAR(30)   NOT NULL,
    [By]      NVARCHAR(50)   NOT NULL,
    At        DATETIME2      NOT NULL,
    Detail    NVARCHAR(1000) NULL
);
