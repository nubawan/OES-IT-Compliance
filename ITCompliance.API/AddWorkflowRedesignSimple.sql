SET IMPLICIT_TRANSACTIONS OFF;
GO

IF COL_LENGTH('dbo.InternetAccessRequests', 'PendingDepartmentCode') IS NULL
BEGIN
    ALTER TABLE [InternetAccessRequests] ADD [PendingDepartmentCode] nvarchar(30) NULL;
END;
GO

IF OBJECT_ID('dbo.RequestTransactions') IS NULL
BEGIN
    CREATE TABLE [RequestTransactions] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [StageStatus] nvarchar(60) NOT NULL,
        [ActorEmpId] nvarchar(25) NOT NULL,
        [ActorRole] nvarchar(30) NOT NULL,
        [Action] nvarchar(20) NOT NULL,
        [Remarks] nvarchar(max) NULL,
        [ActionedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_RequestTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RequestTransactions_InternetAccessRequests_RequestId]
            FOREIGN KEY ([RequestId]) REFERENCES [InternetAccessRequests] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_RequestTransactions_RequestId] ON [RequestTransactions] ([RequestId]);
END;
GO

IF OBJECT_ID('dbo.EmailLogs') IS NULL
BEGIN
    CREATE TABLE [EmailLogs] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] int NOT NULL,
        [RecipientEmail] nvarchar(320) NOT NULL,
        [Purpose] nvarchar(60) NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Success] bit NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [SentAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EmailLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmailLogs_InternetAccessRequests_RequestId]
            FOREIGN KEY ([RequestId]) REFERENCES [InternetAccessRequests] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_EmailLogs_RequestId] ON [EmailLogs] ([RequestId]);
END;
GO

SELECT @@TRANCOUNT AS should_be_zero;
