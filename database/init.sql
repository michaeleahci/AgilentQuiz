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
GO

CREATE TABLE [InstrumentTypes] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_InstrumentTypes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Name] nvarchar(50) NULL,
    [BanExpiryTime] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Instruments] (
    [Id] uniqueidentifier NOT NULL,
    [InstrumentTypeId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Status] int NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Instruments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Instruments_InstrumentTypes_InstrumentTypeId] FOREIGN KEY ([InstrumentTypeId]) REFERENCES [InstrumentTypes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Reservations] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [InstrumentTypeId] uniqueidentifier NOT NULL,
    [StartTime] datetime2 NOT NULL,
    [EndTime] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [Remark] nvarchar(500) NULL,
    [IdempotencyKey] nvarchar(100) NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Reservations_InstrumentTypes_InstrumentTypeId] FOREIGN KEY ([InstrumentTypeId]) REFERENCES [InstrumentTypes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reservations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Notifications] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ReservationId] uniqueidentifier NULL,
    [Type] int NOT NULL,
    [Content] nvarchar(1000) NOT NULL,
    [Status] int NOT NULL,
    [SentAt] datetime2 NULL,
    [RetryCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ReservationItems] (
    [Id] uniqueidentifier NOT NULL,
    [ReservationId] uniqueidentifier NOT NULL,
    [InstrumentId] uniqueidentifier NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UpdatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_ReservationItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReservationItems_Instruments_InstrumentId] FOREIGN KEY ([InstrumentId]) REFERENCES [Instruments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ReservationItems_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_Instruments_Code] ON [Instruments] ([Code]);
GO

CREATE INDEX [IX_Instruments_TypeStatus] ON [Instruments] ([InstrumentTypeId], [Status]);
GO

CREATE UNIQUE INDEX [IX_InstrumentTypes_Code] ON [InstrumentTypes] ([Code]);
GO

CREATE INDEX [IX_Notifications_ReservationId] ON [Notifications] ([ReservationId]);
GO

CREATE INDEX [IX_Notifications_StatusRetry] ON [Notifications] ([Status], [RetryCount]);
GO

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO

CREATE INDEX [IX_ReservationItems_InstrumentStatus] ON [ReservationItems] ([InstrumentId], [Status]);
GO

CREATE INDEX [IX_ReservationItems_ReservationId] ON [ReservationItems] ([ReservationId]);
GO

CREATE UNIQUE INDEX [IX_Reservations_IdempotencyKey] ON [Reservations] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL;
GO

CREATE INDEX [IX_Reservations_PhoneStatus] ON [Reservations] ([Phone], [Status]);
GO

CREATE INDEX [IX_Reservations_TypeStatusTime] ON [Reservations] ([InstrumentTypeId], [Status], [StartTime], [EndTime]);
GO

CREATE INDEX [IX_Reservations_UserId] ON [Reservations] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_Users_Phone] ON [Users] ([Phone]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260920150531_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

