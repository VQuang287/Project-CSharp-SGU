-- ============================================================================
-- TourMap Database Setup Script
-- For SQL Server (LocalDB or full instance)
-- Run this script to initialize the database with sample data
-- ============================================================================

-- Create database (if not exists)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TourMapAdmin')
BEGIN
    CREATE DATABASE TourMapAdmin;
END
GO

USE TourMapAdmin;
GO

-- ============================================================================
-- DROP EXISTING TABLES (for clean setup)
-- Uncomment if you want to reset everything
-- ============================================================================
/*
DROP TABLE IF EXISTS PlaybackHistories;
DROP TABLE IF EXISTS UserLocationLogs;
DROP TABLE IF EXISTS TourPoiMappings;
DROP TABLE IF EXISTS Tours;
DROP TABLE IF EXISTS QrCodeEntries;
DROP TABLE IF EXISTS DeviceConnections;
DROP TABLE IF EXISTS MobileUsers;
DROP TABLE IF EXISTS AdminUsers;
DROP TABLE IF EXISTS Pois;
GO
*/

-- ============================================================================
-- CREATE TABLES
-- ============================================================================

-- POIs (Points of Interest)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Pois')
BEGIN
    CREATE TABLE Pois (
        Id NVARCHAR(450) PRIMARY KEY,
        Title NVARCHAR(450) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        RadiusMeters INT NOT NULL DEFAULT 50,
        Priority INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        ImageUrl NVARCHAR(MAX) NULL,
        AudioUrl NVARCHAR(MAX) NULL,
        MapLink NVARCHAR(MAX) NULL,
        AudioLocalPath NVARCHAR(MAX) NULL,
        DescriptionEn NVARCHAR(MAX) NULL,
        AudioUrlEn NVARCHAR(MAX) NULL,
        DescriptionZh NVARCHAR(MAX) NULL,
        AudioUrlZh NVARCHAR(MAX) NULL,
        DescriptionKo NVARCHAR(MAX) NULL,
        AudioUrlKo NVARCHAR(MAX) NULL,
        DescriptionJa NVARCHAR(MAX) NULL,
        AudioUrlJa NVARCHAR(MAX) NULL,
        DescriptionFr NVARCHAR(MAX) NULL,
        AudioUrlFr NVARCHAR(MAX) NULL,
        TtsScriptVi NVARCHAR(MAX) NULL,
        TtsScriptEn NVARCHAR(MAX) NULL,
        TtsScriptZh NVARCHAR(MAX) NULL,
        TtsScriptKo NVARCHAR(MAX) NULL,
        TtsScriptJa NVARCHAR(MAX) NULL,
        TtsScriptFr NVARCHAR(MAX) NULL,
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Admin Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdminUsers')
BEGIN
    CREATE TABLE AdminUsers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        [Role] NVARCHAR(50) NOT NULL DEFAULT 'Administrator',
        IsActive BIT NOT NULL DEFAULT 1,
        FailedLoginCount INT NOT NULL DEFAULT 0,
        LockedUntilUtc DATETIME2 NULL,
        LastLoginUtc DATETIME2 NULL
    );
END
GO

-- Mobile Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MobileUsers')
BEGIN
    CREATE TABLE MobileUsers (
        Id NVARCHAR(450) PRIMARY KEY,
        DeviceId NVARCHAR(450) NOT NULL,
        Email NVARCHAR(256) NULL,
        PasswordHash NVARCHAR(MAX) NULL,
        DisplayName NVARCHAR(100) NOT NULL DEFAULT N'Khách',
        AvatarUrl NVARCHAR(500) NULL,
        [Role] NVARCHAR(20) NOT NULL DEFAULT 'Guest',
        AuthProvider NVARCHAR(20) NOT NULL DEFAULT 'local',
        IsEmailVerified BIT NOT NULL DEFAULT 0,
        RefreshToken NVARCHAR(MAX) NULL,
        RefreshTokenExpiresAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastLoginAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Device Connections (for real-time tracking)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DeviceConnections')
BEGIN
    CREATE TABLE DeviceConnections (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DeviceId NVARCHAR(450) NOT NULL,
        UserId NVARCHAR(450) NULL,
        UserName NVARCHAR(MAX) NULL,
        DeviceType NVARCHAR(MAX) NOT NULL DEFAULT 'Unknown',
        AppVersion NVARCHAR(MAX) NULL,
        CurrentPoiId NVARCHAR(450) NULL,
        CurrentPoiName NVARCHAR(MAX) NULL,
        LastLatitude FLOAT NULL,
        LastLongitude FLOAT NULL,
        [State] INT NOT NULL DEFAULT 0,
        ConnectedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastHeartbeatAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        SignalRConnectionId NVARCHAR(MAX) NULL
    );
END
GO

-- QR Code Entries
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QrCodeEntries')
BEGIN
    CREATE TABLE QrCodeEntries (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PoiId NVARCHAR(450) NOT NULL,
        DeepLink NVARCHAR(MAX) NOT NULL,
        QrImageUrl NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Tours
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tours')
BEGIN
    CREATE TABLE Tours (
        Id NVARCHAR(450) PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        ThumbnailUrl NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- Tour-POI Mappings
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TourPoiMappings')
BEGIN
    CREATE TABLE TourPoiMappings (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TourId NVARCHAR(450) NOT NULL,
        PoiId NVARCHAR(450) NOT NULL,
        OrderIndex INT NOT NULL DEFAULT 0
    );
END
GO

-- Playback Histories
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlaybackHistories')
BEGIN
    CREATE TABLE PlaybackHistories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PoiId NVARCHAR(450) NOT NULL,
        DeviceId NVARCHAR(450) NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        TriggerType NVARCHAR(32) NOT NULL DEFAULT 'GPS',
        DurationSeconds INT NOT NULL DEFAULT 0
    );
END
GO

-- User Location Logs
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserLocationLogs')
BEGIN
    CREATE TABLE UserLocationLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserAnonId NVARCHAR(450) NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        RecordedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================================
-- CREATE INDEXES
-- ============================================================================

-- POI indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Pois_IsActive' AND object_id = OBJECT_ID('Pois'))
    CREATE INDEX IX_Pois_IsActive ON Pois(IsActive);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Pois_Priority' AND object_id = OBJECT_ID('Pois'))
    CREATE INDEX IX_Pois_Priority ON Pois(Priority);
GO

-- Tour indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tours_IsActive' AND object_id = OBJECT_ID('Tours'))
    CREATE INDEX IX_Tours_IsActive ON Tours(IsActive);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TourPoiMappings_TourId' AND object_id = OBJECT_ID('TourPoiMappings'))
    CREATE INDEX IX_TourPoiMappings_TourId ON TourPoiMappings(TourId);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TourPoiMappings_PoiId' AND object_id = OBJECT_ID('TourPoiMappings'))
    CREATE INDEX IX_TourPoiMappings_PoiId ON TourPoiMappings(PoiId);
GO

-- ============================================================================
-- SEED DATA: Admin User
-- Default: admin / Admin@123 (change after first login!)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM AdminUsers WHERE Username = 'admin')
BEGIN
    INSERT INTO AdminUsers (Username, PasswordHash, [Role], IsActive, FailedLoginCount)
    VALUES ('admin', 'AQAAAAEAACcQAAAAELHKJH7QYs6Kj7Y6G5s/h+NZf3fP9vqLQ2m/5tG7kXJ5P8u4N3Q2R7T9Y1U2I5O7P3==', 'Administrator', 1, 0);
    -- Note: This is a placeholder hash. Use ASP.NET Identity to generate real password hash.
    -- Or run: dotnet run --project TourMap.AdminWeb, then create user via web interface.
END
GO

-- ============================================================================
-- SEED DATA: POIs (Food Tour - Phố Ẩm Thực Vĩnh Khánh)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM Pois WHERE Title = N'Ốc Oanh')
BEGIN
    INSERT INTO Pois (Id, Title, Description, Latitude, Longitude, Priority, RadiusMeters, MapLink, IsActive, UpdatedAt)
    VALUES 
    -- ===== Các quán ốc nổi tiếng =====
    (NEWID(), N'Ốc Oanh', N'Quán ốc nổi tiếng phố Vĩnh Khánh, đa dạng các món ốc xào, nướng', 10.7608247, 106.7034143, 10, 40, 'https://maps.app.goo.gl/ocOanhVK', 1, GETUTCDATE()),
    (NEWID(), N'Ốc Đào', N'Quán ốc lâu đời, nổi tiếng với ốc hương, ốc mỡ, ốc len xào dừa', 10.7609123, 106.7039856, 10, 35, 'https://maps.app.goo.gl/ocDaoVK', 1, GETUTCDATE()),
    (NEWID(), N'Ốc Như', N'Quán ốc bình dân, giá rẻ, đông khách', 10.7606543, 106.7045234, 8, 30, NULL, 1, GETUTCDATE()),

    -- ===== Lẩu và nướng =====
    (NEWID(), N'Lẩu Dê Nhất Ly', N'Lẩu dê nổi tiếng với nước dùng đậm đà, thịt dê tươi', 10.7611784, 106.705375, 9, 40, 'https://maps.app.goo.gl/laudeVK', 1, GETUTCDATE()),
    (NEWID(), N'Lẩu Bò Tí Chuối', N'Lẩu bò lá giang, lẩu bò nhúng dấm đặc trưng', 10.7612345, 106.7058234, 8, 35, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Sườn Nướng Mật Ong', N'Sườn nướng thơm lừng, mật ong caramelized', 10.7608234, 106.7041234, 7, 30, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Bò Nướng Lá Lốt', N'Bò cuốn lá lốt nướng than hoa, chấm mắm nêm', 10.7609234, 106.7046234, 7, 30, NULL, 1, GETUTCDATE()),

    -- ===== Bánh và mì =====
    (NEWID(), N'Bánh Canh Cua', N'Bánh canh cua sợi to, nước dùng sánh đặc, thịt cua tươi', 10.7610234, 106.7052345, 8, 30, 'https://maps.app.goo.gl/banhcanhVK', 1, GETUTCDATE()),
    (NEWID(), N'Bánh Mì Phú Lộc', N'Bánh mì nướng muối ớt, topping đa dạng', 10.7607234, 106.7038234, 7, 25, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Bánh Xèo Tôm Nhảy', N'Bánh xèo giòn rụm, tôm tươi, nước mắm chua ngọt', 10.7611234, 106.7054234, 7, 30, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Bánh Bèo Chén', N'Bánh bèo chén nóng hổi, tôm chấy, mỡ hành', 10.7608234, 106.7043234, 6, 25, NULL, 1, GETUTCDATE()),

    -- ===== Chè và giải khát =====
    (NEWID(), N'Chè Vĩnh Khánh', N'Chè thái, chè khúc bạch, chè trái cây giải nhiệt', 10.7609234, 106.7047234, 6, 25, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Sinh Tố Bơ Đậu Phộng', N'Sinh tố bơ béo ngậy, đậu phộng rang giòn', 10.7610234, 106.7051234, 5, 20, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Nước Mía Sầu Riêng', N'Nước mía ép tươi, thêm sầu riêng độc đáo', 10.7608234, 106.7039234, 5, 20, NULL, 1, GETUTCDATE()),

    -- ===== Các món đặc sản khác =====
    (NEWID(), N'Gỏi Cuốn Tôm Thịt', N'Gỏi cuốn tươi ngon, nước chấm đậm đà', 10.7607234, 106.7045234, 6, 25, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Bún Thịt Nướng', N'Bún thịt nướng chả giò, nước mắm pha chua ngọt', 10.7612234, 106.7056234, 6, 25, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Cơm Tấm Sườn Bì', N'Cơm tấm sườn nướng, bì, chả trứng', 10.7609234, 106.7042234, 7, 30, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Phá Lấu Bò', N'Phá lấu bò nước dừa, ăn kèm bánh mì', 10.7613234, 106.7059234, 6, 25, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Xiên Nướng Mix', N'Các loại xiên nướng: bò, gà, tôm, mực', 10.7608234, 106.7048234, 7, 30, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Mực Nướng Sa Tế', N'Mực nướng sa tế cay cay, thơm nức', 10.7609234, 106.7053234, 8, 30, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Tôm Nướng Muối Ớt', N'Tôm nướng muối ớt Tây Ninh, vỏ giòn thịt ngọt', 10.7610234, 106.7044234, 8, 30, NULL, 1, GETUTCDATE()),
    (NEWID(), N'Cháo Hàu', N'Cháo hàu nóng hổi, hàu tươi ngon bổ dưỡng', 10.7606234, 106.7042234, 7, 30, NULL, 1, GETUTCDATE());
END
GO

-- ============================================================================
-- SEED DATA: Sample Tour (Food Tour Vĩnh Khánh)
-- ============================================================================

DECLARE @TourId NVARCHAR(450) = 'food-tour-vinh-khanh-001';

IF NOT EXISTS (SELECT * FROM Tours WHERE Id = @TourId)
BEGIN
    -- Create the tour
    INSERT INTO Tours (Id, [Name], Description, IsActive, ThumbnailUrl, CreatedAt, UpdatedAt)
    VALUES (@TourId, N'Tour Ẩm Thực Phố Vĩnh Khánh', 
            N'Khám phá các món ngon nổi tiếng trên con phố ẩm thực Vĩnh Khánh - từ ốc xào, lẩu dê đến các món nướng đặc sản.', 
            1, NULL, GETUTCDATE(), GETUTCDATE());

    -- Add POIs to tour (in order)
    INSERT INTO TourPoiMappings (TourId, PoiId, OrderIndex)
    SELECT @TourId, Id, ROW_NUMBER() OVER (ORDER BY Priority DESC, Title) as OrderIndex
    FROM Pois 
    WHERE Title IN (N'Ốc Oanh', N'Ốc Đào', N'Lẩu Dê Nhất Ly', N'Bánh Canh Cua', 
                    N'Mực Nướng Sa Tế', N'Tôm Nướng Muối Ớt', N'Cơm Tấm Sườn Bì',
                    N'Xiên Nướng Mix', N'Cháo Hàu', N'Chè Vĩnh Khánh');
END
GO

-- ============================================================================
-- VERIFICATION QUERIES (uncomment to check data after setup)
-- ============================================================================
/*
-- Count POIs
SELECT 'Total POIs' as Metric, COUNT(*) as Count FROM Pois
UNION ALL
SELECT 'Active Tours', COUNT(*) FROM Tours WHERE IsActive = 1
UNION ALL
SELECT 'Admin Users', COUNT(*) FROM AdminUsers;

-- List all POIs
SELECT Title, Latitude, Longitude, Priority FROM Pois ORDER BY Priority DESC;

-- List tour with POIs
SELECT t.Name as TourName, p.Title as PoiTitle, tm.OrderIndex
FROM Tours t
JOIN TourPoiMappings tm ON t.Id = tm.TourId
JOIN Pois p ON tm.PoiId = p.Id
ORDER BY tm.OrderIndex;
*/

PRINT 'Database setup completed successfully!';
PRINT 'Default admin: username=admin (create password via web interface)';
PRINT 'Sample tour: Tour Ẩm Thực Phố Vĩnh Khánh with 10 POIs';
GO
