-- READ-ONLY. Confirms the actual state of the database before any
-- further scripts run. Paste the full output back.

SELECT 'Employees table' AS Check_, CASE WHEN OBJECT_ID(N'dbo.Employees') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END AS Result
UNION ALL
SELECT 'InternetAccessRequests table', CASE WHEN OBJECT_ID(N'dbo.InternetAccessRequests') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END
UNION ALL
SELECT 'tbl_HODdetails table', CASE WHEN OBJECT_ID(N'dbo.tbl_HODdetails') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END
UNION ALL
SELECT 'Employees.Email column', CASE WHEN COL_LENGTH('dbo.Employees', 'Email') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END
UNION ALL
SELECT 'InternetAccessRequests.EmployeeEmail column', CASE WHEN COL_LENGTH('dbo.InternetAccessRequests', 'EmployeeEmail') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END
UNION ALL
SELECT 'InternetAccessRequests.DepartmentCode column (new)', CASE WHEN COL_LENGTH('dbo.InternetAccessRequests', 'DepartmentCode') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END
UNION ALL
SELECT 'RoleAssignments table (new)', CASE WHEN OBJECT_ID(N'dbo.RoleAssignments') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END
UNION ALL
SELECT '__EFMigrationsHistory table', CASE WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NOT NULL THEN 'EXISTS' ELSE 'MISSING' END;

-- What migrations does the history table currently claim are applied?
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NOT NULL
BEGIN
    SELECT * FROM [__EFMigrationsHistory] ORDER BY [MigrationId];
END

-- Are there any duplicate columns on the affected tables (matches the
-- "column name specified more than once" errors you saw)?
SELECT c.TABLE_NAME, c.COLUMN_NAME, COUNT(*) AS Occurrences
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME IN ('Employees', 'InternetAccessRequests')
GROUP BY c.TABLE_NAME, c.COLUMN_NAME
HAVING COUNT(*) > 1;
