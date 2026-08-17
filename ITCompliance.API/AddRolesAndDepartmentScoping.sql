IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807061636_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Department] nvarchar(max) NOT NULL,
        [Designation] nvarchar(max) NOT NULL,
        [Role] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260807061636_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260807061636_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810055424_AddInternetAccessRequests'
)
BEGIN
    CREATE TABLE [InternetAccessRequests] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] nvarchar(max) NOT NULL,
        [Website] nvarchar(max) NOT NULL,
        [Reason] nvarchar(max) NOT NULL,
        [Duration] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [ITOfficerRemarks] nvarchar(max) NULL,
        [HODRemarks] nvarchar(max) NULL,
        [BossRemarks] nvarchar(max) NULL,
        [SecurityHeadRemarks] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InternetAccessRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810055424_AddInternetAccessRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810055424_AddInternetAccessRequests', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810065507_CreateInternetAccessRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810065507_CreateInternetAccessRequests', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810072751_AddEmployeeDeviceInformation'
)
BEGIN
    ALTER TABLE [Employees] ADD [CellularId] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810072751_AddEmployeeDeviceInformation'
)
BEGIN
    ALTER TABLE [Employees] ADD [DeviceName] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810072751_AddEmployeeDeviceInformation'
)
BEGIN
    ALTER TABLE [Employees] ADD [IpAddress] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810072751_AddEmployeeDeviceInformation'
)
BEGIN
    ALTER TABLE [Employees] ADD [LanLaptopId] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810072751_AddEmployeeDeviceInformation'
)
BEGIN
    ALTER TABLE [Employees] ADD [LanMacId] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810072751_AddEmployeeDeviceInformation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810072751_AddEmployeeDeviceInformation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810103446_AddEmployeeDeviceInformationV2'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [CellularId] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810103446_AddEmployeeDeviceInformationV2'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [DeviceName] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810103446_AddEmployeeDeviceInformationV2'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [IpAddress] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810103446_AddEmployeeDeviceInformationV2'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [LanLaptopId] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810103446_AddEmployeeDeviceInformationV2'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [LanMacId] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810103446_AddEmployeeDeviceInformationV2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810103446_AddEmployeeDeviceInformationV2', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260810105007_FixEmployeeDeviceInformation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260810105007_FixEmployeeDeviceInformation', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812111930_AddEmployeeEmailToInternetAccessRequest'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [EmployeeEmail] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812111930_AddEmployeeEmailToInternetAccessRequest'
)
BEGIN
    ALTER TABLE [Employees] ADD [Email] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812111930_AddEmployeeEmailToInternetAccessRequest'
)
BEGIN
    CREATE TABLE [tbl_HODdetails] (
        [Id] int NOT NULL IDENTITY,
        [DeptCode] nvarchar(max) NOT NULL,
        [DeptName] nvarchar(max) NOT NULL,
        [HODEmpID] nvarchar(max) NOT NULL,
        [HODName] nvarchar(max) NOT NULL,
        [HODEmail] nvarchar(max) NOT NULL,
        [DirectorEmpId] nvarchar(max) NULL,
        [DirectorName] nvarchar(max) NULL,
        CONSTRAINT [PK_tbl_HODdetails] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812111930_AddEmployeeEmailToInternetAccessRequest'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812111930_AddEmployeeEmailToInternetAccessRequest', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817174202_AddRolesAndDepartmentScoping'
)
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [DepartmentCode] nvarchar(30) NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817174202_AddRolesAndDepartmentScoping'
)
BEGIN
    CREATE TABLE [RoleAssignments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] nvarchar(25) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [DepartmentCode] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByEmpId] nvarchar(max) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByEmpId] nvarchar(max) NULL,
        CONSTRAINT [PK_RoleAssignments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817174202_AddRolesAndDepartmentScoping'
)
BEGIN
    CREATE INDEX [IX_RoleAssignments_EmployeeId] ON [RoleAssignments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817174202_AddRolesAndDepartmentScoping'
)
BEGIN
    CREATE INDEX [IX_RoleAssignments_Role_DepartmentCode] ON [RoleAssignments] ([Role], [DepartmentCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817174202_AddRolesAndDepartmentScoping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817174202_AddRolesAndDepartmentScoping', N'10.0.10');
END;

COMMIT;
GO

