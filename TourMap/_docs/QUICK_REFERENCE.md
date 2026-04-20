# TOURMAP QUICK REFERENCE GUIDE

## System Overview

**TourMap** is an intelligent GPS-based tourist guide system with:
- **Mobile**: Android MAUI app with geofencing, audio narration, 6-language support
- **Web Admin**: ASP.NET Core CMS for POI management
- **Analytics**: Real-time device tracking & statistics dashboard

---

## Architecture at a Glance

### Design Pattern
**Layered + Service-Oriented Architecture**

```
Pages (UI) → ViewModels → Services (Business Logic) → Database (SQLite)
                                    ↓
                            Platform-Specific (Android)
```

### Key Design Patterns
- **Dependency Injection** (MauiProgram.cs)
- **Event-Driven** (services communicate via events)
- **Singleton Engines** (GeofenceEngine, NarrationEngine, TourRuntimeService)
- **Thread-Safe** (locks, semaphores, atomic operations)
- **Strategy Pattern** (audio playback fallback chain)
- **Platform Abstraction** (interfaces: ILocationService, IGpsTrackingService, ITtsService)

---

## Core Services

| Service | Responsibility | Lifetime |
|---------|-----------------|----------|
| **GeofenceEngine** | Haversine distance calculation, POI trigger logic | Singleton |
| **NarrationEngine** | Audio/TTS playback, state management | Singleton |
| **TourRuntimeService** | Orchestration: GPS → Geofence → Narration | Singleton |
| **DatabaseService** | SQLite persistence (thread-safe) | Singleton |
| **AuthService** | JWT auth, secure token storage | Singleton |
| **SyncService** | Sync POI data from server | Singleton |
| **DeviceTrackingService** | SignalR connection for real-time analytics | Singleton |
| **LocalizationService** | 6-language support with runtime switching | Singleton |
| **AudioPlayerService** | MP3 playback via Plugin.Maui.Audio | Singleton |
| **TtsService_Android** | Android native TextToSpeech | Singleton |
| **GpsTrackingService_Android** | Continuous GPS polling (5sec intervals) | Singleton |
| **LocationForegroundService** | Android background service for location tracking | Service |

---

## Data Flow

### GPS Detection to Audio Playback
```
User walks into POI radius (every 5 seconds)
    ↓
GpsTrackingService polls location
    ↓
GeofenceEngine.OnLocationChanged()
  - Haversine distance calculation
  - Check debounce (30 sec global)
  - Check cooldown (10 min per POI)
  - Check priority (if multiple POIs nearby)
    ↓ (If all checks pass)
GeofenceEngine.POITriggered event fires
    ↓
TourRuntimeService.OnPoiTriggered()
    ↓
NarrationEngine.OnPOITriggeredAsync()
  - Check state (Playing/Cooldown/Idle)
  - Select audio source:
    1. TTS script from DB (highest priority)
    2. MP3 audio file (if language = Vietnamese)
    3. Localized description via TTS (fallback)
    ↓
Audio playback starts
    ↓
AudioCompleted event → State = Cooldown (10 min)
    ↓
After cooldown → State = Idle (ready for next POI)
```

---

## Database Schema

### Poi Table
```
Id (TEXT, PK)
Title, Description (TEXT)
Latitude, Longitude (REAL)
RadiusMeters, Priority (INTEGER)
IsActive (BOOLEAN)
ImageUrl, AudioUrl, AudioLocalPath (TEXT)
-- Multilingual (7 languages)
DescriptionEn, DescriptionZh, DescriptionKo, DescriptionJa, DescriptionFr
-- TTS Scripts (6 languages)
TtsScriptVi, TtsScriptEn, TtsScriptZh, TtsScriptKo, TtsScriptJa, TtsScriptFr
UpdatedAt (DATETIME)
```

### PlaybackHistoryEntry Table
```
Id (INTEGER, PK, AUTOINCREMENT)
PoiId, PoiTitle (TEXT)
TriggerType (TEXT) -- "GPS" or "Manual"
AudioSource (TEXT) -- "TTS", "AudioFile", "TTS-DB"
PlayedAtUtc (DATETIME)
```

---

## Key Algorithms

### 1. Geofencing (GeofenceEngine)

**Haversine Formula for Distance:**
```csharp
Distance = R × 2 × arctan2(√a, √(1-a))
where a = sin²(Δlat/2) + cos(lat1) × cos(lat2) × sin²(Δlng/2)
      R = 6,371,000 meters (Earth radius)
```

**Trigger Logic:**
1. Calculate distance to all POIs using Haversine
2. Filter by MaxScanRadiusMeters (500m)
3. Check global debounce (30 sec) - prevents rapid-fire
4. Check per-POI cooldown (10 min) - prevents repetition
5. Select candidate: highest priority among those in range
6. Fire POITriggered event

### 2. Audio Playback Priority (NarrationEngine)

**Playback Strategy (in order):**
1. **TTS Script from DB** - Offline, customized by admin
2. **Audio File MP3** - High quality (Vietnamese only)
3. **Localized Description via TTS** - Universal fallback

**Interruption Rules:**
```
If PLAYING:
  If NewPoi.Priority > CurrentPoi.Priority: INTERRUPT
  Else: IGNORE new POI
If COOLDOWN:
  IGNORE all new POIs
If IDLE:
  ACCEPT new POI
```

### 3. Language Switching (LocalizationService)

- **6 Supported Languages**: vi, en, zh, ko, ja, fr
- **Runtime Switch**: No app restart needed
- **Global Effect**: Updates all UI labels and TTS language
- **Persistence**: Saves to Preferences for next startup

---

## Android-Specific Implementation

### Permissions
- **Foreground**: `ACCESS_FINE_LOCATION` (always required)
- **Background**: `ACCESS_BACKGROUND_LOCATION` (Android 12+)
- **Runtime**: MAUI handles via `Permissions.LocationWhenInUse()` and `Permissions.LocationAlways()`

### GPS Tracking Strategy
- **Continuous Polling**: Every 5 seconds (configurable)
- **Noise Filter**: Ignore readings with accuracy > 50m
- **Adaptive Intervals**: 3-10 sec depending on movement
- **Background Service**: `LocationForegroundService` keeps app alive
- **Notification**: Shows "TourMap is tracking..." while active

### Audio/TTS
- **Android TTS Engine**: Native `Android.Speech.Tts.TextToSpeech`
- **Supported**: 6 languages (vi, en, zh-CN, ko, ja, fr)
- **Fallback**: Vietnamese if device doesn't support requested language
- **Playback**: Plugin.Maui.Audio for MP3 files

### Foreground Service
```csharp
[Service(ForegroundServiceType = ForegroundService.TypeLocation)]
public class LocationForegroundService : Service
```
- Keeps GPS tracking alive when app in background
- Shows persistent notification (can't be swiped away)
- Required for Android 12+ background location access

---

## Authentication & Security

### Token Management
1. **Login**: Email + password + DeviceId
2. **Token Storage**: SecureStorage (platform-encrypted)
3. **Token Validation**: Check expiration on startup
4. **Auto-Refresh**: If expired, refresh via /refresh-token endpoint
5. **Logout**: Clear SecureStorage

### Secure Storage
- **Android**: Keystore (encrypted by OS)
- **iOS**: Keychain
- **Windows**: DPAPI

### JWT Configuration
- **Secret Key**: From appsettings.json or JWT_SECRET_KEY env var
- **Issuer**: TourMap.AdminWeb
- **Audience**: TourMap.MobileApp
- **Expiration**: Configurable (default 24 hours)

---

## Data Synchronization

### Sync Flow
```
App Startup
    ↓
Check network availability
    ↓ (If available)
SyncService.SyncPoisFromServerAsync()
    ├─ GET /api/v1/pois/sync/pois?since=<last_sync_time>
    ├─ Parse JSON response
    └─ For each POI:
        ├─ Download audio files
        └─ UpsertPoiAsync() to SQLite
    ↓
Save last_sync_time to Preferences
    ↓
Offline-First: App works without sync (uses seed data)
```

### Resilience
- **Multi-Server Fallback**: Tries multiple server URLs
- **Incremental Sync**: Uses `?since=` timestamp
- **Error Graceful**: Continues if audio download fails (TTS fallback)
- **Offline Capable**: App fully functional without sync

---

## State Management

### NarrationEngine State Machine
```
Idle 
  ├─ OnPOITriggeredAsync() → Playing
  └─ [Ready to accept triggers]

Playing
  ├─ OnPlaybackCompleted() → Cooldown
  ├─ OnPOITriggeredAsync(HigherPriority) → Playing (interrupt)
  └─ [In active narration]

Cooldown
  ├─ Timer expires (10 min) → Idle
  └─ [Ignores all new triggers]
```

### DeviceTrackingService Connection States
```
Disconnected
  ├─ ConnectAsync() → Connecting

Connecting
  ├─ OnStartAsync() → Connected
  └─ OnFailure() → Disconnected

Connected
  ├─ Automatic reconnection on disconnect
  ├─ Heartbeat timer active
  ├─ Responds to SignalR messages
  └─ OnConnectionClose() → Disconnected (retry loop)
```

---

## Performance Characteristics

| Operation | Complexity | Impact |
|-----------|-----------|--------|
| Geofence check | O(n log n) | 5-10ms for 100 POIs |
| Haversine calc | O(1) | < 1ms per POI |
| Database query | O(1) | 1-5ms per table scan |
| TTS playback | O(1) | 2-5 sec (async) |
| GPS polling | O(1) | Every 5 seconds |

**Battery Impact:**
- GPS polling: ~100-200 mA (major drain)
- TTS synthesis: ~50-100 mA
- Screen on: ~500+ mA
- **Estimated**: Full-day tour = 40-60% battery

---

## Web Admin Project

### Tech Stack
- Framework: ASP.NET Core 10.0
- Database: Entity Framework Core + SQLite
- UI: Bootstrap 5 + Razor Pages
- Real-Time: SignalR for device tracking

### Key Features
1. **POI Management**: CRUD with multilingual content
2. **Media Upload**: Images and audio files
3. **Language Management**: Check completeness per language
4. **Analytics Dashboard**: Real-time device status, playback logs
5. **Export/Import**: JSON/Excel data exchange

### Database
- Shares SQLite database with mobile app (optional)
- Or separate database with sync mechanism

---

## Testing Strategy

### Recommended Tests

**Unit Tests**
- GeofenceEngine.OnLocationChanged() (debounce, cooldown, priority)
- NarrationEngine state transitions
- LocalizationService language switching
- AuthService token validation

**Integration Tests**
- GPS → Geofence → Narration full flow
- Database upsert with concurrent access
- SyncService POI sync and audio download

**Manual Tests**
- GPS tracking in real location
- Audio playback with different languages
- Permission requests on different Android versions
- Background tracking (app backgrounded 30+ min)
- Low battery scenarios

---

## Common Issues & Solutions

### Issue: App not tracking GPS in background
**Solution**: 
1. Ensure `LocationAlways` permission granted (Android 12+)
2. Check LocationForegroundService is running
3. Verify app not in battery saver mode
4. Check system location services enabled

### Issue: TTS not speaking
**Solution**:
1. Check device supports requested language
2. Fallback to English if Vietnamese not available
3. Verify volume not muted
4. Check audio focus not taken by other app

### Issue: Geofence not triggering
**Solution**:
1. Check GPS accuracy (should be < 50m)
2. Verify POI is within RadiusMeters
3. Check debounce (30 sec global, 10 min per POI)
4. Verify POI.IsActive = true

### Issue: Sync from server failing
**Solution**:
1. Check internet connection
2. Verify JWT token is valid
3. Check server endpoint is reachable
4. Check DNS resolution
5. Try alternate server URL

---

## Development Workflow

### Local Setup
1. Open `TourMap.slnx` in Visual Studio 2026
2. Set startup project to `TourMap`
3. Target Android API 31+
4. Install dependencies: `dotnet restore`

### Build & Run
```bash
# Debug build for Android
dotnet build -c Debug -f net10.0-android

# Run on connected device/emulator
dotnet run -f net10.0-android
```

### Connection Strings & Config
- **Appsettings**: `appsettings.json` (for admin web)
- **Local DB**: `{AppDataDirectory}/TourMap_v6.db3`
- **Auth URLs**: Fallback list in `AuthService.GetAuthBaseUrls()`
- **Sync URL**: Server base URL in `SyncService.SyncPoisFromServerAsync()`

### Debugging
- Enable debug logging: `#if DEBUG` sections
- Console output: Use `Console.WriteLine()` (appears in Debug Output)
- Log file: Create in `{AppDataDirectory}/logs/`
- Remote debugging: Connect to Android device via USB

---

## Deployment Checklist

- [ ] All tests passing
- [ ] Code review completed
- [ ] JWT secret key configured (env var)
- [ ] Server URLs updated (production)
- [ ] Android permissions reviewed
- [ ] Database version bumped
- [ ] APK signed (release build)
- [ ] ProGuard rules configured (obfuscation)
- [ ] Analytics backend ready
- [ ] Admin web deployed

---

## Version History

**v1.0 (April 2026)**
- Initial MAUI implementation
- 6-language support
- GPS + geofencing
- Audio narration (TTS + MP3)
- Admin CMS
- Device tracking via SignalR

**Planned v1.1**
- iOS support
- Push notifications
- Offline maps
- Heatmap visualization
- Advanced analytics

---

**Last Updated**: April 20, 2026  
**For Detailed Analysis**: See ARCHITECTURE_ANALYSIS.md
