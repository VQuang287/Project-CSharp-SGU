# TOURMAP MAUI APPLICATION - COMPREHENSIVE ARCHITECTURE ANALYSIS

## Executive Summary

TourMap is a sophisticated **intelligent tourist guide system** built with .NET 10.0 MAUI for mobile and ASP.NET Core 10.0 for web administration. The application provides GPS-based geofencing, multi-language audio narration, and real-time analytics. The architecture demonstrates strong separation of concerns, reactive patterns, and careful attention to platform-specific requirements.

---

## 1. OVERALL ARCHITECTURE & DESIGN PATTERNS

### 1.1 Architectural Style: Layered + Service-Oriented Architecture

The application follows a **layered architecture with service-oriented design**:

```
┌─────────────────────────────────────────────┐
│         PRESENTATION LAYER                  │
│  (Pages: MapPage, PoiListPage, etc.)        │
├─────────────────────────────────────────────┤
│         VIEW MODEL LAYER                    │
│  (MainViewModel, state management)          │
├─────────────────────────────────────────────┤
│         SERVICE LAYER (Business Logic)      │
│  (GeofenceEngine, NarrationEngine, etc.)    │
├─────────────────────────────────────────────┤
│         DATA LAYER                          │
│  (DatabaseService, SQLite)                  │
└─────────────────────────────────────────────┘
```

### 1.2 Design Patterns Identified

#### **1. Dependency Injection (DI)**
- Implemented via `MauiProgram.cs` with comprehensive service registration
- Singletons for: `DatabaseService`, `GeofenceEngine`, `NarrationEngine`, `AuthService`, `SyncService`, `DeviceTrackingService`, `AudioPlayerService`, `LoggerService`
- Transients for: Pages, `MainViewModel`
- Proper lifetime management prevents resource leaks and ensures singleton engines operate consistently

**Code Location:** [MauiProgram.cs](MauiProgram.cs#L1-L60)

#### **2. Service Locator Pattern**
- `ServiceHelper.cs` provides static access to DI container
- Enables pages to instantiate services without constructor injection
- Used in `MapPage` for fallback initialization

**Code Location:** [ServiceHelper.cs](Services/ServiceHelper.cs)

#### **3. Event-Driven/Reactive Pattern**
- Services communicate via events: `LocationChanged`, `POITriggered`, `StateChanged`, `SpeechCompleted`
- Example: `GeofenceEngine` triggers `POITriggered` → `TourRuntimeService` listens → `NarrationEngine.OnPOITriggeredAsync()` executes
- Decouples components and enables reactive UI updates

#### **4. Singleton Pattern**
- Core engines (`GeofenceEngine`, `NarrationEngine`, `TourRuntimeService`) registered as singletons
- Ensures single GPS subscription point (SYS-C02 fix) and consistent state throughout app lifecycle

#### **5. Strategy Pattern**
- `NarrationEngine` implements prioritized audio strategy:
  1. TTS script from database (highest priority)
  2. Audio file MP3 (if language is Vietnamese)
  3. Localized description via TTS (fallback)
- `LocalizationService` uses strategy for language-specific text retrieval

#### **6. Observer Pattern**
- Pages observe narration state changes: `NarrationEngine.StateChanged` event
- TourRuntimeService relays GPS updates: `LocationUpdated` event
- Enables decoupled, reactive architecture

#### **7. Thread-Safety Patterns**
- `DatabaseService`: Uses `SemaphoreSlim` for read/write locking
- `GeofenceEngine`: Uses `lock` for POI state and cooldown management
- `GpsTrackingService_Android`: Uses `Interlocked.CompareExchange` for atomic flag checking

#### **8. Adapter/Bridge Pattern**
- Platform-specific services via preprocessor directives (`#if ANDROID`)
- `IGpsTrackingService`, `ILocationService`, `ITtsService` interfaces decouple from Android implementations
- Enables future iOS/Windows support

#### **9. Template Method Pattern**
- `TourRuntimeService` orchestrates workflow:
  1. Load POIs from database
  2. Initialize GPS tracking
  3. Listen to geofence events
  4. Route to narration engine

---

## 2. MAJOR COMPONENTS & RESPONSIBILITIES

### 2.1 CORE SERVICES

#### **2.1.1 GeofenceEngine** (Heart of Location Logic)

**Purpose:** Detects when user enters a POI radius using GPS coordinates.

**Key Algorithms:**
- **Haversine Formula**: Calculates geographic distance between two points
- **Debouncing**: Prevents rapid-fire triggers (30-second global debounce)
- **Per-POI Cooldown**: Prevents same POI triggering within 10 minutes
- **Priority Ordering**: When multiple POIs nearby, selects highest priority

**Parameters (from PRD):**
- `DebounceDurationSeconds = 30`
- `CooldownDurationMinutes = 10`
- `MaxScanRadiusMeters = 500`
- `MaxPoisInRange = 3`

**Thread-Safety:**
- Uses `lock` for accessing POI list and cooldown dictionary
- Snapshot pattern: Takes immutable copy of POI list before processing

**Key Methods:**
- `OnLocationChanged()`: Called every 5 seconds with GPS update
- `GetNearestPoi()`: Finds closest POI for map highlighting
- Event: `POITriggered` - fires when user enters valid geofence

**Code Location:** [GeofenceEngine.cs](Services/GeofenceEngine.cs)

#### **2.1.2 NarrationEngine** (Audio Playback Orchestration)

**Purpose:** Manages audio/TTS playback lifecycle with priority interruption.

**State Machine:**
```
Idle ─(OnPOITriggeredAsync)─> Playing ─(PlaybackCompleted)─> Cooldown ─(timer)─> Idle
```

**Playback Priority:**
1. **TTS Script from DB** (highest priority, offline-capable)
2. **Audio File MP3** (Vietnamese only, for offline use)
3. **Localized Description via TTS** (fallback, uses system TTS)

**Smart Interruption:**
- If PLAYING and new POI has higher priority → interrupt current playback
- If PLAYING and new POI has lower priority → ignore new POI
- If COOLDOWN → ignore all new triggers

**Audio Source Abstraction:**
- `ITtsService`: Android native TextToSpeech (6 languages)
- `IAudioPlayerService`: Plugin.Maui.Audio for MP3 playback
- Supports playback speed control (0.75x, 1.0x, 1.25x, 1.5x)

**Async Handling:**
- Uses `TaskCompletionSource` for non-blocking audio completion
- Proper disposal of resources in `Dispose()`

**Code Location:** [NarrationEngine.cs](Services/NarrationEngine.cs)

#### **2.1.3 TourRuntimeService** (App-Level Orchestration)**

**Purpose:** Coordinates all subsystems - acts as the "conductor" of the tour experience.

**Responsibilities:**
1. **Lifecycle Management**: Initializes GPS tracking, geofence, and narration
2. **Single Subscription Point**: Ensures only ONE GPS subscription (SYS-C02 fix)
3. **Event Routing**: Routes GPS → Geofence → Narration
4. **UI Relay**: Exposes `LocationUpdated` event for MapPage updates
5. **Device Tracking**: Updates current POI state for analytics

**Architecture Benefits:**
- Prevents double-subscribing to GPS (battery drain, race conditions)
- Centralizes error handling for narration failures
- Clean decoupling: Pages don't directly interact with GeofenceEngine or NarrationEngine

**Code Location:** [TourRuntimeService.cs](Services/TourRuntimeService.cs)

#### **2.1.4 DatabaseService** (Persistent Storage with Thread-Safety)**

**Purpose:** Manages SQLite database for POIs and playback history.

**Database Schema:**
```sql
CREATE TABLE Poi (
    Id TEXT PRIMARY KEY,
    Title TEXT,
    Description TEXT,
    Latitude REAL, Longitude REAL,
    RadiusMeters INTEGER,
    Priority INTEGER,
    IsActive BOOLEAN,
    ImageUrl TEXT,
    AudioUrl TEXT,
    AudioLocalPath TEXT,
    -- Multilingual descriptions (7 languages)
    DescriptionEn TEXT, DescriptionZh TEXT, DescriptionKo TEXT,
    DescriptionJa TEXT, DescriptionFr TEXT,
    -- TTS Scripts (6 languages)
    TtsScriptVi TEXT, TtsScriptEn TEXT, TtsScriptZh TEXT,
    TtsScriptKo TEXT, TtsScriptJa TEXT, TtsScriptFr TEXT,
    UpdatedAt DATETIME
);

CREATE TABLE PlaybackHistoryEntry (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PoiId TEXT,
    PoiTitle TEXT,
    TriggerType TEXT,  -- "GPS", "Manual"
    AudioSource TEXT,  -- "TTS", "AudioFile", "TTS-DB"
    PlayedAtUtc DATETIME
);
```

**Concurrency Handling:**
- `_initLock`: Ensures single initialization
- `_writeLock`: Serializes write operations (upsert, history insert)
- Backoff-retry for `SQLITE_BUSY` errors
- WAL (Write-Ahead Logging) enabled for concurrent reads

**Seed Data:**
- Pre-populates 4 sample POIs with multilingual TTS scripts
- Enables offline demo without server connection

**Key Methods:**
- `GetPoisAsync()`: Retrieves all POIs
- `UpsertPoiAsync()`: Thread-safe insert/update
- `GetPoiTtsScriptAsync()`: Retrieves TTS script by language
- `AddPlaybackHistoryAsync()`: Logs audio playback events

**Code Location:** [DatabaseService.cs](Services/DatabaseService.cs)

#### **2.1.5 AuthService** (JWT Authentication & Secure Storage)**

**Purpose:** Handles user authentication, token management, and secure credential storage.

**Features:**
- Email/password login and registration
- JWT token validation with expiration checking
- Automatic token refresh on startup
- Secure storage via `SecureStorage` (platform-specific encryption)
- Multi-server URL fallback (for redundancy)

**JWT Validation:**
- Checks token expiration via JWT claims
- Validates issuer and audience
- Refreshes automatically if near expiration

**Device Identification:**
- Generates unique device ID on first run
- Includes DeviceId in all auth requests
- Enables server-side device tracking

**Key Methods:**
- `InitializeAsync()`: Load cached token, attempt refresh
- `LoginAsync()`: Email/password authentication
- `RegisterAsync()`: New user registration
- `RefreshTokenAsync()`: Renew expired tokens
- `IsAuthenticated`: Property checking token validity

**Code Location:** [AuthService.cs](Services/AuthService.cs#L1-L150)

#### **2.1.6 SyncService** (Server Synchronization)**

**Purpose:** Syncs POI data from admin server to mobile app.

**Sync Strategy:**
- GET `/api/v1/pois/sync/pois` with optional `?since=` timestamp
- Parses JSON response and upserts to local SQLite
- Downloads associated audio files to local cache
- Stores last sync timestamp for incremental updates

**Error Handling:**
- Per-request auth headers (avoids thread-safety issues - SYS-C03 fix)
- Network error differentiation (HttpRequestException vs TimeoutException)
- Graceful fallback if server unavailable

**Code Location:** [SyncService.cs](Services/SyncService.cs)

#### **2.1.7 DeviceTrackingService** (Real-Time Analytics)**

**Purpose:** Tracks device connection, location, and playback state via SignalR.

**Features:**
- SignalR connection to admin server hub
- Automatic reconnection with exponential backoff
- Heartbeat timer (sends periodic "alive" signals)
- JWT authentication for hub connection
- Real-time POI and playback state updates

**Events:**
- `ConnectionStateChanged`: When device connects/disconnects
- `OnError`: Network/connection errors

**Code Location:** [DeviceTrackingService.cs](Services/DeviceTrackingService.cs)

#### **2.1.8 LocalizationService** (Multi-Language Support)**

**Purpose:** Manages 6-language UI and content localization.

**Supported Languages:**
- Vietnamese (vi) - default
- English (en)
- Simplified Chinese (zh-CN)
- Korean (ko-KR)
- Japanese (ja-JP)
- French (fr-FR)

**Features:**
- Singleton service with reactive language switching
- Persists selected language to `Preferences`
- Detects system language on first launch
- Updates `CultureInfo` globally
- Fires `LanguageChanged` event for UI refresh

**Code Location:** [LocalizationService.cs](Services/LocalizationService.cs)

### 2.2 PAGES (UI Layer)

#### **2.2.1 MapPage**

**Purpose:** Displays interactive map with POI markers, GPS location, and audio player control.

**Architecture:**
- Dynamically constructs UI with Mapsui library (not XAML-first)
- Full-screen map with floating header and controls
- Bottom sheet POI preview card
- Active audio player bar

**Components:**
1. **MapControl** (Mapsui): OpenStreetMap tiles + POI markers
2. **Floating Header**: Profile avatar, subtitle
3. **GPS Badge**: "Allow Location" permission button
4. **Search Bar**: POI search (not implemented yet)
5. **POI Preview Card**: Shows details when POI nearby
6. **Audio Player Bar**: Current playback info + controls

**Data Binding:**
- Observes `MainViewModel.Pois` collection
- Observes `TourRuntimeService.LocationUpdated` event
- Observes `NarrationEngine.StateChanged` event

**Map Interaction:**
- Tap POI marker → show preview card
- Long-press → open PoiDetailPage
- Click "Allow Location" button → request GPS permissions

**Code Location:** [MapPage.xaml.cs](Pages/MapPage.xaml.cs)

#### **2.2.2 PoiListPage**

**Purpose:** Scrollable list of all POIs with search and filtering.

**Features:**
- DataTemplate-based list rendering
- POI card: image, title, description, distance
- Tap card → navigate to PoiDetailPage
- Multi-language UI labels

**Code Location:** [PoiListPage.xaml.cs](Pages/PoiListPage.xaml.cs)

#### **2.2.3 PoiDetailPage**

**Purpose:** Detailed view of single POI with play button.

**Features:**
- Full POI information (title, description, image)
- "Play Narration" button to manually trigger audio
- Share location button
- Back navigation

#### **2.2.4 QrScannerPage**

**Purpose:** Scan QR codes to quickly navigate to specific POIs or tours.

#### **2.2.5 SettingsPage**

**Purpose:** App configuration and preferences.

**Features:**
- Language selector (6 options)
- Audio speed control
- About section with app version
- Clear cache option

#### **2.2.6 OfflinePacksPage**

**Purpose:** Manage offline content downloads.

**Features:**
- List of available offline packs
- Download status indicators
- Storage usage display

#### **2.2.7 AuthPages (LoginPage, RegisterPage, ProfilePage)**

**Purpose:** User authentication and profile management.

**Features:**
- Email/password login form
- Registration form with validation
- Profile view with account info

### 2.3 VIEW MODELS

#### **2.3.1 MainViewModel**

**Purpose:** Simple data container for POI collection.

**Properties:**
- `Pois`: ObservableCollection<Poi> - bound to UI lists
- `SelectedPoi`: Currently selected POI

**Methods:**
- `LoadAsync()`: Loads all POIs from database

**Architecture Note:** Currently minimal; could expand for complex state management using MVVM Toolkit.

**Code Location:** [MainViewModel.cs](ViewModels/MainViewModel.cs)

### 2.4 MODELS

#### **2.4.1 Poi** (Point of Interest)

**SQL-mapped entity with multilingual support:**
```csharp
public class Poi
{
    [PrimaryKey]
    public string Id { get; set; }  // GUID
    
    // Geographic (language-independent)
    public string Title { get; set; }
    public string Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }  // Geofence radius
    public int Priority { get; set; }       // 1-10, higher = interrupt lower
    public bool IsActive { get; set; }
    
    // Media
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? AudioLocalPath { get; set; }  // Cached file path
    
    // Multilingual descriptions (7 languages)
    public string? DescriptionEn { get; set; }
    public string? DescriptionZh { get; set; }
    public string? DescriptionKo { get; set; }
    public string? DescriptionJa { get; set; }
    public string? DescriptionFr { get; set; }
    
    // TTS Scripts (6 languages)
    public string? TtsScriptVi { get; set; }
    public string? TtsScriptEn { get; set; }
    // ... etc
}
```

**Code Location:** [Poi.cs](Models/Poi.cs)

#### **2.4.2 PlaybackHistoryEntry**

**Logs every audio playback for analytics:**
```csharp
public class PlaybackHistoryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string PoiId { get; set; }
    public string PoiTitle { get; set; }
    public string TriggerType { get; set; }      // "GPS" or "Manual"
    public string AudioSource { get; set; }      // "TTS", "AudioFile", "TTS-DB"
    public DateTime PlayedAtUtc { get; set; }
}
```

---

## 3. DATA FLOW & RELATIONSHIPS

### 3.1 Complete Data Flow Diagram

```
User Walking with App Open
        │
        ▼
GPS Location (every 5 sec) [Android OS]
        │
        ▼
GpsTrackingService_Android.OnLocationChanged()
        │
        ▼
TourRuntimeService.OnLocationChanged()
        │
        ├─→ [EVENT] LocationUpdated (for MapPage)
        │
        ▼
GeofenceEngine.OnLocationChanged()
        │ ├─ Calculate Haversine distance to all POIs
        │ ├─ Filter by MAX_SCAN_RADIUS (500m)
        │ ├─ Check debounce (30 sec global)
        │ └─ Check per-POI cooldown (10 min)
        │
        ▼ (If all checks pass)
[EVENT] POITriggered(Poi)
        │
        ▼
TourRuntimeService.OnPoiTriggered()
        │ ├─ Update DeviceTrackingService (current POI)
        │ └─ Call NarrationEngine.OnPOITriggeredAsync()
        │
        ▼
NarrationEngine.OnPOITriggeredAsync(Poi, "GPS")
        │ ├─ Check if Playing (priority interruption)
        │ ├─ Check if Cooldown (skip)
        │ └─ Set state = Playing
        │
        ▼ (Playback Priority)
        ├─ Option 1: TTS Script from DB (highest)
        │   └─ ITtsService.SpeakAsync()
        │
        ├─ Option 2: Audio File MP3 (if vi language)
        │   └─ IAudioPlayerService.PlayAsync()
        │
        └─ Option 3: Localized Description
            └─ ITtsService.SpeakAsync() (fallback)
                │
                ▼
            Audio Playback
                │
                ▼ (On Completion)
            [EVENT] AudioCompleted
                │
                ▼
            NarrationEngine.OnPlaybackCompleted()
                │ └─ State = Cooldown
                │     └─ Timer (10 min)
                │
                ▼ (After Timer)
                State = Idle (ready for next trigger)
```

### 3.2 Component Interaction Sequence

```
App Launch
│
├─ MauiProgram.CreateMauiApp()
│  └─ Register all services with DI
│
├─ App.CreateWindow()
│  └─ Instantiate SplashPage
│
├─ AppShell Navigation
│  └─ Register routes (PoiDetailPage, LoginPage, etc.)
│
└─ MapPage OnAppearing()
   ├─ MainViewModel.LoadAsync()
   │  └─ DatabaseService.GetPoisAsync()
   │
   ├─ TourRuntimeService.InitializeAsync()
   │  ├─ DatabaseService.GetPoisAsync()
   │  ├─ GeofenceEngine.UpdatePois()
   │  ├─ Subscribe to GeofenceEngine.POITriggered
   │  ├─ Subscribe to GpsTrackingService.LocationChanged
   │  └─ GpsTrackingService.StartTrackingAsync()
   │
   └─ Loop: LocationChanged → Geofence check → Narration trigger
```

### 3.3 Data Relationships

```
POI (1) ←──→ (N) PlaybackHistoryEntry
         │
         └─ References via PoiId

MapPage
   ├─ Depends on: MainViewModel
   ├─ Subscribes to: TourRuntimeService.LocationUpdated
   ├─ Subscribes to: NarrationEngine.StateChanged
   └─ Displays: Pois, current location, audio state

TourRuntimeService
   ├─ Owns: GPS tracking lifecycle
   ├─ Listens to: GeofenceEngine.POITriggered
   ├─ Listens to: GpsTrackingService.LocationChanged
   ├─ Calls: NarrationEngine.OnPOITriggeredAsync()
   └─ Updates: DeviceTrackingService

NarrationEngine
   ├─ Depends on: ITtsService, IAudioPlayerService, DatabaseService
   ├─ State: Idle → Playing → Cooldown → Idle
   └─ Fires: StateChanged event
```

---

## 4. AUTHENTICATION & DATA SYNCHRONIZATION

### 4.1 Authentication Flow

```
User Opens App
│
├─ AuthService.InitializeAsync()
│  ├─ Check SecureStorage for cached token
│  ├─ If found:
│  │  ├─ Validate token expiration
│  │  └─ If expired: RefreshTokenAsync()
│  │
│  └─ If not found or refresh fails:
│     └─ Redirect to LoginPage
│
└─ User enters email/password
   │
   └─ LoginAsync(email, password)
      ├─ Generate device ID
      ├─ POST /login-user with DeviceId
      ├─ On success:
      │  ├─ Save JWT token to SecureStorage
      │  ├─ Save UserProfile JSON
      │  └─ Navigate to MapPage
      │
      └─ On failure:
         └─ Show error message
```

**Secure Storage:**
- Android: Uses Keystore (encrypted)
- iOS: Uses Keychain
- Windows: Uses DPAPI

**Token Refresh:**
- On startup if token near expiration
- Automatic if endpoint returns 401

**Multi-Server Fallback:**
- Tries multiple backend URLs in sequence
- Supports redundant server setup

**Code Location:** [AuthService.cs](Services/AuthService.cs)

### 4.2 Data Synchronization

```
App Launch (if network available)
│
├─ SyncService.SyncPoisFromServerAsync(serverUrl)
│  ├─ GET /api/v1/pois/sync/pois?since=<last_sync_time>
│  ├─ Parse JSON response
│  │
│  └─ For each POI:
│     ├─ Convert DTO to Poi model
│     ├─ Download audio files
│     │  └─ Save to {AppData}/audio/
│     │
│     └─ UpsertPoiAsync()
│        └─ DatabaseService.UpsertPoiAsync()
│           └─ Update SQLite
│
└─ Save last_sync_time to Preferences
```

**Sync Strategy:**
- **Incremental**: Uses `?since=` parameter to fetch only changed POIs
- **Resilient**: Continues if audio download fails (TTS fallback available)
- **Offline-First**: App works without sync (uses seed data)

**Audio Caching:**
- Downloads MP3 files to `{AppDataDirectory}/audio/`
- Updates POI.AudioLocalPath to cache location
- Falls back to TTS if file not cached

**Last Sync Tracking:**
- Stored in `Preferences.Default` as ISO 8601 datetime
- Used for delta sync on next startup

**Code Location:** [SyncService.cs](Services/SyncService.cs)

---

## 5. PLATFORM-SPECIFIC IMPLEMENTATIONS (ANDROID)

### 5.1 Location Services

#### **5.1.1 ILocationService Interface**

**Abstraction for one-time location request:**
```csharp
public interface ILocationService
{
    Task<Location?> GetCurrentLocationAsync();
}
```

#### **5.1.2 LocationService_Android**

**Implementation using MAUI Geolocation API:**
- Requests `LocationWhenInUse` permission
- Uses `Geolocation.Default.GetLocationAsync()`
- Timeout: 10 seconds
- Accuracy: Best available

**Code Location:** [LocationService_Android.cs](Platforms/Android/LocationService_Android.cs)

### 5.2 GPS Tracking Services

#### **5.2.1 IGpsTrackingService Interface**

**Abstraction for continuous GPS tracking:**
```csharp
public interface IGpsTrackingService
{
    event Action<Location>? LocationChanged;
    bool IsTracking { get; }
    Location? LastKnownLocation { get; }
    Task StartTrackingAsync();
    void StopTracking();
}
```

#### **5.2.2 GpsTrackingService_Android**

**Continuous GPS tracking via polling:**

**Permission Requests:**
1. `LocationWhenInUse`: Required for foreground tracking
2. `LocationAlways` (Android 12+): Required for background tracking

**Tracking Strategy:**
- Polls every 5 seconds (configurable intervals)
- Adaptive interval: 5s moving, 3s fast moving, 10s stationary
- Detects stationary state with 15m movement threshold
- Filters GPS noise: ignores readings with accuracy > 50m

**Foreground Service:**
- Starts `LocationForegroundService` to keep app alive in background
- Shows notification: "TourMap is tracking your location"
- Prevents Android from killing app due to inactivity

**Thread-Safety:**
- Uses `Interlocked.CompareExchange` for atomic flag checking
- Prevents concurrent tracking starts
- Clean cancellation via `CancellationTokenSource`

**Error Handling:**
- Catches and logs permission denials
- Logs accuracy filtering
- Catches GPS polling errors

**Code Location:** [GpsTrackingService_Android.cs](Platforms/Android/GpsTrackingService_Android.cs)

### 5.3 Text-to-Speech Services

#### **5.3.1 ITtsService Interface**

**Abstraction for speech synthesis:**
```csharp
public interface ITtsService
{
    bool IsSpeaking { get; }
    event Action? SpeechCompleted;
    Task SpeakAsync(string text, string languageCode = "vi");
}
```

#### **5.3.2 TtsService_Android**

**Native Android TextToSpeech engine:**

**Supported Languages:**
- Vietnamese (vi-VN) - preferred
- English (en-US)
- Simplified Chinese (zh-CN)
- Korean (ko-KR)
- Japanese (ja-JP)
- French (fr-FR)

**Initialization:**
- Creates `Android.Speech.Tts.TextToSpeech` instance
- Implements `IOnInitListener` interface
- Handles initialization callback

**Language Fallback:**
- If device doesn't support Vietnamese → English
- Sets language via `SetLanguage(Locale)`

**Asynchronous Speech:**
- Uses `TaskCompletionSource<bool>` for non-blocking speech
- Fires `SpeechCompleted` event when utterance finishes
- Proper event cleanup to prevent memory leaks

**Pitch & Speed Control:**
- Sets pitch and speaking rate via TTS engine

**Error Handling:**
- Catches Java exceptions from TTS engine
- Validates Android application context
- Logs language availability checks

**Code Location:** [TtsService_Android.cs](Platforms/Android/TtsService_Android.cs)

### 5.4 Audio Player Services

#### **5.4.1 IAudioPlayerService Interface**

**Abstraction for MP3 audio playback:**
```csharp
public interface IAudioPlayerService
{
    bool IsPlaying { get; }
    float Speed { get; set; }
    event Action? AudioCompleted;
    Task PlayAsync(string filePath);
    void Stop();
}
```

#### **5.4.2 AudioPlayerService**

**Implementation using Plugin.Maui.Audio:**

**Features:**
- Plays MP3 files from local storage
- Supports playback speed control (0.75x - 1.5x)
- Non-blocking playback via TaskCompletionSource
- Proper resource disposal

**Playback Flow:**
1. Opens file stream
2. Creates IAudioPlayer via AudioManager
3. Applies speed setting
4. Plays audio
5. Waits for PlaybackEnded event
6. Fires AudioCompleted event
7. Disposes stream and player

**Error Handling:**
- Checks file existence before playback
- Catches FileNotFoundException, UnauthorizedAccessException
- Logs audio format compatibility issues
- Fires completion event even on errors (prevents UI hanging)

**Code Location:** [AudioPlayerService.cs](Services/AudioPlayerService.cs)

### 5.5 Foreground Service for Background Location Tracking

#### **5.5.1 LocationForegroundService**

**Purpose:** Keeps GPS tracking active even when app is in background.

**Implementation:**
```csharp
[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class LocationForegroundService : Service
```

**Features:**
- Requires Android API 31+ declaration: `ForegroundServiceType.TypeLocation`
- Shows persistent notification while tracking
- Handles service stop via intent action: `STOP_SERVICE`

**Notification:**
- Title: "TourMap đang hoạt động"
- Text: "Đang dò tìm tọa độ GPS để phát Audio Guide..."
- Taps to open app via PendingIntent
- Marked as ongoing (cannot be swiped away)

**API Level Handling:**
- Android 8.0+: Requires NotificationChannel
- Android 12+: Requires immutable PendingIntent flags
- Fallback notification if creation fails

**Code Location:** [LocationForegroundService.cs](Platforms/Android/LocationForegroundService.cs)

### 5.6 Android Manifest Permissions

**Code Location:** [AndroidManifest.xml](Platforms/Android/AndroidManifest.xml)

**Required Permissions:**
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

**Runtime Permissions (Requested by MAUI):**
- `Permissions.LocationWhenInUse` (foreground)
- `Permissions.LocationAlways` (background, Android 12+)

---

## 6. ADMIN WEB PROJECT STRUCTURE

### 6.1 Architecture Overview

**Shared Database Approach:**
- Mobile app and web admin share SQLite database
- Enables real-time bidirectional sync

**Tech Stack:**
- Framework: ASP.NET Core 10.0
- Database: Entity Framework Core + SQLite
- UI: Bootstrap 5 + Razor Pages
- APIs: SignalR for real-time communication

### 6.2 Project Structure

```
TourMap.AdminWeb/
├── Program.cs (Startup & DI configuration)
├── appsettings.json (Connection strings, JWT settings)
├── appsettings.Development.json
│
├── Data/
│   ├── AdminDbContext.cs (EF Core DbContext)
│   └── Database initialization
│
├── Models/
│   ├── Poi.cs
│   ├── PoiTranslation.cs
│   └── PlaybackHistoryEntry.cs
│
├── Controllers/
│   ├── HomeController.cs (Dashboard)
│   ├── PoisController.cs (CRUD operations)
│   ├── AuthController.cs (User authentication)
│   └── ApiController.cs (REST endpoints)
│
├── Views/
│   ├── Home/Index.cshtml (Dashboard)
│   ├── Pois/Index.cshtml (POI list)
│   ├── Pois/Create.cshtml
│   ├── Pois/Edit.cshtml
│   └── Shared/_Layout.cshtml
│
├── Hubs/
│   └── DeviceTrackingHub.cs (SignalR hub for device tracking)
│
├── Services/
│   ├── PoisService.cs
│   └── DeviceTrackingService.cs
│
├── ViewModels/
│   ├── PoiViewModel.cs
│   └── DashboardViewModel.cs
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── uploads/ (Images, audio files)
│
└── App_Data/
    ├── keys/ (Data protection keys)
    └── AdminTourMap.db (SQLite database)
```

### 6.3 Authentication Architecture

**Program.cs Setup:**
```csharp
// Dual authentication: Cookies (UI) + JWT (API)
.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        // Sliding expiration: 8 hours
        // HttpOnly: prevents JavaScript access
        // SameSite: Lax (CSRF protection)
    })
    .AddJwtBearer(options => {
        // Token validation
        // Issuer & Audience verification
    });
```

**JWT Configuration:**
- Secret key: Retrieved from appsettings or environment variable
- Issuer: `TourMap.AdminWeb`
- Audience: `TourMap.MobileApp`

### 6.4 SignalR Hub for Real-Time Device Tracking

**DeviceTrackingHub.cs:**
- **RegisterDevice()**: Called by mobile app on connection
- **SendHeartbeat()**: Periodic device alive signal
- **UpdateLocation()**: Current GPS coordinates
- **UpdateNarrationState()**: Current playing POI
- **Receive**: Admin can see real-time device status

---

## 7. KEY BUSINESS LOGIC & FEATURES

### 7.1 Geofencing Algorithm (Heart of the App)

**Haversine Formula Implementation:**
```csharp
public static double Haversine(double lat1, double lng1, double lat2, double lng2)
{
    const double R = 6_371_000; // Earth radius in meters
    var dLat = (lat2 - lat1) * Math.PI / 180.0;
    var dLng = (lng2 - lng1) * Math.PI / 180.0;
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}
```

**Triggering Logic:**
1. **Debounce Check** (30 sec global): Prevents rapid-fire triggers
2. **Scan Radius Filter** (500m): Only consider nearby POIs
3. **Distance Calculation**: Haversine between user and each POI
4. **Candidate Selection**: Top 3 closest POIs, filtered by trigger radius
5. **Priority Ordering**: Highest priority wins if multiple in range
6. **Per-POI Cooldown** (10 min): Prevents same POI repeated triggers

**Complexity:** O(n log n) where n = number of POIs (acceptable for 10-100 POIs)

### 7.2 Audio Playback Priority System

**Intelligent Fallback Chain:**
1. **TTS Script from Database** (Offline, customized text)
   - Best user experience
   - Requires admin to write scripts
   
2. **Audio File MP3** (Vietnamese only, pre-recorded)
   - High quality narration
   - Requires memory to cache files
   
3. **Localized Description via TTS** (Universal, automatic)
   - Always available
   - Lower quality (generic)

**Interruption Logic:**
```csharp
if (CurrentState == Playing)
{
    if (NewPoi.Priority > CurrentPoi.Priority)
        // Interrupt with higher priority
    else
        // Ignore lower priority
}
else if (CurrentState == Cooldown)
    // Ignore (in cooldown period)
```

### 7.3 Multi-Language Content Management

**Language Support:**
- Vietnamese (vi) - Primary
- English (en)
- Simplified Chinese (zh)
- Korean (ko)
- Japanese (ja)
- French (fr)

**Content Fields per Language:**
- Title
- Description
- TTS Script
- Audio file

**Language Switching:**
- Runtime switching without app restart
- Updates `CultureInfo` globally
- Fires `LanguageChanged` event for UI refresh
- Audio engine switches language on next trigger

### 7.4 Offline Capability

**Offline-First Design:**
- All core data stored in local SQLite
- GPS tracking doesn't require network
- Audio playback from cached files
- Geofence triggers work offline

**Seed Data:**
- 4 sample POIs pre-loaded in database
- Enables demo without server setup

**Sync Capability:**
- Syncs with server when network available
- Downloads missing audio files
- Incremental updates via timestamp

---

## 8. IDENTIFIED PATTERNS, CONVENTIONS & BEST PRACTICES

### 8.1 Naming Conventions

**Services:** `<Feature>Service` (AuthService, DatabaseService)
**Engines:** `<Feature>Engine` (GeofenceEngine, NarrationEngine)
**Interfaces:** `I<Service>` (ILocationService, IGpsTrackingService)
**Pages:** `<PageName>Page` (MapPage, PoiDetailPage)
**Models:** `<Entity>` (Poi, PlaybackHistoryEntry)

### 8.2 Code Comments & Documentation

**Multilingual Comments:** Code includes Vietnamese and English comments
**Detailed Headers:** Service classes have comprehensive purpose statements
**Progress Indicators:** Console.WriteLine() logs with emoji status indicators (🎯, ✅, ❌, ⚠️)

### 8.3 Error Handling Patterns

**Try-Catch with Specific Exception Types:**
```csharp
try { /* operation */ }
catch (HttpRequestException httpEx)
{
    Console.WriteLine($"Network error: {httpEx.StatusCode}");
}
catch (TaskCanceledException)
{
    Console.WriteLine("Request timeout");
}
catch (Exception ex)
{
    Console.WriteLine($"General error: {ex.Message}");
}
```

**Graceful Degradation:**
- If TTS fails → fallback to audio file
- If audio file not cached → fallback to TTS
- If network unavailable → use local data
- If permission denied → disable feature

### 8.4 Async/Await Patterns

**Non-blocking Operations:**
- All I/O operations are async
- TaskCompletionSource for custom async operations
- Proper CancellationToken propagation

**Examples:**
- `await _databaseService.GetPoisAsync()`
- `await _ttsService.SpeakAsync(text, lang)`
- `await _gpsTrackingService.StartTrackingAsync()`

### 8.5 Thread-Safety Patterns

**Semaphore for Write Operations:**
```csharp
await _writeLock.WaitAsync();
try { /* write operation */ }
finally { _writeLock.Release(); }
```

**Lock for Fast Operations:**
```csharp
lock (_lock) { /* update collection */ }
```

**Interlocked for Flags:**
```csharp
if (Interlocked.CompareExchange(ref _flag, 1, 0) == 1)
    return; // Already running
```

### 8.6 Resource Disposal

**Implementing IDisposable:**
```csharp
public void Dispose()
{
    if (_disposed) return;
    _gpsTrackingService.LocationChanged -= OnLocationChanged;
    _disposed = true;
}
```

**Proper Cleanup:**
- Unsubscribe from events
- Close database connections
- Stop foreground services
- Dispose audio players and streams

### 8.7 Configuration Management

**MauiProgram.cs:**
- Centralizes service registration
- Clear lifetime management (Singleton vs Transient)
- Comments explain architectural decisions

**appsettings.json:**
- JWT configuration
- Database connection strings
- Server URLs with fallback list

---

## 9. POTENTIAL ISSUES & AREAS FOR IMPROVEMENT

### 9.1 Architecture Issues

#### Issue #1: Limited ViewModel Logic
**Severity:** Low  
**Description:** `MainViewModel` is minimal; no complex state management.  
**Impact:** As app grows, state management could become scattered across Pages.  
**Recommendation:**
- Consider MVVM Toolkit for property change notifications
- Implement ViewModel base class with common patterns
- Move page state logic to ViewModels

#### Issue #2: Service Locator Anti-Pattern
**Severity:** Low-Medium  
**Description:** `ServiceHelper.GetService<T>()` allows static service access.  
**Impact:** Makes dependency chains less visible; harder to test.  
**Recommendation:**
- Prefer constructor injection for all Pages
- Use ServiceHelper only as fallback
- Add unit tests with mock DI container

#### Issue #3: Circular Event Dependencies
**Severity:** Low  
**Description:** Multiple event subscriptions could create hard-to-trace flows.  
**Impact:** Debugging and maintenance complexity.  
**Recommendation:**
- Document event chains in comments
- Consider request-response pattern for some interactions
- Add event logging for debugging

### 9.2 Performance Issues

#### Issue #1: GPS Polling Every 5 Seconds
**Severity:** Medium  
**Description:** Continuous polling drains battery faster than adaptive polling.  
**Impact:** Battery life concern for full-day tours.  
**Recommendation:**
- Use accelerometer to detect movement
- Increase polling interval when stationary
- Monitor battery impact in real devices

#### Issue #2: Geofence Calculation for Every POI
**Severity:** Low  
**Description:** O(n) distance calculation for all POIs per GPS update.  
**Impact:** Negligible for 10-100 POIs, but scales poorly with 1000s.  
**Recommendation:**
- Implement spatial indexing (quad-tree) for future scaling
- Cache POI coordinates in memory
- Profile on target devices

#### Issue #3: Unoptimized Map Rendering
**Severity:** Low  
**Description:** Full-screen Mapsui map with dynamic marker updates.  
**Impact:** Potential frame drops on lower-end devices.  
**Recommendation:**
- Batch marker updates
- Use marker clustering for many POIs
- Profile on Android API 31 devices

### 9.3 Data & Persistence Issues

#### Issue #1: No Data Validation on Sync
**Severity:** Medium  
**Description:** SyncService doesn't validate POI data from server before inserting.  
**Impact:** Corrupted server data could break app functionality.  
**Recommendation:**
- Add data validation layer
- Log invalid records to separate table
- Notify admin of sync errors

#### Issue #2: No Data Backup/Recovery
**Severity:** Medium  
**Description:** SQLite database has no backup if corrupted.  
**Impact:** User loses all progress/offline data.  
**Recommendation:**
- Implement automatic backups to cloud
- Add database integrity checks on startup
- Provide factory reset with warning

#### Issue #3: Playback History Not Synced to Server
**Severity:** Low  
**Description:** PlaybackHistoryEntry only exists on mobile; analytics can't see data.  
**Impact:** Analytics features incomplete.  
**Recommendation:**
- Upload playback history to server periodically
- Batch requests to minimize network usage
- Implement incremental sync similar to POIs

### 9.4 Security Issues

#### Issue #1: JWT Token Stored in SecureStorage But Checked Every Time
**Severity:** Low  
**Description:** Token validation happens in code even though OS provides encryption.  
**Impact:** Minor redundancy, not a security risk.  
**Recommendation:**
- Streamline token validation
- Cache validation result with timeout

#### Issue #2: No Certificate Pinning for HTTPS
**Severity:** Medium  
**Description:** App trusts any valid HTTPS certificate (vulnerable to MITM).  
**Impact:** Potential data interception on untrusted networks.  
**Recommendation:**
- Implement certificate pinning for admin server
- Add network security configuration (Android)

#### Issue #3: Audio File URLs Exposed in Database
**Severity:** Low  
**Description:** POI.AudioUrl contains full path, potentially exposing server structure.  
**Impact:** Minor information disclosure.  
**Recommendation:**
- Use relative paths or obfuscated IDs
- Implement server-side URL signing

### 9.5 Maintainability Issues

#### Issue #1: Large ServiceHelper Static Helper
**Severity:** Low  
**Description:** ServiceHelper.cs mixes platform-specific conditionals.  
**Impact:** Hard to extend to new platforms.  
**Recommendation:**
- Use platform-specific partial classes
- Create factory pattern for service provider

#### Issue #2: No Logging Framework
**Severity:** Low  
**Description:** LoggerService is minimal; uses Debug.WriteLine.  
**Impact:** Hard to diagnose issues on production devices.  
**Recommendation:**
- Integrate structured logging (Serilog, NLog)
- Log to file with rotation
- Remote logging to server for error tracking

#### Issue #3: Comments in Vietnamese Mixed with English
**Severity:** Very Low  
**Description:** Code comments mix Vietnamese and English inconsistently.  
**Impact:** Readability variance.  
**Recommendation:**
- Choose single language for code (English recommended)
- Keep Vietnamese for UI strings only

### 9.6 Testing Issues

#### Issue #1: No Unit Tests
**Severity:** Medium  
**Description:** No unit test project; services tightly coupled to platform APIs.  
**Impact:** Hard to test business logic; regression risk on changes.  
**Recommendation:**
- Create `TourMap.Tests` project
- Abstract platform APIs behind interfaces (already done for most)
- Test GeofenceEngine, NarrationEngine logic in isolation

#### Issue #2: No UI Tests
**Severity:** Low  
**Description:** No automated UI testing; only manual testing possible.  
**Impact:** Regressions in UI flows not caught early.  
**Recommendation:**
- Use Appium for Android UI testing
- Create smoke tests for key flows (login, map, playback)

#### Issue #3: Integration Tests Missing
**Severity:** Medium  
**Description:** No integration tests between services.  
**Impact:** Service integration issues found late.  
**Recommendation:**
- Create integration test suite
- Test full data flow: GPS → Geofence → Narration
- Mock external services (Auth, Sync)

---

## 10. ARCHITECTURE STRENGTHS

### ✅ Well-Designed Service Layer
- Clear separation of concerns
- Event-driven communication
- Platform abstraction via interfaces

### ✅ Smart Geofencing Algorithm
- Haversine formula for accuracy
- Debounce & cooldown for reliability
- Priority system for complex scenarios

### ✅ Robust Audio Strategy
- Intelligent fallback chain
- Offline capability
- Multiple audio sources (file, TTS)

### ✅ Multi-Language Support
- 6 language support
- Runtime language switching
- Comprehensive localization

### ✅ Thread-Safe Concurrency
- Proper locking mechanisms
- No obvious race conditions
- Careful resource cleanup

### ✅ Offline-First Design
- App works without network
- Graceful sync when available
- Seed data for demo

### ✅ Platform-Specific Excellence
- Android foreground service for background tracking
- Native TTS for quality
- Proper permission handling

---

## 11. RECOMMENDATIONS FOR ENHANCEMENT

### Priority 1 (Critical)
1. **Add comprehensive unit tests** - Especially GeofenceEngine, NarrationEngine
2. **Implement certificate pinning** - Secure server communication
3. **Add remote error logging** - Diagnose production issues
4. **Validate sync data from server** - Prevent data corruption

### Priority 2 (Important)
1. **Refactor ServiceHelper** - Reduce static access
2. **Implement MVVM Toolkit** - Better state management
3. **Add database backups** - Protect user data
4. **Optimize GPS polling** - Accelerometer-based adaptation
5. **Improve analytics sync** - Upload playback history to server

### Priority 3 (Nice-to-Have)
1. **Add Serilog integration** - Structured logging
2. **Create UI test suite** - Automation for regression testing
3. **Implement spatial indexing** - For scaling to 1000s of POIs
4. **Add heatmap overlay** - Visualize popular POIs
5. **Implement tour bundles** - Group related POIs

---

## 12. CONCLUSION

**TourMap is a well-architected MAUI application** with strong fundamentals:

- **Solid Service-Oriented Architecture**: Clear separation, event-driven, extensible
- **Intelligent Business Logic**: Sophisticated geofencing and audio orchestration
- **Platform Excellence**: Proper Android integration with foreground services, TTS, permissions
- **Offline Capability**: Works without network, syncs when available
- **Multi-Language**: 6-language support with runtime switching
- **Thread-Safe**: Careful concurrency patterns for GPS and database

**Main areas for improvement:**
- Testing (unit, integration, UI)
- Logging and observability
- State management scaling
- Security enhancements (certificate pinning)

**Overall Assessment:** Production-ready with recommended enhancements for long-term maintenance and scalability.

---

## Appendix: File Structure Reference

```
TourMap/
├── Models/
│   └── Poi.cs                              # Data model with multilingual fields
├── Services/
│   ├── AuthService.cs                      # JWT auth + secure storage
│   ├── DatabaseService.cs                  # SQLite persistence, thread-safe
│   ├── DeviceTrackingService.cs           # SignalR device tracking
│   ├── GeofenceEngine.cs                   # Haversine-based geofencing logic
│   ├── NarrationEngine.cs                 # Audio/TTS playback orchestration
│   ├── TourRuntimeService.cs              # App-level orchestration
│   ├── SyncService.cs                      # POI data sync from server
│   ├── AudioPlayerService.cs               # MP3 playback
│   ├── LocalizationService.cs             # 6-language support
│   ├── LoggerService.cs                    # Basic logging
│   ├── ServiceHelper.cs                    # DI service locator
│   ├── IAudioPlayerService.cs              # Audio interface
│   ├── IGpsTrackingService.cs             # GPS interface
│   ├── ILocationService.cs                 # Location interface
│   ├── ILoggerService.cs                   # Logger interface
│   └── ITtsService.cs                      # TTS interface
├── Pages/
│   ├── MapPage.xaml.cs                     # Interactive map + audio controls
│   ├── MapPage.xaml
│   ├── PoiListPage.xaml                    # POI list view
│   ├── PoiListPage.xaml.cs
│   ├── PoiDetailPage.cs                    # Single POI details
│   ├── QrScannerPage.cs                    # QR code scanning
│   ├── SettingsPage.cs                     # Language + preferences
│   ├── OfflinePacksPage.cs                 # Offline content
│   ├── LoginPage.cs                        # User authentication
│   ├── RegisterPage.cs                     # User registration
│   ├── ProfilePage.cs                      # User profile
│   └── SplashPage.cs                       # App startup
├── ViewModels/
│   └── MainViewModel.cs                    # POI collection binding
├── Platforms/Android/
│   ├── GpsTrackingService_Android.cs      # Continuous GPS polling
│   ├── LocationService_Android.cs          # One-time location request
│   ├── TtsService_Android.cs              # Android native TTS
│   ├── LocationForegroundService.cs       # Background service
│   ├── MainActivity.cs                     # Android entry point
│   ├── MainApplication.cs                  # App initialization
│   ├── AndroidManifest.xml                 # Permissions
│   └── Resources/
├── MauiProgram.cs                          # Startup + DI registration
├── App.xaml.cs                             # App entry point
├── AppShell.xaml                           # Navigation shell + tabs
├── AppShell.xaml.cs
├── MainPage.xaml                           # Fallback page (not used)
├── TourMap.csproj                          # Project configuration
└── _docs/
    ├── PRD_TourMap.md                      # Product requirements document
    ├── module_status.csv                   # Feature status tracker
    ├── tourmap_deep_code_review.md        # Code review notes
    └── ...

TourMap.AdminWeb/
├── Program.cs                              # ASP.NET Core startup
├── appsettings.json                        # Configuration
├── Controllers/
│   ├── HomeController.cs                   # Dashboard
│   ├── PoisController.cs                   # POI CRUD
│   └── AuthController.cs                   # Authentication
├── Models/
│   ├── Poi.cs
│   └── PoiTranslation.cs
├── Views/
│   ├── Home/Index.cshtml                  # Dashboard
│   ├── Pois/Index.cshtml                  # POI list
│   └── Shared/_Layout.cshtml              # Layout template
├── Hubs/
│   └── DeviceTrackingHub.cs               # SignalR hub
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── uploads/
├── Data/
│   └── AdminDbContext.cs                   # EF Core DbContext
└── TourMap.AdminWeb.csproj
```

---

**End of Analysis Document**
