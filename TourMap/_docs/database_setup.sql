-- ============================================================================
-- TourMap Database Setup Script
-- For SQL Server (LocalDB or full instance)
-- Run this script to initialize the database with sample data
-- ============================================================================

-- Create database (if not exists)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'TourMap')
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
-- SEED DATA: POIs (Phố Ẩm Thực Vĩnh Khánh - 10 địa điểm chính xác)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM Pois WHERE Title = N'Ốc Oánh')
BEGIN
    INSERT INTO Pois (Id, Title, Description, Latitude, Longitude, Priority, RadiusMeters, ImageUrl, MapLink, IsActive, UpdatedAt)
    VALUES 
    (NEWID(), N'Ốc Oánh', N'Quán ốc nổi tiếng tại Phố ẩm thực Vĩnh Khánh với các món ốc tươi ngon, giá bình dân. Địa chỉ: 534 Vĩnh Khánh, Phường 8, Quận 4, TP. HCM.', 10.7607194, 106.7032972, 1, 50, 'https://images.unsplash.com/photo-1534080564583-6be75777b70a?w=800&q=80', 'https://maps.google.com/?q=10.7607194,106.7032972', 1, GETUTCDATE()),
    (NEWID(), N'Quán Ốc Thảo', N'Quán ốc Thảo chuyên các món hải sản tươi sống, chế biến đậm đà hương vị miền Nam. Địa chỉ: 383 Vĩnh Khánh, Phường 8, Quận 4, TP. HCM.', 10.7616799, 106.7023636, 2, 50, 'https://images.unsplash.com/photo-1559339352-11d035aa65de?w=800&q=80', 'https://maps.google.com/?q=10.7616799,106.7023636', 1, GETUTCDATE()),
    (NEWID(), N'Lãng Quán', N'Lãng Quán - Điểm đến lý tưởng cho những buổi tụ tập bạn bè với không gian thoáng đãng và món ăn đa dạng. Địa chỉ: 531 Vĩnh Khánh, Phường 10, Quận 4, TP. HCM.', 10.7611131, 106.7054162, 3, 50, 'https://images.unsplash.com/photo-1553621042-f6e147245754?w=800&q=80', 'https://maps.google.com/?q=10.7611131,106.7054162', 1, GETUTCDATE()),
    (NEWID(), N'Ớt Xiêm Quán', N'Ớt Xiêm Quán nổi tiếng với các món cay đặc trưng, phù hợp cho những thực khách thích hương vị mạnh. Địa chỉ: 568 Vĩnh Khánh, Phường 10, Quận 4, TP. HCM.', 10.7611663, 106.7057009, 4, 50, 'https://images.unsplash.com/photo-1594007654729-407eedc4be65?w=800&q=80', 'https://maps.google.com/?q=10.7611663,106.7057009', 1, GETUTCDATE()),
    (NEWID(), N'Chilli Lẩu Nướng Quán', N'Chilli Lẩu Nướng Quán - Không gian hiện đại với các món lẩu và nướng đa dạng, phù hợp cho cả gia đình. Địa chỉ: 232 Vĩnh Khánh, Phường 10, Quận 4, TP. HCM.', 10.760693, 106.7036324, 5, 50, 'https://images.unsplash.com/photo-1504544750208-dc0358e63f7f?w=800&q=80', 'https://maps.google.com/?q=10.760693,106.7036324', 1, GETUTCDATE()),
    (NEWID(), N'Quán ốc Sáu Nở', N'Quán ốc Sáu Nở - Địa điểm quen thuộc của dân sành ăn ốc với thực đơn phong phú và giá cả hợp lý. Địa chỉ: 128 Vĩnh Khánh, Phường 10, Quận 4, TP. HCM.', 10.7609643, 106.702942, 6, 50, 'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?w=800&q=80', 'https://maps.google.com/?q=10.7609643,106.702942', 1, GETUTCDATE()),
    (NEWID(), N'Quán Ốc Vũ', N'Quán Ốc Vũ - Chuyên các món ốc và hải sản tươi ngon, phục vụ nhanh chóng và chuyên nghiệp. Địa chỉ: 37 Vĩnh Khánh, Phường 8, Quận 4, TP. HCM.', 10.7614025, 106.7027047, 7, 50, 'https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=800&q=80', 'https://maps.google.com/?q=10.7614025,106.7027047', 1, GETUTCDATE()),
    (NEWID(), N'Ốc Cúc Vĩnh Khánh', N'Ốc Cúc Vĩnh Khánh - Quán ốc lâu năm với công thức chế biến độc đáo, giữ chân thực khách bằng chất lượng và hương vị. Địa chỉ: 129 Vĩnh Khánh, Phường 8, Quận 4, TP. HCM.', 10.761224, 106.7026292, 8, 50, 'https://images.unsplash.com/photo-1512058564366-18510be2db19?w=800&q=80', 'https://maps.google.com/?q=10.761224,106.7026292', 1, GETUTCDATE()),
    (NEWID(), N'Sushi Ko', N'Sushi Ko - Nhà hàng Nhật Bản với sushi và sashimi tươi ngon, không gian sang trọng và ấm cúng. Địa chỉ: 122 Vĩnh Khánh, Phường 10, Quận 4, TP. HCM.', 10.7607387, 106.7046509, 9, 50, 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=800&q=80', 'https://maps.google.com/?q=10.7607387,106.7046509', 1, GETUTCDATE()),
    (NEWID(), N'An An Quán', N'An An Quán - Quán ăn gia đình với các món ăn Việt truyền thống, không gian ấm cúng và giá cả phải chăng. Địa chỉ: 122 Vĩnh Khánh, Phường 10, Quận 4, TP. HCM.', 10.7606347, 106.7044488, 10, 50, 'https://images.unsplash.com/photo-1555126634-323283e090fa?w=800&q=80', 'https://maps.google.com/?q=10.7606347,106.7044488', 1, GETUTCDATE());
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
    SELECT @TourId, Id, ROW_NUMBER() OVER (ORDER BY Priority ASC, Title) as OrderIndex
    FROM Pois 
    WHERE Title IN (N'Ốc Oánh', N'Quán Ốc Thảo', N'Lãng Quán', N'Ớt Xiêm Quán', 
                    N'Chilli Lẩu Nướng Quán', N'Quán ốc Sáu Nở', N'Quán Ốc Vũ',
                    N'Ốc Cúc Vĩnh Khánh', N'Sushi Ko', N'An An Quán');
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

-- ============================================================================
-- UPDATE IMAGE URLs (Run this if POIs already exist without images)
-- ============================================================================

UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1534080564583-6be75777b70a?w=800&q=80' WHERE Title = N'Ốc Oánh';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1559339352-11d035aa65de?w=800&q=80' WHERE Title = N'Quán Ốc Thảo';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1553621042-f6e147245754?w=800&q=80' WHERE Title = N'Lãng Quán';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1594007654729-407eedc4be65?w=800&q=80' WHERE Title = N'Ớt Xiêm Quán';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1504544750208-dc0358e63f7f?w=800&q=80' WHERE Title = N'Chilli Lẩu Nướng Quán';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1626645738196-c2a7c87a8f58?w=800&q=80' WHERE Title = N'Quán ốc Sáu Nở';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1579584425555-c3ce17fd4351?w=800&q=80' WHERE Title = N'Quán Ốc Vũ';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1512058564366-18510be2db19?w=800&q=80' WHERE Title = N'Ốc Cúc Vĩnh Khánh';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=800&q=80' WHERE Title = N'Sushi Ko';
UPDATE Pois SET ImageUrl = 'https://images.unsplash.com/photo-1555126634-323283e090fa?w=800&q=80' WHERE Title = N'An An Quán';
GO

PRINT 'Database setup completed successfully!';
PRINT 'Default admin: username=admin (create password via web interface)';
PRINT 'Sample tour: Tour Ẩm Thực Phố Vĩnh Khánh with 10 accurate POIs + images';
GO
