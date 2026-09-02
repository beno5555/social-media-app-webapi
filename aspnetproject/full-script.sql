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
    WHERE [MigrationId] = N'20260803140901_RebaselineSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803140901_RebaselineSnapshot', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803141151_AddLastUpdatedAt'
)
BEGIN
    ALTER TABLE [Users] ADD [LastUpdatedAt] datetime2(3) NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803141151_AddLastUpdatedAt'
)
BEGIN
    ALTER TABLE [Posts] ADD [LastUpdatedAt] datetime2(3) NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803141151_AddLastUpdatedAt'
)
BEGIN
    ALTER TABLE [Messages] ADD [LastUpdatedAt] datetime2(3) NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803141151_AddLastUpdatedAt'
)
BEGIN
    ALTER TABLE [Friendships] ADD [LastUpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803141151_AddLastUpdatedAt'
)
BEGIN
    ALTER TABLE [Comments] ADD [LastUpdatedAt] datetime2(3) NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803141151_AddLastUpdatedAt'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803141151_AddLastUpdatedAt', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803142350_AddLogsTable'
)
BEGIN
    CREATE TABLE [Logs] (
        [Id] int NOT NULL IDENTITY,
        [AuthorizedRequest] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UserId] int NULL,
        [Succeeded] bit NOT NULL DEFAULT CAST(1 AS bit),
        [Action] nvarchar(100) NOT NULL,
        [Details] nvarchar(1000) NULL,
        [EntityName] nvarchar(50) NOT NULL,
        [EntityId] int NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [LastUpdatedAt] datetime2(3) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Logs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Logs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803142350_AddLogsTable'
)
BEGIN
    CREATE INDEX [IX_Logs_UserId] ON [Logs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260803142350_AddLogsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260803142350_AddLogsTable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812175500_AddedRefreshToken'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nvarchar(44) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [LastUpdatedAt] datetime2(3) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812175500_AddedRefreshToken'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812175500_AddedRefreshToken'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812175500_AddedRefreshToken'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812175500_AddedRefreshToken', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826194309_FriendshipSentAtMigration'
)
BEGIN
    ALTER TABLE [Friendships] ADD [SentAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826194309_FriendshipSentAtMigration'
)
BEGIN
    UPDATE Friendships SET SentAt = CreatedAt
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260826194309_FriendshipSentAtMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260826194309_FriendshipSentAtMigration', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827131559_AddedMessageIndexx'
)
BEGIN
    CREATE INDEX [IX_Messages_SenderUserId_ReceiverUserId_CreatedAt] ON [Messages] ([SenderUserId], [ReceiverUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827131559_AddedMessageIndexx'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827131559_AddedMessageIndexx', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827132513_RemoveReceiverIdx'
)
BEGIN
    DROP INDEX [IX_Messages_ReceiverUserId] ON [Messages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827132513_RemoveReceiverIdx'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827132513_RemoveReceiverIdx', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'LastUpdatedAt');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Users] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
    ALTER TABLE [Users] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN

                    UPDATE Users
                    SET LastUpdatedAt = NULL
                    WHERE LastUpdatedAt = CreatedAt OR LastUpdatedAt = '2026-08-03 14:12:23.873';
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'LastUpdatedAt');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [RefreshTokens] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
    ALTER TABLE [RefreshTokens] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN

                    UPDATE RefreshTokens
                    SET LastUpdatedAt = NULL
                    WHERE LastUpdatedAt = CreatedAt
                        OR LastUpdatedAt = '2026-08-25 12:10:54.175'
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'LastUpdatedAt');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Posts] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
    ALTER TABLE [Posts] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN

                    UPDATE Posts
                    SET LastUpdatedAt = NULL
                    WHERE LastUpdatedAt = CreatedAt
                        OR LastUpdatedAt = '2026-08-03 14:12:23.920'
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'LastUpdatedAt');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
    ALTER TABLE [Messages] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN

                    UPDATE Messages
                    SET LastUpdatedAt = NULL
                    WHERE LastUpdatedAt = CreatedAt
                        OR LastUpdatedAt = '2026-08-03 14:12:23.930'
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Logs]') AND [c].[name] = N'LastUpdatedAt');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Logs] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Logs] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
    ALTER TABLE [Logs] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'LastUpdatedAt');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Friendships] ALTER COLUMN [LastUpdatedAt] datetime2 NULL;
    ALTER TABLE [Friendships] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN

                    UPDATE Friendships
                    SET LastUpdatedAt = NULL
                    WHERE LastUpdatedAt = CreatedAt
                        OR LastUpdatedAt = '2026-08-03 14:12:23.9366667'
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'LastUpdatedAt');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Comments] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
    ALTER TABLE [Comments] ADD DEFAULT (GETUTCDATE()) FOR [LastUpdatedAt];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN

                    UPDATE Comments
                    SET LastUpdatedAt = NULL
                    WHERE LastUpdatedAt = CreatedAt
                        OR LastUpdatedAt = '2026-08-03 14:12:23.943'
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260827195919_MakeUpdatedAtNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260827195919_MakeUpdatedAtNullable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828154620_SeenAtMigration'
)
BEGIN
    EXEC sp_rename N'[Messages].[IsRead]', N'Seen', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828154620_SeenAtMigration'
)
BEGIN
    ALTER TABLE [Messages] ADD [SeenAt] datetime2(3) NULL DEFAULT (null);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828154620_SeenAtMigration'
)
BEGIN
    UPDATE Messages SET SeenAt = GETUTCDATE() WHERE Seen = 1 AND SeenAt is NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828154620_SeenAtMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828154620_SeenAtMigration', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828204120_UsernameLastChangedAtColumn'
)
BEGIN
    ALTER TABLE [Users] ADD [UsernameLastChangedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260828204120_UsernameLastChangedAtColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260828204120_UsernameLastChangedAtColumn', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'LastUpdatedAt');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var7 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'LastUpdatedAt');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT ' + @var8 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'LastUpdatedAt');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT ' + @var9 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'LastUpdatedAt');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var10 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Logs]') AND [c].[name] = N'LastUpdatedAt');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Logs] DROP CONSTRAINT ' + @var11 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'LastUpdatedAt');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var12 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'LastUpdatedAt');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT ' + @var13 + ';');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829102953_RemoveUpdatedAtDefaultValue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829102953_RemoveUpdatedAtDefaultValue', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829140634_AddLastActiveAtColumn'
)
BEGIN
    ALTER TABLE [Users] ADD [LastOnlineAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829140634_AddLastActiveAtColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829140634_AddLastActiveAtColumn', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830211841_AddPasswordResetToken'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetTokenExpiresAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830211841_AddPasswordResetToken'
)
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetTokenHash] nvarchar(44) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830211841_AddPasswordResetToken'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830211841_AddPasswordResetToken', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831151851_AddRolesTable'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [LastUpdatedAt] datetime2(3) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831151851_AddRolesTable'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831151851_AddRolesTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'LastUpdatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([Id], [CreatedAt], [LastUpdatedAt], [Name])
    VALUES (1, ''2026-08-31T15:04:00.000Z'', NULL, N''User''),
    (2, ''2026-08-31T15:00:00.000Z'', NULL, N''Administrator'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'LastUpdatedAt', N'Name') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831151851_AddRolesTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId') AND [object_id] = OBJECT_ID(N'[UserRoles]'))
        SET IDENTITY_INSERT [UserRoles] ON;
    EXEC(N'INSERT INTO [UserRoles] ([RoleId], [UserId])
    VALUES (1, 3114),
    (2, 3114)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'UserId') AND [object_id] = OBJECT_ID(N'[UserRoles]'))
        SET IDENTITY_INSERT [UserRoles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831151851_AddRolesTable'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831151851_AddRolesTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831151851_AddRolesTable', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831175332_AddDeactivatedBoolean'
)
BEGIN
    ALTER TABLE [Users] ADD [IsAccountDeactivated] bit NOT NULL DEFAULT (1);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831175332_AddDeactivatedBoolean'
)
BEGIN
    EXEC sp_rename N'[Users].[LastActiveAt]', N'LastOnlineAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831175332_AddDeactivatedBoolean'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831175332_AddDeactivatedBoolean', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831182046_DeactivationNewVersion'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'IsAccountDeactivated');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [Users] DROP COLUMN [IsAccountDeactivated];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831182046_DeactivationNewVersion'
)
BEGIN
    ALTER TABLE [Users] ADD [AccountDeactivatedAt] datetime2 NULL DEFAULT (NULL);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831182046_DeactivationNewVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831182046_DeactivationNewVersion', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831184221_DefineQueryFilter'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831184221_DefineQueryFilter', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831205234_GeneralizeResetTokenMigration'
)
BEGIN
    EXEC sp_rename N'[Users].[PasswordResetTokenHash]', N'ResetTokenHash', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831205234_GeneralizeResetTokenMigration'
)
BEGIN
    EXEC sp_rename N'[Users].[PasswordResetTokenExpiresAt]', N'ResetTokenExpiresAt', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831205234_GeneralizeResetTokenMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831205234_GeneralizeResetTokenMigration', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Comments] DROP CONSTRAINT [FK_Comments_Users_CommenterUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] DROP CONSTRAINT [FK_Friendships_Users_AddresseeUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] DROP CONSTRAINT [FK_Friendships_Users_RequesterUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Messages] DROP CONSTRAINT [FK_Messages_Users_ReceiverUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Messages] DROP CONSTRAINT [FK_Messages_Users_SenderUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Posts] DROP CONSTRAINT [FK_Posts_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] DROP CONSTRAINT [PK_Friendships];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Users] ADD [AccountDeletedAt] datetime2 NULL DEFAULT (NULL);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'UserId');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [Posts] ALTER COLUMN [UserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'SenderUserId');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [SenderUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'ReceiverUserId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [ReceiverUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'LastUpdatedAt');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [Friendships] ALTER COLUMN [LastUpdatedAt] datetime2(3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'CreatedAt');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [Friendships] ALTER COLUMN [CreatedAt] datetime2(3) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'AddresseeUserId');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [Friendships] ALTER COLUMN [AddresseeUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'RequesterUserId');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [Friendships] ALTER COLUMN [RequesterUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] ADD [Id] int NOT NULL IDENTITY;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Comments]') AND [c].[name] = N'CommenterUserId');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Comments] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [Comments] ALTER COLUMN [CommenterUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] ADD CONSTRAINT [PK_Friendships] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Friendships_RequesterUserId_AddresseeUserId] ON [Friendships] ([RequesterUserId], [AddresseeUserId]) WHERE [RequesterUserId] IS NOT NULL AND [AddresseeUserId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Comments] ADD CONSTRAINT [FK_Comments_Users_CommenterUserId] FOREIGN KEY ([CommenterUserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] ADD CONSTRAINT [FK_Friendships_Users_AddresseeUserId] FOREIGN KEY ([AddresseeUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Friendships] ADD CONSTRAINT [FK_Friendships_Users_RequesterUserId] FOREIGN KEY ([RequesterUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Users_ReceiverUserId] FOREIGN KEY ([ReceiverUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Users_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    ALTER TABLE [Posts] ADD CONSTRAINT [FK_Posts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902004141_NullableUserIdAndIntPkFriendship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902004141_NullableUserIdAndIntPkFriendship', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    ALTER TABLE [Posts] DROP CONSTRAINT [FK_Posts_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    DROP INDEX [IX_Friendships_RequesterUserId_AddresseeUserId] ON [Friendships];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    DROP INDEX [IX_Posts_UserId] ON [Posts];
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Posts]') AND [c].[name] = N'UserId');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Posts] DROP CONSTRAINT ' + @var23 + ';');
    EXEC(N'UPDATE [Posts] SET [UserId] = 0 WHERE [UserId] IS NULL');
    ALTER TABLE [Posts] ALTER COLUMN [UserId] int NOT NULL;
    ALTER TABLE [Posts] ADD DEFAULT 0 FOR [UserId];
    CREATE INDEX [IX_Posts_UserId] ON [Posts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    DROP INDEX [IX_Messages_SenderUserId_ReceiverUserId_CreatedAt] ON [Messages];
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'SenderUserId');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var24 + ';');
    EXEC(N'UPDATE [Messages] SET [SenderUserId] = 0 WHERE [SenderUserId] IS NULL');
    ALTER TABLE [Messages] ALTER COLUMN [SenderUserId] int NOT NULL;
    ALTER TABLE [Messages] ADD DEFAULT 0 FOR [SenderUserId];
    CREATE INDEX [IX_Messages_SenderUserId_ReceiverUserId_CreatedAt] ON [Messages] ([SenderUserId], [ReceiverUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    DROP INDEX [IX_Messages_SenderUserId_ReceiverUserId_CreatedAt] ON [Messages];
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'ReceiverUserId');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var25 + ';');
    EXEC(N'UPDATE [Messages] SET [ReceiverUserId] = 0 WHERE [ReceiverUserId] IS NULL');
    ALTER TABLE [Messages] ALTER COLUMN [ReceiverUserId] int NOT NULL;
    ALTER TABLE [Messages] ADD DEFAULT 0 FOR [ReceiverUserId];
    CREATE INDEX [IX_Messages_SenderUserId_ReceiverUserId_CreatedAt] ON [Messages] ([SenderUserId], [ReceiverUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'RequesterUserId');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var26 + ';');
    EXEC(N'UPDATE [Friendships] SET [RequesterUserId] = 0 WHERE [RequesterUserId] IS NULL');
    ALTER TABLE [Friendships] ALTER COLUMN [RequesterUserId] int NOT NULL;
    ALTER TABLE [Friendships] ADD DEFAULT 0 FOR [RequesterUserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    DROP INDEX [IX_Friendships_AddresseeUserId] ON [Friendships];
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Friendships]') AND [c].[name] = N'AddresseeUserId');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Friendships] DROP CONSTRAINT ' + @var27 + ';');
    EXEC(N'UPDATE [Friendships] SET [AddresseeUserId] = 0 WHERE [AddresseeUserId] IS NULL');
    ALTER TABLE [Friendships] ALTER COLUMN [AddresseeUserId] int NOT NULL;
    ALTER TABLE [Friendships] ADD DEFAULT 0 FOR [AddresseeUserId];
    CREATE INDEX [IX_Friendships_AddresseeUserId] ON [Friendships] ([AddresseeUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Friendships_RequesterUserId_AddresseeUserId] ON [Friendships] ([RequesterUserId], [AddresseeUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    ALTER TABLE [Posts] ADD CONSTRAINT [FK_Posts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902020901_RevertPkChanges'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902020901_RevertPkChanges', N'10.0.10');
END;

COMMIT;
GO

