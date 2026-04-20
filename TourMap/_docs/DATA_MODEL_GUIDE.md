# TOURMAP - DATA MODEL & RELATIONSHIPS GUIDE

## Database Schema Overview

```sql
┌─────────────────────────────────────────────────────────────┐
│                    TOURMAP DATABASE (SQLite)                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐         ┌───────────────────────┐   │
│  │      POI         │         │ PlaybackHistoryEntry  │   │
│  ├──────────────────┤         ├───────────────────────┤   │
│  │ Id (PK)          │◄────────┤ PoiId (FK)            │   │
│  │ Title            │         │ PoiTitle              │   │
│  │ Description      │         │ TriggerType           │   │
│  │ Latitude         │         │ AudioSource           │   │
│  │ Longitude        │         │ PlayedAtUtc           │   │
│  │ RadiusMeters     │         └───────────────────────┘   │
│  │ Priority         │                                     │
│  │ IsActive         │         ┌───────────────────────┐   │
│  │ ImageUrl         │         │ UserLocationEntry     │   │
│  │ AudioUrl         │◄────────┤ (Future)              │   │
│  │ AudioLocalPath   │         │ Latitude              │   │
│  │ DescriptionXxx   │         │ Longitude             │   │
│  │ TtsScriptXxx     │         │ TimestampUtc          │   │
│  │ UpdatedAt        │         └───────────────────────┘   │
│  └──────────────────┘                                     │
│                                                             │
│  Indexes:                                                  │
│  - PRIMARY KEY(Id)                                        │
│  - UNIQUE(Latitude, Longitude) -- Future optimization    │
└─────────────────────────────────────────────────────────────┘
```

---

## POI Entity

### Complete Field Listing

#### Geographic Fields (Language-Independent)
```csharp
public string Id { get; set; }              // GUID, Primary Key
public double Latitude { get; set; }        // -90 to 90
public double Longitude { get; set; }       // -180 to 180
public int RadiusMeters { get; set; }       // Geofence trigger radius (30-100m typical)
public int Priority { get; set; }           // 1-10 (higher = interrupt lower)
public bool IsActive { get; set; }          // Soft delete
```

#### Base Language Content (Vietnamese)
```csharp
public string Title { get; set; }           // POI name
public string Description { get; set; }     // Long description
public string? ImageUrl { get; set; }       // Image URL or local path
public string? AudioUrl { get; set; }       // MP3 file URL
public string? AudioLocalPath { get; set; } // Cached MP3 path
public string? MapLink { get; set; }        // Deep link to maps app
```

#### Multilingual Descriptions (Fallback for UI)
```csharp
public string? DescriptionEn { get; set; }  // English description
public string? DescriptionZh { get; set; }  // Chinese (Simplified) description
public string? DescriptionKo { get; set; }  // Korean description
public string? DescriptionJa { get; set; }  // Japanese description
public string? DescriptionFr { get; set; }  // French description
```

#### Multilingual Audio URLs
```csharp
public string? AudioUrlEn { get; set; }     // English audio MP3
public string? AudioUrlZh { get; set; }     // Chinese audio MP3
public string? AudioUrlKo { get; set; }     // Korean audio MP3
public string? AudioUrlJa { get; set; }     // Japanese audio MP3
public string? AudioUrlFr { get; set; }     // French audio MP3
```

#### TTS Scripts (Customized Text-to-Speech)
```csharp
public string? TtsScriptVi { get; set; }    // Vietnamese TTS script (optimized for pronunciation)
public string? TtsScriptEn { get; set; }    // English TTS script
public string? TtsScriptZh { get; set; }    // Chinese TTS script
public string? TtsScriptKo { get; set; }    // Korean TTS script
public string? TtsScriptJa { get; set; }    // Japanese TTS script
public string? TtsScriptFr { get; set; }    // French TTS script
```

#### Metadata
```csharp
public DateTime UpdatedAt { get; set; }     // Last modification timestamp (UTC)
```

### Field Relationships

```
Title (vi) 
   ├─ Used directly on MapPage
   └─ Falls back to DescriptionEn if Title empty

Description (vi)
   ├─ Primary content for default language
   └─ Used in POI detail view

TtsScriptVi (highest priority)
   └─ Read by NarrationEngine.OnPOITriggeredAsync()
      - If not empty: Use TtsService.SpeakAsync(TtsScriptVi)
      - If empty: Fall back to DescriptionVi

AudioUrl / AudioLocalPath
   └─ Used if language = Vietnamese
      - If AudioLocalPath exists: Play MP3 file
      - If AudioUrl exists: Download and cache

DescriptionEn/Zh/Ko/Ja/Fr
   └─ Used for non-Vietnamese languages
      - If present: Use TtsService.SpeakAsync(Description)
      - Fallback if TtsScript empty

AudioUrlEn/Zh/Ko/Ja/Fr (Future)
   └─ Not currently used (TTS scripts preferred)
   └─ Could enable pre-recorded audio per language

ImageUrl
   └─ Display on MapPage, PoiDetailPage
   └─ Download and cache on first sync
```

### Sample POI Instance

```csharp
new Poi
{
    Id = Guid.NewGuid().ToString(),
    Title = "Ngã 4 Hoàng Diệu",
    Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh, điểm khởi đầu...",
    Latitude = 10.7618898,
    Longitude = 106.7020039,
    RadiusMeters = 30,
    Priority = 1,
    IsActive = true,
    ImageUrl = "https://server.com/images/poi_001.jpg",
    AudioUrl = "https://server.com/audio/poi_001_vi.mp3",
    AudioLocalPath = "/data/data/com.companyname.tourmap/audio/poi_001_vi.mp3",
    MapLink = "https://maps.google.com/?q=10.7618898,106.7020039",
    
    // Vietnamese (primary)
    TtsScriptVi = "Ngã tư Hoàng Diệu Vĩnh Khánh. Đây là điểm khởi đầu...",
    
    // English
    DescriptionEn = "Hoang Dieu and Vinh Khanh intersection. This is the starting point...",
    TtsScriptEn = "Hoang Dieu and Vinh Khanh intersection...",
    
    // Chinese
    DescriptionZh = "黄迪文与永庆路口。这是第四郡著名美食街的起点...",
    TtsScriptZh = "黄迪文与永庆路口...",
    
    // Korean, Japanese, French (similar)
    ...
    
    UpdatedAt = DateTime.UtcNow
}
```

---

## PlaybackHistoryEntry Entity

### Purpose
Logs every time a user triggers audio playback (either via GPS or manual).

### Fields

```csharp
[PrimaryKey, AutoIncrement]
public int Id { get; set; }                 // Local database ID (auto-increment)

public string PoiId { get; set; }           // Reference to POI (denormalized)
public string PoiTitle { get; set; }        // POI name (snapshot at time of play)

public string TriggerType { get; set; }     // "GPS" (geofence triggered) or "Manual" (user button)

public string AudioSource { get; set; }     // "TTS" (system TTS)
                                            // "AudioFile" (pre-recorded MP3)
                                            // "TTS-DB" (custom TTS script from DB)

public DateTime PlayedAtUtc { get; set; }   // When playback started (UTC)
```

### Example Records

```
Id | PoiId                                | PoiTitle           | TriggerType | AudioSource | PlayedAtUtc
---+--------------------------------------+--------------------+-------------+-------------+-------------------
1  | 3fa85f64-5717-4562-b3fc-2c963f66afa6 | Ngã 4 Hoàng Diệu  | GPS         | TTS-DB      | 2026-04-20 14:32:15
2  | 3fa85f64-5717-4562-b3fc-2c963f66afa6 | Ngã 4 Hoàng Diệu  | Manual      | AudioFile   | 2026-04-20 15:20:30
3  | 5ba90d2f-3ef1-4e2a-9c1b-7d4e8f9a0b1c | Ốc Oanh            | GPS         | TTS         | 2026-04-20 15:45:00
```

### Usage

**Recording Playback:**
```csharp
await _databaseService.AddPlaybackHistoryAsync(new PlaybackHistoryEntry
{
    PoiId = poi.Id,
    PoiTitle = poi.Title,
    TriggerType = "GPS",           // or "Manual"
    AudioSource = "TTS-DB",        // or "AudioFile", "TTS"
    PlayedAtUtc = DateTime.UtcNow
});
```

**Analytics Queries:**
```csharp
// Top POIs by play count
var topPois = await _db.Table<PlaybackHistoryEntry>()
    .GroupBy(h => h.PoiId)
    .OrderByDescending(g => g.Count())
    .Take(10)
    .ToListAsync();

// Language distribution
var langDist = await _db.Table<PlaybackHistoryEntry>()
    .Where(h => h.PlayedAtUtc > DateTime.UtcNow.AddDays(-7))
    .GroupBy(h => h.AudioSource)
    .Select(g => new { Source = g.Key, Count = g.Count() })
    .ToListAsync();
```

---

## Data Model Normalization

### Current State (Denormalized for Mobile Performance)

**Pros:**
- Single table query for POI → Fast
- No joins required
- Minimal database complexity
- Easy to cache and serialize

**Cons:**
- Language duplication (7 language fields)
- Future scale challenges (1000+ POIs = massive table)
- Update complexity (admin must update 7+ fields)

### Recommended Future Normalization (v2.0)

```sql
CREATE TABLE Poi (
    Id TEXT PRIMARY KEY,
    Latitude REAL,
    Longitude REAL,
    RadiusMeters INTEGER,
    Priority INTEGER,
    IsActive BOOLEAN,
    ImageUrl TEXT,
    UpdatedAt DATETIME
);

CREATE TABLE PoiTranslation (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PoiId TEXT NOT NULL,
    LanguageCode TEXT NOT NULL,       -- "vi", "en", "zh", "ko", "ja", "fr"
    Title TEXT,
    Description TEXT,
    TtsScript TEXT,
    AudioUrl TEXT,
    AudioLocalPath TEXT,
    UpdatedAt DATETIME,
    FOREIGN KEY(PoiId) REFERENCES Poi(Id),
    UNIQUE(PoiId, LanguageCode)
);

CREATE TABLE PlaybackHistoryEntry (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PoiId TEXT NOT NULL,
    PoiTitle TEXT,
    LanguageCode TEXT,               -- Track which language was played
    TriggerType TEXT,
    AudioSource TEXT,
    PlayedAtUtc DATETIME,
    FOREIGN KEY(PoiId) REFERENCES Poi(Id)
);
```

**Benefits:**
- Scales to 10,000+ POIs without bloat
- Language management separate from POI
- Enables language-specific analytics
- Admin web simpler (tabbed interface)
- Mobile app can selectively load languages

---

## Data Lifecycle

### POI Data Lifecycle

```
1. CREATION (Admin Web)
   ├─ Admin creates POI with location
   ├─ Admin adds content for each language (6 tabs)
   └─ Saves to AdminTourMap.db

2. EXPORT/SYNC (Admin → Mobile)
   ├─ SyncService calls GET /api/v1/pois/sync/pois
   ├─ Server returns POI JSON with all fields
   ├─ Mobile downloads audio files to {AppData}/audio/
   └─ Upserts POI to local TourMap_v6.db

3. USAGE (Mobile App)
   ├─ GeofenceEngine monitors POI coordinates
   ├─ On trigger: NarrationEngine selects audio source
   ├─ NarrationEngine reads TtsScript or plays AudioLocalPath
   ├─ PlaybackHistoryEntry logged
   └─ POI data unchanged

4. UPDATE (Admin Web)
   ├─ Admin modifies POI content
   ├─ Updates AdminTourMap.db
   ├─ Next sync: Mobile receives updated POI
   └─ Overwrites local copy via Upsert

5. DELETION (Admin Web)
   ├─ Admin sets IsActive = false (soft delete)
   ├─ Next sync: Mobile receives IsActive = false
   ├─ Mobile hides/skips POI in geofence check
   └─ Data preserved in PlaybackHistoryEntry
```

### PlaybackHistoryEntry Data Lifecycle

```
1. RECORDING (Mobile)
   ├─ POI triggered (GPS or Manual)
   ├─ Audio starts playing
   └─ AddPlaybackHistoryAsync() logs event

2. LOCAL STORAGE (Mobile)
   ├─ Stored in TourMap_v6.db
   ├─ Persists across app restarts
   ├─ Enables offline analytics
   └─ Maximum storage: Limited by device

3. TRANSMISSION (Future Enhancement)
   ├─ Mobile syncs history to server
   ├─ Server stores in shared/analytics DB
   ├─ Analytics queries history
   └─ Generate reports/heatmaps

4. ANALYTICS (Admin Web)
   ├─ QueryPlaybackHistory() aggregates data
   ├─ Groups by POI, language, date
   ├─ Generates charts (top POIs, language dist)
   └─ Enables heatmap visualization
```

---

## Data Validation Rules

### POI Validation (Mobile)

```csharp
// In SyncService when processing server response
if (string.IsNullOrWhiteSpace(poi.Id))
    poi.Id = Guid.NewGuid().ToString();  // Generate if missing

if (poi.Latitude < -90 || poi.Latitude > 90)
    return; // Invalid latitude

if (poi.Longitude < -180 || poi.Longitude > 180)
    return; // Invalid longitude

if (poi.RadiusMeters <= 0)
    poi.RadiusMeters = 50; // Default 50m

if (poi.Priority < 1)
    poi.Priority = 1; // Minimum priority
else if (poi.Priority > 10)
    poi.Priority = 10; // Maximum priority

if (string.IsNullOrWhiteSpace(poi.Title))
    poi.Title = "Unnamed POI"; // Fallback title
```

### Language Field Validation

```csharp
// TTS scripts should be reasonable length (10-500 chars)
if (string.IsNullOrWhiteSpace(poi.TtsScriptVi) || poi.TtsScriptVi.Length > 500)
{
    // Log warning, use fallback
    poi.TtsScriptVi = poi.Description;
}

// Audio URLs should be properly formatted
if (!string.IsNullOrEmpty(poi.AudioUrl) && !Uri.TryCreate(poi.AudioUrl, UriKind.Absolute, out _))
{
    // Invalid URL, skip audio download
    poi.AudioUrl = null;
}
```

---

## SQL Queries & Patterns

### Get All Active POIs
```sql
SELECT * FROM Poi WHERE IsActive = true ORDER BY Priority DESC;
```

### Find POI by ID
```sql
SELECT * FROM Poi WHERE Id = '3fa85f64-5717-4562-b3fc-2c963f66afa6';
```

### Get Playback History (Last 24 Hours)
```sql
SELECT * FROM PlaybackHistoryEntry 
WHERE PlayedAtUtc >= datetime('now', '-1 day')
ORDER BY PlayedAtUtc DESC;
```

### Top 10 POIs by Play Count
```sql
SELECT PoiId, PoiTitle, COUNT(*) as PlayCount
FROM PlaybackHistoryEntry
GROUP BY PoiId
ORDER BY PlayCount DESC
LIMIT 10;
```

### Audio Source Distribution
```sql
SELECT AudioSource, COUNT(*) as Count
FROM PlaybackHistoryEntry
WHERE PlayedAtUtc >= datetime('now', '-7 days')
GROUP BY AudioSource
ORDER BY Count DESC;
```

### POIs Missing Content (Admin Web Future)
```sql
SELECT *
FROM Poi
WHERE TtsScriptEn IS NULL 
   OR TtsScriptZh IS NULL
   OR TtsScriptKo IS NULL;
```

---

## Data Size Estimates

### Single POI Size (SQLite)
```
Field                   | Type        | Bytes
------------------------|-------------|-------
Id                      | TEXT(36)    | 36
Title                   | TEXT(100)   | ~100
Description             | TEXT(500)   | ~500
DescriptionEn/Zh/Ko/Ja/Fr | TEXT(500) | ~2500
TtsScriptVi/En/Zh/Ko/Ja/Fr | TEXT(300) | ~1800
ImageUrl                | TEXT(256)   | ~256
AudioUrl                | TEXT(256)   | ~256
AudioUrlEn/Zh/Ko/Ja/Fr  | TEXT(256)   | ~1280
Misc fields             | Various     | ~200
                        |             | ~7928 bytes (~8 KB)
```

### Database Size for 10-100 POIs
```
POI Table:          8 KB × 10-100  = 80-800 KB
PlaybackHistory:    ~100 bytes × 1000 entries = ~100 KB
Total:              ~200-900 KB (well within device limits)
```

### Database Size Projections
```
10 POIs:        ~200 KB
100 POIs:       ~1.5 MB (single day of usage)
1000 POIs:      ~8-10 MB (NOT RECOMMENDED - consider normalization)
```

---

## Data Consistency Patterns

### Upsert Pattern (Used in SyncService)

```csharp
public async Task UpsertPoiAsync(Poi poi)
{
    var existing = await _db.Table<Poi>()
        .FirstOrDefaultAsync(p => p.Id == poi.Id);
    
    if (existing != null)
        await _db.UpdateAsync(poi);  // Update if exists
    else
        await _db.InsertAsync(poi);  // Insert if new
}
```

**Benefits:**
- Idempotent (safe to call multiple times)
- Handles both new and modified POIs
- No duplicate ID errors
- Thread-safe with write lock

### Soft Delete Pattern (IsActive Flag)

```csharp
// Don't delete, just deactivate
public async Task DeactivatePoiAsync(string poiId)
{
    var poi = await _db.Table<Poi>()
        .FirstOrDefaultAsync(p => p.Id == poiId);
    
    if (poi != null)
    {
        poi.IsActive = false;
        poi.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(poi);
    }
}

// Query only active POIs
public async Task<List<Poi>> GetActivePoisAsync()
{
    return await _db.Table<Poi>()
        .Where(p => p.IsActive)
        .ToListAsync();
}
```

**Benefits:**
- Preserves playback history integrity
- Allows "undelete" if needed
- Audit trail preserved
- No cascading deletes

---

## Future Data Model Enhancements

### 1. User Profiles (if auth added)
```csharp
public class User
{
    [PrimaryKey]
    public string Id { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string PreferredLanguage { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. User Tours (Grouping POIs)
```csharp
public class Tour
{
    [PrimaryKey]
    public string Id { get; set; }
    public string Title { get; set; }
    public List<string> PoiIds { get; set; }  // JSON array
    public DateTime CreatedAt { get; set; }
}
```

### 3. User Comments/Ratings
```csharp
public class PoiReview
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string PoiId { get; set; }
    public string? UserId { get; set; }
    public string Comment { get; set; }
    public int Rating { get; set; }  // 1-5 stars
    public DateTime CreatedAt { get; set; }
}
```

### 4. Geofence Heatmap Data
```csharp
public class UserLocationSnapshot
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string? UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Accuracy { get; set; }
    public DateTime CapturedAtUtc { get; set; }
}
```

---

**End of Data Model Guide**
