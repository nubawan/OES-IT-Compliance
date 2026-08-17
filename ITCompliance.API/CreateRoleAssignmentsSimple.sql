SET IMPLICIT_TRANSACTIONS OFF;
GO

IF COL_LENGTH('dbo.InternetAccessRequests', 'DepartmentCode') IS NULL
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [DepartmentCode] nvarchar(30) NULL DEFAULT N'';
END;
GO

IF OBJECT_ID('dbo.RoleAssignments') IS NULL
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

    CREATE INDEX [IX_RoleAssignments_EmployeeId] ON [RoleAssignments] ([EmployeeId]);
    CREATE INDEX [IX_RoleAssignments_Role_DepartmentCode] ON [RoleAssignments] ([Role], [DepartmentCode]);
END;
GO

SELECT @@TRANCOUNT AS should_be_zero;
SELECT * FROM RoleAssignments;
