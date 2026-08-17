-- Run this FIRST, once, before re-running AddRolesAndDepartmentScoping.sql.
--
-- Your database already has the schema from every migration up through
-- AddEmployeeEmailToInternetAccessRequest (confirmed by the "already an
-- object/column" errors), but __EFMigrationsHistory has no record of
-- them. This script only INSERTs bookkeeping rows - it does not touch
-- any table or column. Safe to re-run.

-- Safety check: only proceed if the schema actually matches what all
-- seven prior migrations should have produced. If anything is missing,
-- this raises an error and inserts nothing - do not skip this.
IF OBJECT_ID(N'[dbo].[Employees]') IS NULL
   OR OBJECT_ID(N'[dbo].[InternetAccessRequests]') IS NULL
   OR OBJECT_ID(N'[dbo].[tbl_HODdetails]') IS NULL
   OR COL_LENGTH(N'dbo.Employees', N'Email') IS NULL
   OR COL_LENGTH(N'dbo.Employees', N'CellularId') IS NULL
   OR COL_LENGTH(N'dbo.InternetAccessRequests', N'EmployeeEmail') IS NULL
   OR COL_LENGTH(N'dbo.InternetAccessRequests', N'CellularId') IS NULL
BEGIN
    RAISERROR('Schema does not match the expected prior-migration state - stopping. Check which tables/columns are missing before proceeding.', 16, 1);
    RETURN;
END;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT v.MigrationId, N'10.0.10'
FROM (VALUES
    (N'20260807061636_InitialCreate'),
    (N'20260810055424_AddInternetAccessRequests'),
    (N'20260810065507_CreateInternetAccessRequests'),
    (N'20260810072751_AddEmployeeDeviceInformation'),
    (N'20260810103446_AddEmployeeDeviceInformationV2'),
    (N'20260810105007_FixEmployeeDeviceInformation'),
    (N'20260812111930_AddEmployeeEmailToInternetAccessRequest')
) AS v(MigrationId)
WHERE NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] h WHERE h.MigrationId = v.MigrationId
);
GO

-- Sanity check - should return 7 rows.
SELECT * FROM [__EFMigrationsHistory] ORDER BY [MigrationId];
