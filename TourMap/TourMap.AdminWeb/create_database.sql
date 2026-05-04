/*
  TourMap AdminWeb - SQL Server bootstrap script
  Source: EF Core migration 20260420113547_InitialSqlServerDb

  Usage:
  1) Open this file in SSMS/Azure Data Studio.
  2) Change database name below if needed.
  3) Execute the script.
*/

IF DB_ID(N'TourMapAdmin') IS NULL
BEGIN
    CREATE DATABASE [TourMapAdmin];
END
GO

USE [TourMapAdmin];
GO

/* =========================
   AdminUsers
   ========================= */
IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AdminUsers]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](100) NOT NULL,
        [PasswordHash] [nvarchar](max) NOT NULL,
        [Role] [nvarchar](50) NOT NULL,
        [IsActive] [bit] NOT NULL,
        [FailedLoginCount] [int] NOT NULL,
        [LockedUntilUtc] [datetime2](7) NULL,
        [LastLoginUtc] [datetime2](7) NULL,
        CONSTRAINT [PK_AdminUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   DeviceConnections
   ========================= */
IF OBJECT_ID(N'dbo.DeviceConnections', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DeviceConnections]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [DeviceId] [nvarchar](max) NOT NULL,
        [UserId] [nvarchar](max) NULL,
        [UserName] [nvarchar](max) NULL,
        [DeviceType] [nvarchar](max) NOT NULL,
        [AppVersion] [nvarchar](max) NULL,
        [LastLatitude] [float] NULL,
        [LastLongitude] [float] NULL,
        [CurrentPoiId] [nvarchar](max) NULL,
        [CurrentPoiName] [nvarchar](max) NULL,
        [State] [int] NOT NULL,
        [ConnectedAt] [datetime2](7) NOT NULL,
        [LastHeartbeatAt] [datetime2](7) NOT NULL,
        [SignalRConnectionId] [nvarchar](max) NULL,
        CONSTRAINT [PK_DeviceConnections] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   MobileUsers
   ========================= */
IF OBJECT_ID(N'dbo.MobileUsers', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MobileUsers]
    (
        [Id] [nvarchar](450) NOT NULL,
        [DeviceId] [nvarchar](max) NOT NULL,
        [Email] [nvarchar](256) NULL,
        [PasswordHash] [nvarchar](max) NULL,
        [DisplayName] [nvarchar](100) NOT NULL,
        [AvatarUrl] [nvarchar](500) NULL,
        [Role] [nvarchar](20) NOT NULL,
        [AuthProvider] [nvarchar](20) NOT NULL,
        [IsEmailVerified] [bit] NOT NULL,
        [RefreshToken] [nvarchar](max) NULL,
        [RefreshTokenExpiresAt] [datetime2](7) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [LastLoginAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_MobileUsers] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   Pois
   ========================= */
IF OBJECT_ID(N'dbo.Pois', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Pois]
    (
        [Id] [nvarchar](450) NOT NULL,
        [Title] [nvarchar](max) NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        [Latitude] [float] NOT NULL,
        [Longitude] [float] NOT NULL,
        [RadiusMeters] [int] NOT NULL,
        [Priority] [int] NOT NULL,
        [IsActive] [bit] NOT NULL,
        [ImageUrl] [nvarchar](max) NULL,
        [AudioUrl] [nvarchar](max) NULL,
        [MapLink] [nvarchar](max) NULL,
        [AudioLocalPath] [nvarchar](max) NULL,
        [DescriptionEn] [nvarchar](max) NULL,
        [AudioUrlEn] [nvarchar](max) NULL,
        [DescriptionZh] [nvarchar](max) NULL,
        [AudioUrlZh] [nvarchar](max) NULL,
        [DescriptionKo] [nvarchar](max) NULL,
        [AudioUrlKo] [nvarchar](max) NULL,
        [DescriptionJa] [nvarchar](max) NULL,
        [AudioUrlJa] [nvarchar](max) NULL,
        [DescriptionFr] [nvarchar](max) NULL,
        [AudioUrlFr] [nvarchar](max) NULL,
        [TtsScriptVi] [nvarchar](max) NULL,
        [TtsScriptEn] [nvarchar](max) NULL,
        [TtsScriptZh] [nvarchar](max) NULL,
        [TtsScriptKo] [nvarchar](max) NULL,
        [TtsScriptJa] [nvarchar](max) NULL,
        [TtsScriptFr] [nvarchar](max) NULL,
        [UpdatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_Pois] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   QrCodeEntries
   ========================= */
IF OBJECT_ID(N'dbo.QrCodeEntries', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[QrCodeEntries]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [PoiId] [nvarchar](max) NOT NULL,
        [DeepLink] [nvarchar](max) NOT NULL,
        [QrImageUrl] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_QrCodeEntries] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   Tours
   ========================= */
IF OBJECT_ID(N'dbo.Tours', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Tours]
    (
        [Id] [nvarchar](450) NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [Description] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_Tours] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   UserLocationLogs
   ========================= */
IF OBJECT_ID(N'dbo.UserLocationLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserLocationLogs]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserAnonId] [nvarchar](max) NULL,
        [Latitude] [float] NOT NULL,
        [Longitude] [float] NOT NULL,
        [RecordedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_UserLocationLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

/* =========================
   PlaybackHistories
   ========================= */
IF OBJECT_ID(N'dbo.PlaybackHistories', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PlaybackHistories]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [PoiId] [nvarchar](450) NOT NULL,
        [DeviceId] [nvarchar](max) NULL,
        [Timestamp] [datetime2](7) NOT NULL,
        [TriggerType] [nvarchar](32) NOT NULL,
        [DurationSeconds] [int] NOT NULL,
        [IsCompleted] [bit] NOT NULL,
        CONSTRAINT [PK_PlaybackHistories] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_PlaybackHistories_Pois_PoiId]
            FOREIGN KEY ([PoiId]) REFERENCES [dbo].[Pois]([Id])
            ON DELETE CASCADE
    );
END
GO

/* =========================
   TourPoiMappings
   ========================= */
IF OBJECT_ID(N'dbo.TourPoiMappings', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TourPoiMappings]
    (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [TourId] [nvarchar](450) NOT NULL,
        [PoiId] [nvarchar](450) NOT NULL,
        [OrderIndex] [int] NOT NULL,
        CONSTRAINT [PK_TourPoiMappings] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_TourPoiMappings_Pois_PoiId]
            FOREIGN KEY ([PoiId]) REFERENCES [dbo].[Pois]([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_TourPoiMappings_Tours_TourId]
            FOREIGN KEY ([TourId]) REFERENCES [dbo].[Tours]([Id])
            ON DELETE CASCADE
    );
END
GO

/* =========================
   Indexes (from migration)
   ========================= */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PlaybackHistories_PoiId'
      AND object_id = OBJECT_ID(N'dbo.PlaybackHistories')
)
BEGIN
    CREATE INDEX [IX_PlaybackHistories_PoiId]
    ON [dbo].[PlaybackHistories]([PoiId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TourPoiMappings_PoiId'
      AND object_id = OBJECT_ID(N'dbo.TourPoiMappings')
)
BEGIN
    CREATE INDEX [IX_TourPoiMappings_PoiId]
    ON [dbo].[TourPoiMappings]([PoiId]);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TourPoiMappings_TourId'
      AND object_id = OBJECT_ID(N'dbo.TourPoiMappings')
)
BEGIN
    CREATE INDEX [IX_TourPoiMappings_TourId]
    ON [dbo].[TourPoiMappings]([TourId]);
END
GO

PRINT N'TourMapAdmin database schema is ready.';
GO
