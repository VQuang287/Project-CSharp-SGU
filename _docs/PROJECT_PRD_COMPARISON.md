# PROJECT PRD COMPARISON

**Project:** TourMap - Hệ thống Hướng dẫn Du lịch Thông minh  
**PRD Version:** PRD_TourMap.md  
**Generated:** April 7, 2026  
**Comparison By:** Tech Lead + Code Reviewer

---

## 1. TỔNG QUAN

### 1.1 Kết quả đánh giá nhanh

| Module | Trạng thái | % Hoàn thành | Ghi chú |
|--------|-----------|--------------|---------|
| **Mobile App** | 🟡 In Progress | ~85% | Core features done, UI polish needed |
| **CMS Web Admin** | 🟡 In Progress | ~70% | CRUD done, AI features added (beyond PRD) |
| **Analytics** | 🟡 Partial | ~50% | Controller exists, views limited |
| **Localization** | ✅ Complete | ~95% | 6 languages implemented |

### 1.2 Tech Stack Verification

| PRD Requirement | Actual Implementation | Status |
|----------------|----------------------|--------|
| .NET 10.0 MAUI | ✅ net10.0-android | ✅ |
| ASP.NET Core 10.0 | ✅ net10.0 | ✅ |
| SQLite (Mobile) | ✅ sqlite-net-pcl | ✅ |
| SQLite (Web) | ✅ EF Core + SQLite | ✅ |
| Mapsui 5.0+ | ✅ Mapsui.Maui 5.0.2 | ✅ |
| Plugin.Maui.Audio 3.0+ | ✅ 4.0.0 | ✅ (newer) |
| Android API 31+ | ✅ API 31-34 | ✅ |

---

## 2. BẢNG ĐỐI CHIẾU CHI TIẾT

### 2.1 MOBILE APP MODULE

#### M-001: Theo dõi vị trí GPS

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| GPS Tracking (FusedLocation) | ✅ Implemented | `GpsTrackingService_Android.cs`, `LocationForegroundService.cs` | Full implementation with foreground service |
| 5s foreground / 10s background | ✅ Implemented | `GpsTrackingService_Android.cs:37-38` | Configurable timer-based |
| Android 12+ permissions | ✅ Implemented | `MainActivity.cs`, `AndroidManifest.xml` | ACCESS_BACKGROUND_LOCATION, FOREGROUND_SERVICE |
| Accuracy 5-10m | ⚠️ Partial | `GpsTrackingService_Android.cs:102-106` | Filters by accuracy but not explicitly set to 5-10m |

#### M-002: Geofencing (Phát hiện POI)

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| Haversine formula | ✅ Implemented | `GeofenceEngine.cs:115-124` | Exact formula as PRD |
| 30-50m radius per POI | ✅ Implemented | `Poi.cs:13`, `GeofenceEngine.cs:67` | Default 50m, customizable per POI |
| 10 min cooldown | ✅ Implemented | `GeofenceEngine.cs:14`, `lines 74-80` | Prevents spam |
| 30s debounce | ✅ Implemented | `GeofenceEngine.cs:13`, `lines 45-47` | Global debounce between triggers |
| Priority system | ✅ Implemented | `GeofenceEngine.cs:68` | OrderByDescending priority |
| Max 3 POIs in range | ✅ Implemented | `GeofenceEngine.cs:16`, `line 60` | Take(MaxPoisInRange) |

#### M-003: Audio Narration đa ngôn ngữ

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| 6 languages support | ✅ Implemented | `LocalizationService.cs` | vi, en, zh, ko, ja, fr |
| MP3 priority | ✅ Implemented | `NarrationEngine.cs:120-158` | Checks AudioLocalPath first |
| TTS fallback | ✅ Implemented | `NarrationEngine.cs:159-187`, `TtsService_Android.cs` | TTS script from DB |
| Default Vietnamese | ✅ Implemented | `NarrationEngine.cs:189-203` | Fallback chain ends with vi |
| Playback speed control | ✅ Implemented (EXTRA) | `AudioPlayerService.cs:17-29` | 0.75x, 1x, 1.25x, 1.5x |
| Audio focus handling | ⚠️ Basic | `NarrationEngine.cs:108-116` | Implemented but not extensively tested |

#### M-004: Map với POI Markers

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| OpenStreetMap (Mapsui) | ✅ Implemented | `MapPage.xaml.cs` | Mapsui 5.0.2 integrated |
| POI markers | ✅ Implemented | `MapPage.xaml.cs:270-320` | Pins with callouts |
| Language-specific labels | ✅ Implemented | `LocalizationService.cs` + `MapPage` | Dynamic based on current language |
| Tap to view POI info | ✅ Implemented | `MapPage.xaml.cs:540-600` | Navigates to PoiDetailPage |
| User location indicator | ✅ Implemented | `MapPage.xaml.cs:200-250` | Blue dot with accuracy circle |
| Search POI on map | ✅ Implemented (EXTRA) | `MapPage.xaml.cs:596-776` | Real-time search with results popup |

#### M-005: Đổi ngôn ngữ runtime

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| 6 languages in Settings | ✅ Implemented | `SettingsPage.cs`, `LocalizationService.cs` | Dropdown with flags |
| Runtime language switch | ✅ Implemented | `LocalizationService.cs:30-40` | No restart needed |
| UI sync | ✅ Implemented | `LocalizationService.LanguageChanged` event | All pages subscribe |
| Audio sync | ✅ Implemented | `NarrationEngine.SetLanguageAsync()` | TTS language updates |

#### M-006: Additional Mobile Features (Beyond PRD)

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| QR Code Scanner | ✅ Implemented | `QrScannerPage.cs` | Full camera integration with torch |
| Offline Packs Management | ✅ Implemented | `OfflinePacksPage.cs` | Download/delete tour packs |
| POI List View | ✅ Implemented | `PoiListPage.xaml.cs` | Alternative to map view |
| Audio speed control | ✅ Implemented | `PoiDetailPage.cs:503-513` | UI + backend |
| First-launch onboarding | ✅ Implemented | `MainPage.xaml.cs:40-80` | Language selection + permissions |
| Splash screen with logo | ✅ Implemented | `SplashPage.cs` | 600ms delay + auto-navigation |

---

### 2.2 CMS WEB ADMIN MODULE

#### W-001: Dashboard tổng quan

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| POI count | ✅ Implemented | `HomeController.cs`, `Views/Home/Index.cshtml` | Stats card displayed |
| Language count | ⚠️ Hardcoded | `Views/Home/Index.cshtml` | Shows "6 Languages" static |
| Completeness status | ❌ Missing | - | Not showing incomplete POIs |
| Quick action links | ✅ Implemented | `Views/Home/Index.cshtml` | Links to POIs, Analytics |

#### W-002: Quản lý POI (CRUD)

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| List View with table | ✅ Implemented | `Views/Pois/Index.cshtml` | Full CRUD table |
| Filter by status | ⚠️ Basic | `Views/Pois/Index.cshtml` | Has search, no completeness filter |
| Create POI | ✅ Implemented | `Views/Pois/Create.cshtml` | Form with validation |
| Edit POI | ✅ Implemented | `Views/Pois/Edit.cshtml` | Full edit with media replace |
| Delete POI | ✅ Implemented | `PoisController.cs:130-145` | With confirmation |
| Multi-language tabs | ❌ Missing | - | Single form, no tabs per language |
| TTS Script editor | ❌ Missing | - | Only Description field |

#### W-003: Language Management

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| 6 languages list | ⚠️ Hardcoded | `Views/Pois/Create.cshtml` | Static list, no dynamic management |
| Completeness check | ❌ Missing | - | No warning for missing translations |
| Export by language | ❌ Missing | - | Not implemented |

#### W-004: Media Upload

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| Image upload (JPG/PNG) | ✅ Implemented | `PoisController.cs:40-47` | Saves to wwwroot/uploads/images |
| Audio upload (MP3) | ✅ Implemented | `PoisController.cs:49-56` | Saves to wwwroot/uploads/audio |
| 2MB/5MB limits | ❌ Missing | - | No size validation |
| Drag & drop | ❌ Missing | - | Not implemented |
| Progress indicator | ❌ Missing | - | Not implemented |

#### W-005: Export dữ liệu

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| Export to JSON | ⚠️ Partial | `Api/PoisController.cs` | API endpoint exists, no UI button |
| Export to CSV/Excel | ❌ Missing | - | Not implemented |
| Manual sync support | ⚠️ Partial | - | API available, no documented sync process |

#### W-006: AI Translation (Beyond PRD)

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| Auto AI Translation | ✅ Implemented | `PoisController.cs:58-74` | Checkbox to auto-translate on create |
| AI TTS Generation | ✅ Implemented | `AITranslationService.cs` | Generates audio files via TTS |
| 5 languages auto | ✅ Implemented | `PoisController.cs:60-73` | en, zh, ko, ja, fr |

---

### 2.3 ANALYTICS MODULE

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| Playback History table | ✅ Implemented | `Models/PlaybackHistoryEntry.cs` | In Mobile DB |
| PlaybackHistory logging | ✅ Implemented | `NarrationEngine.cs:253-271` | Saves on audio complete |
| Analytics Dashboard View | ⚠️ Partial | `Views/Analytics/Index.cshtml` | Exists but minimal |
| Top POIs chart | ❌ Missing | - | No Chart.js integration |
| Language pie chart | ❌ Missing | - | No Chart.js integration |
| Heatmap with Leaflet | ❌ Missing | - | Controller exists, view not implemented |
| Playback Log View | ❌ Missing | - | Controller exists, view not implemented |
| CSV Export | ❌ Missing | - | Not implemented |
| UserLocationLog | ❌ Missing | - | Table not in DB schema |

---

### 2.4 LOCALIZATION MODULE

| Feature | Trạng thái | File/Module | Ghi chú |
|---------|-----------|-------------|---------|
| 6 .resx files | ❌ Not Used | - | Using LocalizationService instead |
| LocalizationService | ✅ Implemented | `LocalizationService.cs` | Dictionary-based approach (better for MAUI) |
| Runtime language switch | ✅ Implemented | `LocalizationService.cs:30-40` | Fully working |
| 6 locales for TTS | ✅ Implemented | `TtsService_Android.cs:65-74` | Java Locale for each language |
| JSON columns for content | ⚠️ Different | `Poi.cs:24-42` | Flat properties instead of JSON columns |

---

## 3. DATA MODEL COMPARISON

### 3.1 Mobile Poi Model

| PRD Spec | Actual Implementation | Status |
|----------|----------------------|--------|
| `TitleJson` (JSON) | Flat `Title`, `DescriptionEn`, etc. | ⚠️ Different but functional |
| `DescriptionJson` (JSON) | Flat `Description`, `DescriptionEn`, etc. | ⚠️ Different but functional |
| `TtsScriptJson` (JSON) | Flat `TtsScriptVi`, `TtsScriptEn`, etc. | ⚠️ Different but functional |
| `AudioPathJson` (JSON) | Flat `AudioUrl`, `AudioUrlEn`, etc. | ⚠️ Different but functional |
| JSON parsing overhead | No JSON parsing needed | ✅ Better performance |

**Assessment:** Flattened structure works better for SQLite + MAUI. No JSON serialization overhead.

### 3.2 Web Poi Model

| PRD Spec | Actual Implementation | Status |
|----------|----------------------|--------|
| `Poi` + `PoiTranslation` (EF relationship) | Single `Poi` table with flat columns | ⚠️ Different |
| Navigation property `Translations` | Not used | ❌ Missing |

**Assessment:** Web uses same flattened model as Mobile. Simpler but less normalized.

---

## 4. FEATURE CÒN THIẾU (MISSING)

### 4.1 Mobile App

| # | Feature | Mức độ ưu tiên | Ghi chú |
|---|---------|---------------|---------|
| 1 | Analytics data upload to server | Medium | PlaybackHistory chỉ ở local |
| 2 | UserLocationLog for heatmap | Low | PRD optional |
| 3 | Real-time sync with CMS | Low | PRD marked as "future" |

### 4.2 CMS Web Admin

| # | Feature | Mức độ ưu tiên | Ghi chú |
|---|---------|---------------|---------|
| 1 | Multi-language tabs in Edit POI | High | Current form too long |
| 2 | TTS Script editor | High | Needed for content management |
| 3 | Completeness check/warning | Medium | Dashboard needs this |
| 4 | File size validation (2MB/5MB) | Medium | Security requirement |
| 5 | Drag & drop upload | Low | Nice to have |
| 6 | Progress indicator | Low | UX improvement |

### 4.3 Analytics Dashboard

| # | Feature | Mức độ ưu tiên | Ghi chú |
|---|---------|---------------|---------|
| 1 | Chart.js integration | High | Core requirement |
| 2 | Top POIs bar chart | High | Stats card placeholder only |
| 3 | Language pie chart | Medium | PRD requirement |
| 4 | Line chart (7/30 days) | Medium | PRD requirement |
| 5 | Heatmap with Leaflet | Low | PRD optional, complex |
| 6 | Playback Log view | Medium | Data exists, no UI |
| 7 | CSV export | Low | PRD optional |

---

## 5. FEATURE CHƯA HOÀN CHỈNH (INCOMPLETE)

### 5.1 Mobile App

| Feature | Vấn đề | Cách fix |
|---------|--------|----------|
| Android build | MSB6006 javac.exe memory error | Giảm Java heap, tăng page file |
| Geofence testing | Chưa test trên device thật | Test GPS tracking accuracy |
| Audio focus | Basic implementation only | Test với incoming calls |
| Background GPS | Service exists, cần test durability | Test 10+ phút background |

### 5.2 CMS Web Admin

| Feature | Vấn đề | Cách fix |
|---------|--------|----------|
| POI Edit form | Quá dài, không có tabs | Chia tab per language |
| AI Translation | Chỉ chạy on create | Thêm button "Regenerate" on edit |
| Dashboard | Completeness info missing | Query POIs thiếu translation |

### 5.3 Analytics

| Feature | Vấn đề | Cách fix |
|---------|--------|----------|
| Views | Chỉ có placeholder | Implement Chart.js views |
| Data flow | Mobile không gửi analytics | Thêm API endpoint để nhận logs |

---

## 6. ĐỀ XUẤT TIẾP THEO (PRIORITY)

### 6.1 Immediate (Week 11)

| # | Task | Effort | Impact |
|---|------|--------|--------|
| 1 | **Fix Mobile build error** | 2h | 🔴 Critical |
| 2 | **Chart.js integration for Analytics** | 4h | 🔴 High |
| 3 | Multi-language tabs in CMS Edit POI | 3h | 🟡 Medium |
| 4 | Completeness check in CMS Dashboard | 2h | 🟡 Medium |

### 6.2 Short-term (Before submission)

| # | Task | Effort | Impact |
|---|------|--------|--------|
| 1 | Test end-to-end on real Android device | 4h | 🔴 Critical |
| 2 | TTS Script editor in CMS | 3h | 🟡 Medium |
| 3 | Playback Log view in Analytics | 2h | 🟡 Medium |
| 4 | UI Polish (Mobile + Web) | 4h | 🟢 Low |
| 5 | Demo video recording | 2h | 🔴 Critical |

### 6.3 Optional (If time permits)

| # | Task | Effort | Impact |
|---|------|--------|--------|
| 1 | Heatmap with Leaflet.js | 6h | 🟢 Low |
| 2 | CSV Export for Analytics | 1h | 🟢 Low |
| 3 | File upload size validation | 1h | 🟡 Medium |
| 4 | Drag & drop file upload | 2h | 🟢 Low |

---

## 7. TỔNG KẾT ĐÁNH GIÁ

### 7.1 Điểm mạnh (Strengths)

1. **Mobile App rất mạnh** - 85% complete, nhiều features beyond PRD (QR, Search, Speed control)
2. **AI Integration** - Auto-translation và TTS generation vượt yêu cầu PRD
3. **Localization hoàn chỉnh** - 6 languages đầy đủ trên Mobile
4. **Architecture tốt** - Service-based, DI properly configured
5. **Code quality** - Clean, documented, follows patterns

### 7.2 Điểm yếu (Weaknesses)

1. **Analytics còn thiếu** - Chỉ 50% complete, cần Chart.js integration
2. **CMS UI chưa optimized** - Edit form quá dài, cần tabs
3. **Build issues** - Java heap memory error cần fix
4. **Testing** - Chưa test trên device thật

### 7.3 Khuyến nghị cuối cùng

**Để hoàn thành đồ án đúng hạn (Week 12):**

1. ✅ **Fix build error ngay** - Mobile không build được là blocker lớn nhất
2. ✅ **Tập trung vào Analytics Dashboard** - Đây là module còn thiếu nhiều nhất
3. ✅ **CMS multi-language tabs** - Critical cho demo
4. ⚠️ **Heatmap có thể bỏ qua** - PRD đánh dấu là optional
5. ⚠️ **Real-time sync bỏ qua** - PRD đã đưa vào "future"

**Overall Project Health:** 🟡 **GOOD** - ~75% complete, fixable within remaining time.

---

## APPENDIX: File Structure Comparison

### PRD vs Actual

| PRD Structure | Actual Structure | Note |
|-------------|-----------------|------|
| `Resources/Strings/*.resx` | `Services/LocalizationService.cs` | Better approach for MAUI |
| `Resources/Audio/poi_001/` | Flat wwwroot/uploads/audio | Simpler structure |
| `Services/TtsService_Android.cs` | ✅ Exact match | |
| `ViewModels/` | ✅ Exists | `MainViewModel.cs` only |
| `TourMap.AdminWeb/Analytics/` | `Views/Analytics/` | Chung project (PRD khuyến nghị) |

---

**End of Comparison Report**
**Generated:** April 7, 2026
