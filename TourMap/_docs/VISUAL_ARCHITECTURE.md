# TOURMAP - VISUAL ARCHITECTURE DIAGRAMS & FLOW CHARTS

## 1. System Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────┐
│                    TOURMAP SYSTEM ARCHITECTURE                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ╔═══════════════════════════════════════════════════════════════╗ │
│  ║              PRESENTATION LAYER (UI/Pages)                   ║ │
│  ╠═══════════════════════════════════════════════════════════════╣ │
│  ║ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐ ║ │
│  ║ │ MapPage  │ │PoiList   │ │Settings  │ │PoiDetailPage    │ ║ │
│  ║ │          │ │          │ │          │ │                  │ ║ │
│  ║ │[Map]     │ │[Scroll]  │ │[Language]│ │[Title/Desc]     │ ║ │
│  ║ │[Markers] │ │[Search]  │ │[Version] │ │[Play Button]    │ ║ │
│  ║ └──────────┘ └──────────┘ └──────────┘ └──────────────────┘ ║ │
│  ║                                                               ║ │
│  ║ Binding: MainViewModel.Pois (ObservableCollection)           ║ │
│  ║ Events: LocationUpdated, StateChanged                        ║ │
│  ╚═══════════════════════════════════════════════════════════════╝ │
│                              ↑                                      │
│                     [Data Binding] [Events]                        │
│                              │                                      │
│  ╔═══════════════════════════════════════════════════════════════╗ │
│  ║           VIEW MODEL LAYER (State Management)                 ║ │
│  ╠═══════════════════════════════════════════════════════════════╣ │
│  ║ ┌──────────────────────────────────────────────────────────┐ ║ │
│  ║ │ MainViewModel                                            │ ║ │
│  ║ ├──────────────────────────────────────────────────────────┤ ║ │
│  ║ │ • Pois: ObservableCollection<Poi>                        │ ║ │
│  ║ │ • SelectedPoi: Poi?                                      │ ║ │
│  ║ │ • LoadAsync()                                            │ ║ │
│  ║ └──────────────────────────────────────────────────────────┘ ║ │
│  ║                                                               ║ │
│  ║ Minimal ViewModel → Could expand with MVVM Toolkit           ║ │
│  ╚═══════════════════════════════════════════════════════════════╝ │
│                              ↓                                      │
│                        [Queries/Commands]                          │
│                              ↓                                      │
│  ╔═══════════════════════════════════════════════════════════════╗ │
│  ║         SERVICE LAYER (Business Logic & Orchestration)        ║ │
│  ╠═══════════════════════════════════════════════════════════════╣ │
│  ║                                                               ║ │
│  ║  ╔════════════════════════════════════════════════════════╗  ║ │
│  ║  ║           CORE ENGINES (Singletons)                   ║  ║ │
│  ║  ╠════════════════════════════════════════════════════════╣  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌──────────────────┐       ┌───────────────────┐    ║  ║ │
│  ║  ║  │  GeofenceEngine  │────→  │  NarrationEngine  │    ║  ║ │
│  ║  ║  ├──────────────────┤       ├───────────────────┤    ║  ║ │
│  ║  ║  │ • Haversine      │       │ • State machine   │    ║  ║ │
│  ║  ║  │ • Debounce       │       │ • Audio priority  │    ║  ║ │
│  ║  ║  │ • Cooldown       │       │ • TTS/MP3 select  │    ║  ║ │
│  ║  ║  │ • Priority order │       │ • Interruption    │    ║  ║ │
│  ║  ║  └──────────────────┘       └───────────────────┘    ║  ║ │
│  ║  ║           ↑ POITriggered              ↓              ║  ║ │
│  ║  ║           │                    StateChanged          ║  ║ │
│  ║  ║           │                                          ║  ║ │
│  ║  ║  ╔────────┴──────────┐                              ║  ║ │
│  ║  ║  │ TourRuntimeService│ [Orchestrator]               ║  ║ │
│  ║  ║  ├───────────────────┤                              ║  ║ │
│  ║  ║  │ • GPS → Geofence  │                              ║  ║ │
│  ║  ║  │ • → Narration     │                              ║  ║ │
│  ║  ║  │ • Lifecycle mgmt  │                              ║  ║ │
│  ║  ║  │ • Event routing   │                              ║  ║ │
│  ║  ║  └───────────────────┘                              ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ╚════════════════════════════════════════════════════════╝  ║ │
│  ║                                                               ║ │
│  ║  ╔════════════════════════════════════════════════════════╗  ║ │
│  ║  ║          SUPPORT SERVICES (Singletons)               ║  ║ │
│  ║  ╠════════════════════════════════════════════════════════╣  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌──────────────┐    ┌──────────────┐               ║  ║ │
│  ║  ║  │ AuthService  │    │ DatabaseServ │               ║  ║ │
│  ║  ║  ├──────────────┤    ├──────────────┤               ║  ║ │
│  ║  ║  │ • JWT tokens │    │ • SQLite ops │               ║  ║ │
│  ║  ║  │ • Login/Reg  │    │ • Thread-safe│               ║  ║ │
│  ║  ║  │ • Token mgmt │    │ • Locks      │               ║  ║ │
│  ║  ║  └──────────────┘    └──────────────┘               ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌──────────────┐    ┌──────────────┐               ║  ║ │
│  ║  ║  │ SyncService  │    │LocalizService│               ║  ║ │
│  ║  ║  ├──────────────┤    ├──────────────┤               ║  ║ │
│  ║  ║  │ • API sync   │    │ • 6 languages│               ║  ║ │
│  ║  ║  │ • Audio DL   │    │ • Runtime SW │               ║  ║ │
│  ║  ║  │ • Incremental│    │ • Persistence│               ║  ║ │
│  ║  ║  └──────────────┘    └──────────────┘               ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌──────────────────┐  ┌──────────────────┐          ║  ║ │
│  ║  ║  │DeviceTracking    │  │AudioPlayerSvc    │          ║  ║ │
│  ║  ║  │Service           │  │                  │          ║  ║ │
│  ║  ║  ├──────────────────┤  ├──────────────────┤          ║  ║ │
│  ║  ║  │ • SignalR hub    │  │ • MP3 playback   │          ║  ║ │
│  ║  ║  │ • Heartbeat      │  │ • Speed control  │          ║  ║ │
│  ║  ║  │ • Real-time      │  │ • Non-blocking   │          ║  ║ │
│  ║  ║  └──────────────────┘  └──────────────────┘          ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ╚════════════════════════════════════════════════════════╝  ║ │
│  ║                                                               ║ │
│  ║  ╔════════════════════════════════════════════════════════╗  ║ │
│  ║  ║      PLATFORM-SPECIFIC SERVICES (Android)             ║  ║ │
│  ║  ╠════════════════════════════════════════════════════════╣  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌────────────────────────────────────────────────┐   ║  ║ │
│  ║  ║  │ GpsTrackingService_Android                     │   ║  ║ │
│  ║  ║  ├────────────────────────────────────────────────┤   ║  ║ │
│  ║  ║  │ • 5-second polling                            │   ║  ║ │
│  ║  ║  │ • Foreground service (background tracking)    │   ║  ║ │
│  ║  ║  │ • Noise filtering (>50m ignored)              │   ║  ║ │
│  ║  ║  │ • Adaptive intervals (3-10 sec)               │   ║  ║ │
│  ║  ║  │ • Permission handling (API 31+ aware)         │   ║  ║ │
│  ║  ║  └────────────────────────────────────────────────┘   ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌────────────────────────────────────────────────┐   ║  ║ │
│  ║  ║  │ TtsService_Android                            │   ║  ║ │
│  ║  ║  ├────────────────────────────────────────────────┤   ║  ║ │
│  ║  ║  │ • Native Android.Speech.Tts engine            │   ║  ║ │
│  ║  ║  │ • 6-language support                          │   ║  ║ │
│  ║  ║  │ • Language fallback (vi → en)                 │   ║  ║ │
│  ║  ║  │ • TaskCompletionSource for async speech       │   ║  ║ │
│  ║  ║  └────────────────────────────────────────────────┘   ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ║  ┌────────────────────────────────────────────────┐   ║  ║ │
│  ║  ║  │ LocationForegroundService                     │   ║  ║ │
│  ║  ║  ├────────────────────────────────────────────────┤   ║  ║ │
│  ║  ║  │ • Keeps GPS alive in background               │   ║  ║ │
│  ║  ║  │ • Persistent notification                     │   ║  ║ │
│  ║  ║  │ • Android 8.0+ channel management             │   ║  ║ │
│  ║  ║  │ • Graceful shutdown on user request           │   ║  ║ │
│  ║  ║  └────────────────────────────────────────────────┘   ║  ║ │
│  ║  ║                                                        ║  ║ │
│  ║  ╚════════════════════════════════════════════════════════╝  ║ │
│  ║                                                               ║ │
│  ╚═══════════════════════════════════════════════════════════════╝ │
│                              ↓                                      │
│                    [CRUD Operations] [Events]                      │
│                              ↓                                      │
│  ╔═══════════════════════════════════════════════════════════════╗ │
│  ║              DATA LAYER (Persistence)                         ║ │
│  ╠═══════════════════════════════════════════════════════════════╣ │
│  ║ ┌──────────────────────────────────────────────────────────┐ ║ │
│  ║ │ DatabaseService (Thread-Safe Wrapper)                   │ ║ │
│  ║ ├──────────────────────────────────────────────────────────┤ ║ │
│  ║ │ ┌───────────────────────────────────────────────────┐   │ ║ │
│  ║ │ │ SQLiteAsyncConnection                            │   │ ║ │
│  ║ │ ├───────────────────────────────────────────────────┤   │ ║ │
│  ║ │ │ Tables:                                           │   │ ║ │
│  ║ │ │  • Poi (multilingual POI data)                    │   │ ║ │
│  ║ │ │  • PlaybackHistoryEntry (analytics)              │   │ ║ │
│  ║ │ │                                                   │   │ ║ │
│  ║ │ │ Database: {AppData}/TourMap_v6.db3               │   │ ║ │
│  ║ │ │ Encoding: WAL mode (concurrent read/write)       │   │ ║ │
│  ║ │ └───────────────────────────────────────────────────┘   │ ║ │
│  ║ └──────────────────────────────────────────────────────────┘ ║ │
│  ║                                                               ║ │
│  ╚═══════════════════════════════════════════════════════════════╝ │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. GPS to Narration Data Flow (Complete Flow)

```
┌─────────────────────────────────────────────────────────────────────┐
│                  GPS DETECTION TO AUDIO PLAYBACK FLOW               │
└─────────────────────────────────────────────────────────────────────┘

                         USER WALKING
                            │
                            ▼
              ┌─────────────────────────┐
              │  Android OS GPS Module  │
              │  (Updates every 5 sec)  │
              └──────────────┬──────────┘
                             │
                             ▼
              ┌─────────────────────────────────────┐
              │ GpsTrackingService_Android          │
              ├─────────────────────────────────────┤
              │ • Poll location every 5 seconds     │
              │ • Filter accuracy > 50m             │
              │ • Detect movement changes          │
              └──────────────┬──────────────────────┘
                             │
                             ▼
              ┌──────────────────────────────────────┐
              │ [EVENT] LocationChanged              │
              │ Payload: Location object (lat, lng)  │
              └──────────────┬───────────────────────┘
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
          ▼                  ▼                  ▼
    [MapPage]        [TourRuntimeService]  [GpsTrackingService]
    Relay to UI            Route event        Continue polling

                             │
                             ▼
              ┌──────────────────────────────────────┐
              │ GeofenceEngine.OnLocationChanged()   │
              ├──────────────────────────────────────┤
              │ Step 1: Check debounce              │
              │ ├─ If < 30 sec since last trigger  │
              │ └─ SKIP (return early)              │
              │                                     │
              │ Step 2: Filter nearby POIs          │
              │ ├─ Calculate Haversine distance     │
              │ ├─ Filter by MaxScanRadius (500m)   │
              │ └─ Take top 3 closest              │
              │                                     │
              │ Step 3: Check per-POI cooldown     │
              │ ├─ Lookup cooldown timestamp       │
              │ ├─ If < 10 min since last trigger  │
              │ └─ SKIP this POI                   │
              │                                     │
              │ Step 4: Select by priority         │
              │ ├─ Among remaining POIs             │
              │ └─ Pick highest Priority value      │
              │                                     │
              │ Step 5: Validate trigger radius    │
              │ ├─ Check distance < Poi.RadiusM   │
              │ └─ If > radius: SKIP               │
              │                                     │
              │ Step 6: Record cooldown timestamp  │
              │ └─ Set _cooldowns[poiId] = now     │
              └──────────────┬──────────────────────┘
                             │
                (If ALL checks pass)
                             │
                             ▼
              ┌──────────────────────────────────────┐
              │ [EVENT] POITriggered(Poi)            │
              │ Payload: Poi object                 │
              └──────────────┬───────────────────────┘
                             │
                             ▼
              ┌──────────────────────────────────────┐
              │ TourRuntimeService.OnPoiTriggered() │
              ├──────────────────────────────────────┤
              │ • Update DeviceTrackingService      │
              │ • Log current POI to analytics      │
              │ • Call narration engine             │
              └──────────────┬───────────────────────┘
                             │
                             ▼
              ┌──────────────────────────────────────┐
              │ NarrationEngine.OnPOITriggeredAsync()│
              ├──────────────────────────────────────┤
              │                                     │
              │ Check Current State:               │
              │ ├─ If PLAYING:                    │
              │ │  ├─ Check: NewPoi.Priority >   │
              │ │  │         CurrentPoi.Priority│
              │ │  ├─ YES: Stop current,        │
              │ │  │       Continue (INTERRUPT) │
              │ │  └─ NO: Return early (IGNORE) │
              │ │                               │
              │ ├─ If COOLDOWN:                 │
              │ │  └─ Return early (IGNORE)     │
              │ │                               │
              │ └─ If IDLE:                     │
              │    └─ Continue (ACCEPT)         │
              │                                 │
              │ State ← Playing                 │
              │ CurrentPoi ← Poi                │
              │                                 │
              └──────────────┬──────────────────┘
                             │
            ┌────────────────┼────────────────┐
            │                │                │
      Check Priority 1   Check Priority 2  Check Priority 3
            │                │                │
            ▼                ▼                ▼
      ┌──────────┐    ┌──────────┐    ┌──────────┐
      │Has TTS   │    │Has Audio │    │Has Desc  │
      │Script    │    │File      │    │Text      │
      │(Offline) │    │(Cached)  │    │(Fallback)│
      └────┬─────┘    └────┬─────┘    └────┬─────┘
           │               │               │
        YES│NO          YES│NO          YES│
         │   │           │   │           │
         ▼   ▼           ▼   ▼           ▼
         │   └──────────→│   └──────────→│
         │               │               │
         ▼               ▼               ▼
      ┌──────────────┐
      │ Audio Source │
      │  Selected!   │
      └──────┬───────┘
             │
             ▼
      ┌────────────────────────────────┐
      │ ITtsService.SpeakAsync()       │
      │   OR                           │
      │ IAudioPlayerService.PlayAsync()│
      └────────────┬───────────────────┘
                   │
                   ▼
          AUDIO PLAYBACK STARTS
                   │
                   ├─ TTS Engine speaking
                   │  text in selected language
                   │
                   └─ OR MP3 file playing
                      from local storage
                   │
                   ▼ (When complete)
      ┌────────────────────────────────┐
      │[EVENT] AudioCompleted          │
      │ or SpeechCompleted             │
      └────────────┬───────────────────┘
                   │
                   ▼
      ┌────────────────────────────────┐
      │ NarrationEngine.                │
      │ OnPlaybackCompleted()           │
      ├────────────────────────────────┤
      │ • Log PlaybackHistoryEntry     │
      │ • State ← Cooldown             │
      │ • Start 10-min cooldown timer  │
      └────────────┬───────────────────┘
                   │
                   ▼
           STATE: COOLDOWN (10 min)
           All new POI triggers IGNORED
                   │
                   ▼ (After 10 minutes)
                   
                 State ← Idle
                   │
                   ▼
       READY FOR NEXT POI TRIGGER
```

---

## 3. Service Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│              SERVICE DEPENDENCY GRAPH                       │
└─────────────────────────────────────────────────────────────┘

                         MauiApp
                            │
         ┌──────────────────┼──────────────────┐
         │                  │                  │
         ▼                  ▼                  ▼
    ┌────────────┐    ┌────────────┐    ┌────────────┐
    │HttpClient  │    │Database    │    │Services    │
    │Factory     │    │Service     │    │Registration│
    └────┬───────┘    └────┬───────┘    └────────────┘
         │                 │
         ▼                 ▼
    ┌────────────────┐    ┌──────────────────────┐
    │ AuthService    │←───│ DatabaseService      │
    │ SyncService    │    │ (Thread-safe SQLite) │
    └────┬───────────┘    └──────────────────────┘
         │
         ▼
    ┌────────────────┐
    │ TourRuntime    │
    │ Service        │
    │ (Orchestrator) │
    └───┬──┬──┬──────┘
        │  │  │
     ┌──┘  │  └──────────┬───────────┐
     │     │             │           │
     ▼     ▼             ▼           ▼
  ┌──────────┐    ┌──────────┐  ┌──────────┐
  │Geofence  │    │Narration │  │Device    │
  │Engine    │    │Engine    │  │Tracking  │
  └──┬───────┘    └──┬───────┘  │Service   │
     │               │          └──────────┘
     │               ├──→ ITtsService
     │               │
     │               └──→ IAudioPlayerService
     │
     └──→ LocationChanged Event
         (from GPS Service)


         GpsTrackingService_Android
                    │
                    ├─→ LocationForegroundService
                    │    (Background GPS tracking)
                    │
                    └─→ LocationChanged Event
                        (5 sec intervals)


         LocalizationService
                    │
                    └─→ Used by All Pages
                        & NarrationEngine
                        (6-language support)


         LoggerService
                    │
                    └─→ All services log
                        to Debug output
```

---

## 4. Page Navigation Flow

```
┌─────────────────────────────────────────────────────────┐
│            NAVIGATION & PAGE FLOW                       │
└─────────────────────────────────────────────────────────┘

                    App Start
                        │
                        ▼
              ┌─────────────────┐
              │ SplashPage      │
              │ [Startup logic] │
              └────────┬────────┘
                       │
                       ├─ AuthService.InitializeAsync()
                       │  ├─ Check SecureStorage token
                       │  └─ Validate/Refresh if needed
                       │
                       ▼
              ┌─────────────────┐         ┌──────────────┐
              │ Is Authenticated?        │              │
              └────┬──────────┬─────┘    │              │
                   │          │         │              │
                 NO│          │YES      │              │
                   │          │         │              │
                   ▼          ▼         │              │
         ┌────────────────┐  ┌──────────┴──────────┐  │
         │ LoginPage      │  │    AppShell        │   │
         │ [email/pwd]    │  │ [Tab Navigation]   │   │
         └───┬──┬─────────┘  └────┬────┬─────┬────┘   │
             │  │                 │    │     │        │
          ON │  │FAIL            │    │     │        │
      LOGIN │  │                 │    │     │        │
             │  ▼              │    │     │        │
             │ RegisterPage    │    │     │        │
             │ [Create acct]   │    │     │        │
             │                 │    │     │        │
             └────────────────→│    │     │        │
                               │    │     │        │
        ┌──────────────────────┘    │     │        │
        │                           ▼     ▼        ▼
        │                    ┌──────────────────────────┐
        └──────────────────→ │ AppShell (Logged In)     │
                              ├──────────────────────────┤
                              │ TabBar with 5 Tabs:      │
                              │                          │
                              │ [Map]─────────────────→ MapPage
                              │ │ [Geofence + GPS]
                              │ │ [Audio Player]
                              │ │ [Real-time tracking]
                              │ │
                              │ [POI List]────────────→ PoiListPage
                              │ │ [Scrollable list]
                              │ │ [Tap → PoiDetailPage]
                              │ │
                              │ [QR Scanner]──────────→ QrScannerPage
                              │ │ [Scan to navigate]
                              │ │
                              │ [Offline Packs]────────→ OfflinePacksPage
                              │ │ [Download content]
                              │ │
                              │ [Settings]────────────→ SettingsPage
                              │ │ [Language switch]
                              │ │ [About]
                              │ │
                              │ [Profile]─────────────→ ProfilePage
                              │                         [User info]
                              │
                              └─ Push Navigation ───→ PoiDetailPage
                                 (from list)            [Full POI view]
                              │
                              └─ Push Navigation ───→ LoginPage
                                 (logout/re-auth)
```

---

## 5. State Management Diagram

### NarrationEngine State Machine

```
┌───────────────────────────────────────────────────────────┐
│        NARRATION ENGINE STATE MACHINE                     │
└───────────────────────────────────────────────────────────┘

                         ┌──────────┐
                         │  [START] │
                         └────┬─────┘
                              │
                              ▼
                    ╔════════════════╗
                    ║      IDLE      ║
                    ║ [Ready to play]║
                    ╚════════┬═══════╝
                             │
                   POITriggered()
                             │
            ┌────────────────┼────────────────┐
            │                │                │
        PLAYING?        COOLDOWN?         IDLE?
            │YES          │YES             │YES
            │             │                │
            ▼             ▼                ▼
       ┌────────┐     ┌────────┐     Continue
       │ Check  │     │ IGNORE │     to PLAYING
       │Priority│     │  POI   │
       └─┬──┬───┘     └────────┘
         │  │
    HIGH │  │LOW
    PRIO │  │PRIO
         │  │
         ▼  ▼
      INTERRUPT IGNORE
      current      new
         │            │
         ▼            │
         └────────────┘
                │
         Continue PLAYING
                │
         ┌──────▼──────┐
         │              │
    ┌────┴─────┐        │
    ▼          ▼        │
PLAYING    COOLDOWN     │
    │        10 min     │
    │          │        │
    │   After timer     │
    │          │        │
    │          ▼        │
    │        IDLE◄──────┘
    │
    │ (Playback completes)
    │
    ▼
╔════════════════╗
║    COOLDOWN    ║ (10 minutes)
║ [Ignores POI]  ║
╚════════┬═══════╝
         │
      Timer
    expires
         │
         ▼
    ╔════════════════╗
    ║      IDLE      ║ ──→ [Ready for next trigger]
    ║ [Ready to play]║
    ╚════════════════╝
```

### DeviceTrackingService Connection States

```
┌───────────────────────────────────────────────────┐
│   DEVICE TRACKING SERVICE CONNECTION STATES       │
└───────────────────────────────────────────────────┘

                   START
                    │
                    ▼
         ╔═════════════════════╗
         ║   DISCONNECTED      ║ [Initial state]
         ║                     ║
         ║ IsConnected = false ║
         ╚════════┬════════════╝
                  │
      ConnectAsync()
                  │
                  ▼
         ╔═════════════════════╗
         ║   CONNECTING       ║
         ║                    ║
         ║ Establishing...    ║
         ╚════════┬════════════╝
                  │
         ┌────────┼────────┐
         │        │        │
      SUCCESS  NETWORK  AUTH
      FAILURE  ERROR    ERROR
         │        │        │
         ▼        ▼        ▼
         │        │        │
         │    RECONNECT    │
         │    (Exponential)│
         │        │        │
         │        └────┬───┘
         │             │
         ├─────────────┘
         │
         ▼
╔══════════════════════╗
║    CONNECTED        ║
║                     ║
║ IsConnected = true  ║
║ Heartbeat active    ║
║ SignalR ready       ║
╚═══════┬══════════════╝
        │
        ├──→ [Send heartbeat every 30sec]
        │
        ├──→ [Send location updates]
        │
        ├──→ [Send narration state]
        │
        └──→ Hub.Closed() / Network down
            │
            ▼
  ╔═════════════════════╗
  ║  AUTO-RECONNECT    ║
  ║ (Exponential wait) ║
  ║  0s → 2s → 10s...  ║
  ╚════════┬════════════╝
           │
           └──→ Back to CONNECTING
                (Loop until success)
```

---

## 6. Thread Safety & Concurrency Model

```
┌───────────────────────────────────────────────────────────┐
│     THREAD SAFETY & CONCURRENCY PATTERNS                  │
└───────────────────────────────────────────────────────────┘

DATABASE SERVICE (DatabaseService.cs)
│
├─ READ OPERATIONS (Lock-Free)
│  ├─ GetPoisAsync()       → Direct table query
│  └─ GetPoiByIdAsync()    → Direct lookup
│
└─ WRITE OPERATIONS (Serialized)
   │
   ├─ Entry: _writeLock.WaitAsync()
   │
   ├─ UpsertPoiAsync()
   │  ├─ Query: Check if exists
   │  ├─ Decision: Update or Insert
   │  └─ Execute
   │
   ├─ AddPlaybackHistoryAsync()
   │  └─ Insert history record
   │
   └─ Exit: _writeLock.Release()
   
   Result: Only ONE write at a time
           Multiple reads concurrent


GEOFENCE ENGINE (GeofenceEngine.cs)
│
├─ Protected Data:
│  ├─ _pois (list of POIs)
│  └─ _cooldowns (dict of timestamps)
│
├─ Protection Mechanism:
│  └─ lock (_lock) { /* atomic operation */ }
│
├─ Read-Side (OnLocationChanged)
│  ├─ Snapshot POI list under lock
│  └─ Release lock → Calculate distances (no lock)
│
└─ Write-Side (UpdatePois, cooldown tracking)
   └─ Exclusive access under lock


GPS TRACKING SERVICE (GpsTrackingService_Android.cs)
│
├─ Flag: _isTrackingFlag (int)
│  └─ Protected by Interlocked.CompareExchange()
│
├─ Thread-Safe Check:
│  │
│  if (Interlocked.CompareExchange(ref _isTrackingFlag, 1, 0) == 1)
│      return; // Already tracking
│
└─ Result: Only ONE tracking loop at a time


NARRATION ENGINE (NarrationEngine.cs)
│
├─ Current State: _state (NarrationState)
│  └─ NOT protected (single-threaded UI assumption)
│
├─ Blocking Operations:
│  ├─ TTS Speak (async, waits for completion)
│  ├─ Audio Play (async, waits for completion)
│  └─ Cooldown Timer (async Task)
│
└─ Thread Model: All operations on MainThread
   (via MainThread.BeginInvokeOnMainThread)


AUDIO PLAYER SERVICE (AudioPlayerService.cs)
│
├─ Player: IAudioPlayer (single instance)
│  └─ Stop() clears previous playback
│
├─ Stream: FileStream
│  └─ Properly disposed in finally block
│
└─ Async Model:
   ├─ PlayAsync() returns when playback completes
   ├─ Uses TaskCompletionSource for non-blocking wait
   └─ PlaybackEnded event triggers completion


TTS SERVICE (TtsService_Android.cs)
│
├─ Android TTS Engine: Single instance
│  └─ Not thread-safe (use from UI thread only)
│
├─ Async Speaking:
│  ├─ SpeakAsync() returns when speech completes
│  ├─ Uses OnUtteranceProgressListener callback
│  └─ TaskCompletionSource for async handling
│
└─ Thread Model: All TTS calls from MainThread
```

---

## 7. Error Handling Flow

```
┌──────────────────────────────────────────────────┐
│     ERROR HANDLING & RECOVERY PATTERNS            │
└──────────────────────────────────────────────────┘

AUTHENTICATION ERROR (AuthService)
│
├─ HttpRequestException (network)
│  ├─ Try multiple server URLs (failover)
│  └─ If all fail: "Không thể kết nối đến server"
│
├─ Invalid credentials (401)
│  ├─ Return: AuthResult(false, "Email hoặc mật khẩu không đúng")
│  └─ Clear tokens
│
├─ Token expired
│  ├─ Attempt RefreshTokenAsync()
│  ├─ If success: Continue
│  └─ If fail: Clear tokens → Redirect to LoginPage
│
└─ Timeout (TaskCanceledException)
   └─ "Request timeout" → Retry


SYNC ERROR (SyncService)
│
├─ Network error (no internet)
│  ├─ Log error
│  ├─ Return false
│  └─ App continues with cached data (offline mode)
│
├─ Audio download fails
│  ├─ Log "Failed to download audio for POI X"
│  ├─ Skip audio download
│  └─ Continue POI sync (TTS fallback available)
│
├─ Invalid JSON from server
│  ├─ Log parsing error
│  └─ Skip malformed POI
│
├─ 401 Unauthorized
│  ├─ Refresh token
│  ├─ Retry request
│  └─ If still fails: Redirect to login
│
└─ Timeout (20 sec)
   ├─ Log "Sync timeout"
   └─ Return false


GEOFENCE ERROR (GeofenceEngine)
│
├─ Invalid coordinates
│  ├─ Filter out POI with lat/lng out of range
│  └─ Log warning
│
├─ Massive POI list (1000+)
│  ├─ O(n) calculation may be slow
│  └─ Log performance warning
│
└─ No POIs loaded
   └─ Skip trigger (safe)


NARRATION ERROR (NarrationEngine)
│
├─ TTS not available
│  ├─ Fall back to AudioFile
│  ├─ Fall back to Description TTS
│  └─ Log warning
│
├─ Audio file not cached
│  ├─ Try download from server
│  ├─ If fails: Use TTS
│  └─ Log warning
│
├─ TTS speech fails
│  ├─ Catch exception
│  ├─ Log error with stack trace
│  └─ Fire AudioCompleted anyway (prevent hang)
│
├─ POI data is null/invalid
│  ├─ Catch ArgumentNullException
│  ├─ Log POI details
│  └─ Skip playback
│
└─ User interrupts (cancellation)
   ├─ Catch TaskCanceledException
   └─ Gracefully stop


GPS TRACKING ERROR (GpsTrackingService_Android)
│
├─ Permission denied
│  ├─ Log "Permission denied"
│  ├─ Stop tracking
│  └─ Show UI prompt
│
├─ Location service disabled
│  ├─ Log warning
│  ├─ Return null location
│  └─ Try again next poll
│
├─ Poor GPS accuracy (>50m)
│  ├─ Filter out (ignore reading)
│  └─ Continue polling
│
└─ Foreground service fails
   ├─ Log error
   └─ Continue tracking (no background service)


RECOVERY STRATEGIES
│
├─ Offline-First
│  ├─ Always load from local cache first
│  └─ Sync updates when network available
│
├─ Graceful Degradation
│  ├─ Audio file missing? Use TTS
│  ├─ TTS unavailable? Use text
│  └─ Network down? Use cache
│
├─ Auto-Retry with Backoff
│  ├─ Network request fails
│  ├─ Wait 2s → Retry
│  └─ Exponential backoff for persistence
│
├─ Event-Driven Async
│  ├─ Use TaskCompletionSource for timeouts
│  ├─ Prevent UI thread blocking
│  └─ Always fire completion event
│
└─ Logging
   ├─ Log all errors to console
   ├─ Include full exception + stack trace
   └─ (Future: Remote error logging)
```

---

**End of Visual Architecture Diagrams**
