using SQLite;
using TourMap.Models;

namespace TourMap.Services;

public class DatabaseService : IDisposable
{
    private const string DatabaseFileName = "TourMap_v7.db3";
    private const string SecretKeyPreferenceKey = "db_secret_key";
    private const string SeedPoiHoangDieuId = "seed-hoang-dieu";
    private const string SeedPoiOcOanhId = "seed-oc-oanh";
    private const string SeedPoiOtXiemId = "seed-ot-xiem";
    private const string SeedPoiTonDanId = "seed-ton-dan";
    private const double PoiCoordinateDedupTolerance = 0.00008; // ~9 meters

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;
    private List<Poi>? _inMemoryFallbackPois;

    private bool _isDbInitialized;

    public DatabaseService()
    {
    }

    private async Task InitAsync()
    {
        if (_isDbInitialized)
            return;

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
        string? secretKey = null;
        SQLiteConnectionString? options = null;

        await _initLock.WaitAsync();
        try
        {
            if (_isDbInitialized) // Double-check after acquiring lock
                return;

            Console.WriteLine("[Database] Bắt đầu khởi tạo database...");
            
            // Xử lý nạp Secret key động từ SecureStorage thay vì Hardcode
            secretKey = await SecureStorage.Default.GetAsync(SecretKeyPreferenceKey);
            if (string.IsNullOrEmpty(secretKey))
            {
                secretKey = Guid.NewGuid().ToString("N");
                await SecureStorage.Default.SetAsync(SecretKeyPreferenceKey, secretKey);
            }

            options = new SQLiteConnectionString(databasePath, true, key: secretKey);
            
            // Đã chuyển thư viện sang sqlite-net-sqlcipher trong .csproj
            _db = new SQLiteAsyncConnection(options);
            await TryEnableWalModeAsync();

            // Tạo bảng nếu chưa có
            Console.WriteLine("[Database] Đang tạo bảng...");
            await _db.CreateTableAsync<Poi>();
            await _db.CreateTableAsync<PlaybackHistoryEntry>();
            await _db.CreateTableAsync<Tour>();
            await _db.CreateTableAsync<TourPoiMapping>();
            await EnsureSeedDataAsync();
            await NormalizePoiDatasetAsync();

            _isDbInitialized = true;
            Console.WriteLine("[Database] Khởi tạo thành công!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Lỗi khởi tạo SQLite: {ex.Message}");
            Console.WriteLine($"[Database] Init exception type: {ex.GetType().Name}");

            // SAFE RECOVERY: Retry với exponential backoff thay vì xóa ngay
            const int maxRetries = 3;
            bool recoverySuccess = false;
            
            for (int retry = 1; retry <= maxRetries && !recoverySuccess; retry++)
            {
                try
                {
                    Console.WriteLine($"[Database] Recovery attempt {retry}/{maxRetries}...");
                    await Task.Delay(retry * 100); // Exponential backoff
                    
                    if (_db != null)
                    {
                        try { await _db.CloseAsync(); }
                        catch { }
                        _db = null;
                    }

                    // Chỉ xóa file khi retry lần cuối và thực sự là lỗi corrupted
                    if (retry == maxRetries && IsDbCorrupted(ex))
                    {
                        Console.WriteLine("[Database] DB appears corrupted after retries. Creating new DB...");
                        DeleteDbFiles(databasePath);
                        secretKey = Guid.NewGuid().ToString("N");
                        await SecureStorage.Default.SetAsync(SecretKeyPreferenceKey, secretKey);
                    }
                    
                    options = new SQLiteConnectionString(databasePath, true, key: secretKey);
                    _db = new SQLiteAsyncConnection(options);
                    await TryEnableWalModeAsync();
                    await _db.CreateTableAsync<Poi>();
                    await _db.CreateTableAsync<PlaybackHistoryEntry>();
                    await EnsureSeedDataAsync();
                    await NormalizePoiDatasetAsync();

                    _isDbInitialized = true;
                    recoverySuccess = true;
                    Console.WriteLine($"[Database] Recovery successful after {retry} attempt(s).");
                }
                catch (Exception retryEx)
                {
                    Console.WriteLine($"[Database] Recovery attempt {retry} failed: {retryEx.Message}");
                    if (retry == maxRetries)
                    {
                        Console.WriteLine("[Database] All recovery attempts exhausted. Falling back to in-memory mode.");
                        _db = null;
                        EnableInMemoryFallback();
                    }
                }
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Kiểm tra xem exception có phải do DB corrupted không
    /// </summary>
    private static bool IsDbCorrupted(Exception ex)
    {
        var corruptedIndicators = new[]
        {
            "file is not a database",
            "malformed database",
            "database disk image is malformed",
            "not a database",
            "SQLITE_CORRUPT",
            "SQLITE_NOTADB",
            "encryption key incorrect"
        };

        var message = ex.Message?.ToLowerInvariant() ?? "";
        return corruptedIndicators.Any(indicator => message.Contains(indicator.ToLowerInvariant()));
    }

    /// <summary>
    /// Xóa các file DB một cách an toàn
    /// </summary>
    private static void DeleteDbFiles(string databasePath)
    {
        try
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
            if (File.Exists(databasePath + "-wal"))
                File.Delete(databasePath + "-wal");
            if (File.Exists(databasePath + "-shm"))
                File.Delete(databasePath + "-shm");
            Console.WriteLine("[Database] DB files deleted for recreation.");
        }
        catch (Exception deleteEx)
        {
            Console.WriteLine($"[Database] Error deleting DB files: {deleteEx.Message}");
        }
    }

    private async Task TryEnableWalModeAsync()
    {
        if (_db == null)
            return;

        try
        {
            // PRAGMA journal_mode returns a scalar row; ExecuteScalar avoids provider quirks.
            await _db.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL;");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] WAL unavailable, continue without WAL: {ex.Message}");
        }
    }

    private void EnableInMemoryFallback()
    {
        _inMemoryFallbackPois = BuildSeedPois();
        _isDbInitialized = true;
        Console.WriteLine("[Database] Fallback mode enabled: using in-memory seed POIs.");
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        await InitAsync();

        if (_db == null)
            return (_inMemoryFallbackPois ??= BuildSeedPois()).ToList();

        try
        {
            var pois = await _db!.Table<Poi>().ToListAsync();
            if (pois.Count == 0)
            {
                await EnsureSeedDataAsync();
                pois = await _db.Table<Poi>().ToListAsync();
            }

            return pois;
        }
        catch (NullReferenceException)
        {
            return (_inMemoryFallbackPois ??= BuildSeedPois()).ToList();
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Busy)
        {
            // Backoff-retry for SQLITE_BUSY
            await Task.Delay(100);
            return await _db!.Table<Poi>().ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] GetPois failed, fallback to memory: {ex.Message}");
            return (_inMemoryFallbackPois ??= BuildSeedPois()).ToList();
        }
    }

    private async Task EnsureSeedDataAsync()
    {
        if (_db == null)
            return;

        var count = await _db.Table<Poi>().CountAsync();
        if (count > 0)
            return;

        await _db.InsertAllAsync(BuildSeedPois());
        Console.WriteLine("[Database] Seeded 4 POIs with multilingual TTS scripts");
    }

    private async Task NormalizePoiDatasetAsync()
    {
        if (_db == null)
            return;

        var all = await _db.Table<Poi>().ToListAsync();
        if (all.Count <= 1)
            return;

        var idsToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pass 1: merge exact logical duplicates (title + rounded coordinates)
        var logicalGroups = all
            .GroupBy(BuildLogicalPoiKey)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in logicalGroups)
        {
            var canonical = group
                .OrderBy(p => IsSeedPoiId(p.Id) ? 1 : 0) // prefer non-seed rows
                .ThenByDescending(p => p.UpdatedAt)
                .First();

            foreach (var duplicate in group)
            {
                if (!string.Equals(duplicate.Id, canonical.Id, StringComparison.OrdinalIgnoreCase))
                {
                    idsToDelete.Add(duplicate.Id);
                }
            }
        }

        // Pass 2: if a seed row is at same coordinates with a non-seed row, drop the seed row
        foreach (var seedPoi in all.Where(p => IsSeedPoiId(p.Id)))
        {
            var nonSeedMatch = all.FirstOrDefault(p =>
                !IsSeedPoiId(p.Id)
                && !idsToDelete.Contains(p.Id)
                && AreCoordinatesClose(seedPoi, p));

            if (nonSeedMatch != null)
            {
                idsToDelete.Add(seedPoi.Id);
            }
        }

        if (idsToDelete.Count == 0)
            return;

        foreach (var id in idsToDelete)
        {
            await _db.DeleteAsync<Poi>(id);
        }

        Console.WriteLine($"[Database] Deduplicated POIs: removed {idsToDelete.Count} duplicate rows.");
    }

    private static bool IsSeedPoiId(string? id)
    {
        return !string.IsNullOrWhiteSpace(id)
            && id.StartsWith("seed-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreCoordinatesClose(Poi a, Poi b)
    {
        return Math.Abs(a.Latitude - b.Latitude) <= PoiCoordinateDedupTolerance
               && Math.Abs(a.Longitude - b.Longitude) <= PoiCoordinateDedupTolerance;
    }

    private static string BuildLogicalPoiKey(Poi poi)
    {
        var normalizedTitle = NormalizeTitle(poi.Title);
        var roundedLat = Math.Round(poi.Latitude, 5);
        var roundedLon = Math.Round(poi.Longitude, 5);
        return $"{normalizedTitle}|{roundedLat}|{roundedLon}";
    }

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return string.Concat(title.Trim().ToLowerInvariant().Where(c => !char.IsWhiteSpace(c)));
    }

    private static List<Poi> BuildSeedPois()
    {
        return
        [
            new Poi
            {
                Id = SeedPoiHoangDieuId,
                Title = "Ngã 4 Hoàng Diệu",
                Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh, điểm khởi đầu của tuyến ẩm thực nổi tiếng Quận 4.",
                Latitude = 10.7618898,
                Longitude = 106.7020039,
                RadiusMeters = 30,
                Priority = 1,
                TtsScriptVi = "Ngã tư Hoàng Diệu Vĩnh Khánh. Đây là điểm khởi đầu của tuyến ẩm thực nổi tiếng Quận 4. Bạn sẽ tìm thấy vô vàn món ngon từ các quán vỉa hè đến nhà hàng sang trọng.",
                TtsScriptEn = "Hoang Dieu and Vinh Khanh intersection. This is the starting point of the famous District 4 food street. You'll find countless delicious dishes from street food stalls to upscale restaurants.",
                TtsScriptZh = "黄迪文与永庆路口。这是第四郡著名美食街的起点。您会发现无数美味佳肴，从街边小摊到高档餐厅。",
                TtsScriptKo = "황디에우와 빈칸 교차로. 이것은 4구의 유명한 음식거리의 시작점입니다. 길거리 음식부터 고급 레스토랑까지 수많은 맛있는 요리를 찾을 수 있습니다.",
                TtsScriptJa = "ホアンディエウとヴィンカンの交差点。これは4区の有名な美食街の出発点です。屋台から高級レストランまで、数え切れないほどの美味しい料理が見つかります。",
                TtsScriptFr = "Intersection Hoang Dieu et Vinh Khanh. C'est le point de départ de la célèbre rue gastronomique du 4ème arrondissement. Vous trouverez d'innombrables plats délicieux, des étals de rue aux restaurants haut de gamme."
            },
            new Poi
            {
                Id = SeedPoiOcOanhId,
                Title = "Ốc Oanh",
                Description = "Trọng điểm phố ẩm thực Vĩnh Khánh, nổi tiếng với các món ốc đặc sản Sài Gòn.",
                Latitude = 10.7608247,
                Longitude = 106.7034143,
                RadiusMeters = 40,
                Priority = 10,
                TtsScriptVi = "Ốc Oanh. Đây là một trong những quán ốc nổi tiếng nhất phố Vĩnh Khánh. Quán phục vụ đa dạng các món ốc tươi ngon được chế biến theo nhiều cách khác nhau.",
                TtsScriptEn = "Oc Oanh restaurant. This is one of the most famous seafood restaurants on Vinh Khanh Street. They serve a variety of fresh shellfish prepared in many different styles.",
                TtsScriptZh = "阿文海鲜店。这是永庆街上最著名的海鲜餐厅之一。他们提供各种新鲜贝类，以多种不同风格烹制。",
                TtsScriptKo = "옥오안 식당. 이것은 빈칸 거리에서 가장 유명한 해산물 레스토랑 중 하나입니다. 다양한 스타일로 준비된 신선한 조개류를 제공합니다.",
                TtsScriptJa = "オックオアン食堂。これはヴィンカン通りで最も有名な海鮮料理店の一つです。様々なスタイルで調理された新鮮な貝類を提供しています。",
                TtsScriptFr = "Restaurant Oc Oanh. C'est l'un des restaurants de fruits de mer les plus célèbres de la rue Vinh Khanh. Ils servent une variété de coquillages frais préparés selon de nombreux styles différents."
            },
            new Poi
            {
                Id = SeedPoiOtXiemId,
                Title = "Ớt Xiêm Quán",
                Description = "Quán ăn nổi tiếng với hương vị cay nồng đặc trưng, thu hút đông đảo thực khách.",
                Latitude = 10.7611784,
                Longitude = 106.705375,
                RadiusMeters = 30,
                Priority = 5,
                TtsScriptVi = "Ớt Xiêm Quán. Quán ăn nổi tiếng với hương vị cay nồng đặc trưng từ ớt xiêm đảo Phú Quốc. Món ngon nhất là lẩu hải sản cay với nước lèo đậm đà.",
                TtsScriptEn = "Ot Xiem restaurant. Famous for its distinctive spicy flavor from Phu Quoc island peppers. The best dish is spicy seafood hotpot with rich broth.",
                TtsScriptZh = "阿仙餐厅。以富国岛辣椒的独特辛辣风味而闻名。最好的菜是辣海鲜火锅，汤底浓郁。",
                TtsScriptKo = "옫씨엠 식당. 푸꾸옥 섬 고추의 독특한 매운 맛으로 유명합니다. 가장 좋은 요리는 진한 국물의 매운 해산물 전골입니다.",
                TtsScriptJa = "オットシエム食堂。フーコック島の唐辛子の特徴的な辛さで有名です。最高の料理は濃厚な出汁の辛い海鮮鍋です。",
                TtsScriptFr = "Restaurant Ot Xiem. Célèbre pour sa saveur épicée distinctive des poivrons de l'île de Phu Quoc. Le meilleur plat est le pot-au-feu de fruits de mer épicé avec un bouillon riche."
            },
            new Poi
            {
                Id = SeedPoiTonDanId,
                Title = "Ngã 3 Tôn Đản",
                Description = "Giao lộ Tôn Đản - Vĩnh Khánh, nơi tập trung nhiều quán ăn vặt đêm.",
                Latitude = 10.760456,
                Longitude = 106.707236,
                RadiusMeters = 50,
                Priority = 2,
                TtsScriptVi = "Ngã ba Tôn Đản Vĩnh Khánh. Đây là nơi tập trung nhiều quán ăn vặt đêm nổi tiếng. Đặc biệt nổi tiếng là các món nướng và lẩu đêm.",
                TtsScriptEn = "Ton Dan and Vinh Khanh three-way intersection. This area is known for its famous late-night snack stalls. Especially popular are grilled dishes and late-night hotpot.",
                TtsScriptZh = "Tôn Đản与永庆三岔路口。这个地区以著名的深夜小吃摊而闻名。特别受欢迎的是烧烤和深夜火锅。",
                TtsScriptKo = "톤단과 빈칸 삼거리. 이 지역은 유명한 야간 간식摊으로 알려져 있습니다. 특히 인기 있는 것은 구운 요리와 심야 전골입니다.",
                TtsScriptJa = "トンダンとヴィンカンの三叉路。この地域は有名な深夜の屋台で知られています。特に人気があるのは焼き料理と深夜の鍋料理です。",
                TtsScriptFr = "Intersection à trois voies Ton Dan et Vinh Khanh. Ce quartier est connu pour ses célèbres étals de snacks nocturnes. Les plats grillés et le pot-au-feu nocturne sont particulièrement populaires."
            }
        ];
    }

    /// <summary>Lấy 1 POI theo ID.</summary>
    public async Task<Poi?> GetPoiByIdAsync(string id)
    {
        await InitAsync();

        if (_db == null)
            return (_inMemoryFallbackPois ??= BuildSeedPois()).FirstOrDefault(p => p.Id == id);

        try
        {
            return await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Busy)
        {
            await Task.Delay(100);
            return await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] GetPoiById fallback: {ex.Message}");
            return (_inMemoryFallbackPois ??= BuildSeedPois()).FirstOrDefault(p => p.Id == id);
        }
    }

    /// <summary>Upsert: cập nhật nếu đã có, thêm mới nếu chưa có. Thread-safe write.</summary>
    public async Task UpsertPoiAsync(Poi poi)
    {
        await InitAsync();

        if (_db == null)
        {
            var fallback = _inMemoryFallbackPois ??= BuildSeedPois();
            var idx = fallback.FindIndex(p => p.Id == poi.Id);
            if (idx >= 0)
                fallback[idx] = poi;
            else
                fallback.Add(poi);
            return;
        }

        await _writeLock.WaitAsync();
        try
        {
            var existing = await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == poi.Id);
            if (existing != null)
            {
                await _db.UpdateAsync(poi);
                return;
            }

            var all = await _db.Table<Poi>().ToListAsync();

            // Replace seed POI if a server POI arrives at the same coordinates.
            if (!IsSeedPoiId(poi.Id))
            {
                var seedDuplicate = all.FirstOrDefault(p => IsSeedPoiId(p.Id) && AreCoordinatesClose(p, poi));
                if (seedDuplicate != null)
                {
                    await _db.DeleteAsync<Poi>(seedDuplicate.Id);
                    all.RemoveAll(p => string.Equals(p.Id, seedDuplicate.Id, StringComparison.OrdinalIgnoreCase));
                }
            }

            // If an equivalent logical POI already exists, update that row instead of inserting a duplicate.
            var logicalDuplicate = all.FirstOrDefault(p => BuildLogicalPoiKey(p) == BuildLogicalPoiKey(poi));
            if (logicalDuplicate != null)
            {
                poi.Id = logicalDuplicate.Id;
                await _db.UpdateAsync(poi);
                return;
            }

            await _db.InsertAsync(poi);
        }
        finally { _writeLock.Release(); }
    }

    public async Task AddPlaybackHistoryAsync(PlaybackHistoryEntry history)
    {
        await InitAsync();
        if (_db == null)
            return;

        await _writeLock.WaitAsync();
        try { await _db!.InsertAsync(history); }
        finally { _writeLock.Release(); }
    }

    public async Task<List<PlaybackHistoryEntry>> GetPlaybackHistoryAsync()
    {
        await InitAsync();
        if (_db == null)
            return new List<PlaybackHistoryEntry>();

        return await _db!.Table<PlaybackHistoryEntry>()
            .OrderByDescending(x => x.PlayedAtUtc)
            .ToListAsync();
    }
    
    /// <summary>Cập nhật TTS script cho POI theo ngôn ngữ. Thread-safe write.</summary>
    public async Task UpdatePoiTtsScriptAsync(string poiId, string languageCode, string ttsScript)
    {
        await InitAsync();

        if (_db == null)
        {
            var fallbackPoi = (_inMemoryFallbackPois ??= BuildSeedPois()).FirstOrDefault(p => p.Id == poiId);
            if (fallbackPoi == null) return;

            switch (languageCode.ToLower())
            {
                case "vi": fallbackPoi.TtsScriptVi = ttsScript; break;
                case "en": fallbackPoi.TtsScriptEn = ttsScript; break;
                case "zh": fallbackPoi.TtsScriptZh = ttsScript; break;
                case "ko": fallbackPoi.TtsScriptKo = ttsScript; break;
                case "ja": fallbackPoi.TtsScriptJa = ttsScript; break;
                case "fr": fallbackPoi.TtsScriptFr = ttsScript; break;
            }
            fallbackPoi.UpdatedAt = DateTime.UtcNow;
            return;
        }

        await _writeLock.WaitAsync();
        try
        {
            var poi = await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == poiId);
            if (poi == null) return;
            
            switch (languageCode.ToLower())
            {
                case "vi": poi.TtsScriptVi = ttsScript; break;
                case "en": poi.TtsScriptEn = ttsScript; break;
                case "zh": poi.TtsScriptZh = ttsScript; break;
                case "ko": poi.TtsScriptKo = ttsScript; break;
                case "ja": poi.TtsScriptJa = ttsScript; break;
                case "fr": poi.TtsScriptFr = ttsScript; break;
            }
            
            poi.UpdatedAt = DateTime.UtcNow;
            await _db.UpdateAsync(poi);
        }
        finally { _writeLock.Release(); }
    }
    
    /// <summary>Lấy TTS script cho POI theo ngôn ngữ</summary>
    public async Task<string?> GetPoiTtsScriptAsync(string poiId, string languageCode)
    {
        await InitAsync();

        Poi? poi;
        if (_db == null)
        {
            poi = (_inMemoryFallbackPois ??= BuildSeedPois()).FirstOrDefault(p => p.Id == poiId);
        }
        else
        {
            poi = await _db.Table<Poi>().FirstOrDefaultAsync(p => p.Id == poiId);
        }

        if (poi == null) return null;
        
        return languageCode.ToLower() switch
        {
            "vi" => poi.TtsScriptVi,
            "en" => poi.TtsScriptEn,
            "zh" => poi.TtsScriptZh,
            "ko" => poi.TtsScriptKo,
            "ja" => poi.TtsScriptJa,
            "fr" => poi.TtsScriptFr,
            _ => poi.TtsScriptVi
        };
    }
    
    /// <summary>Cập nhật toàn bộ TTS scripts cho POI. Thread-safe write.</summary>
    public async Task UpdatePoiTtsScriptsAsync(string poiId, Dictionary<string, string> ttsScripts)
    {
        await InitAsync();

        if (_db == null)
        {
            var poiFallback = (_inMemoryFallbackPois ??= BuildSeedPois()).FirstOrDefault(p => p.Id == poiId);
            if (poiFallback == null) return;

            foreach (var (lang, script) in ttsScripts)
            {
                switch (lang.ToLower())
                {
                    case "vi": poiFallback.TtsScriptVi = script; break;
                    case "en": poiFallback.TtsScriptEn = script; break;
                    case "zh": poiFallback.TtsScriptZh = script; break;
                    case "ko": poiFallback.TtsScriptKo = script; break;
                    case "ja": poiFallback.TtsScriptJa = script; break;
                    case "fr": poiFallback.TtsScriptFr = script; break;
                }
            }

            poiFallback.UpdatedAt = DateTime.UtcNow;
            return;
        }

        await _writeLock.WaitAsync();
        try
        {
            var poi = await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == poiId);
            if (poi == null) return;
            
            foreach (var (lang, script) in ttsScripts)
            {
                switch (lang.ToLower())
                {
                    case "vi": poi.TtsScriptVi = script; break;
                    case "en": poi.TtsScriptEn = script; break;
                    case "zh": poi.TtsScriptZh = script; break;
                    case "ko": poi.TtsScriptKo = script; break;
                    case "ja": poi.TtsScriptJa = script; break;
                    case "fr": poi.TtsScriptFr = script; break;
                }
            }
            
            poi.UpdatedAt = DateTime.UtcNow;
            await _db.UpdateAsync(poi);
            Console.WriteLine($"[Database] Updated TTS scripts for POI {poiId}");
        }
        finally { _writeLock.Release(); }
    }

    // ========== TOUR METHODS ==========

    public async Task<List<Tour>> GetActiveToursAsync()
    {
        await InitAsync();
        return await _db!.Table<Tour>().Where(t => t.IsActive).ToListAsync();
    }

    public async Task<Tour?> GetTourByIdAsync(string tourId)
    {
        await InitAsync();
        return await _db!.Table<Tour>().FirstOrDefaultAsync(t => t.Id == tourId);
    }

    public async Task<List<Poi>> GetTourPoisAsync(string tourId)
    {
        await InitAsync();
        
        var mappings = await _db!.Table<TourPoiMapping>()
            .Where(tm => tm.TourId == tourId)
            .OrderBy(tm => tm.OrderIndex)
            .ToListAsync();

        if (mappings.Count == 0) return new List<Poi>();

        var poiIds = mappings.Select(m => m.PoiId).ToList();
        var pois = await _db.Table<Poi>().Where(p => poiIds.Contains(p.Id)).ToListAsync();

        // Order by OrderIndex
        var orderedPois = mappings
            .Join(pois, m => m.PoiId, p => p.Id, (m, p) => new { m.OrderIndex, Poi = p })
            .OrderBy(x => x.OrderIndex)
            .Select(x => x.Poi)
            .ToList();

        return orderedPois;
    }

    public async Task UpsertTourAsync(Tour tour)
    {
        await InitAsync();
        await _writeLock.WaitAsync();
        try
        {
            var existing = await _db!.Table<Tour>().FirstOrDefaultAsync(t => t.Id == tour.Id);
            if (existing == null)
            {
                await _db.InsertAsync(tour);
            }
            else
            {
                await _db.UpdateAsync(tour);
            }
        }
        finally { _writeLock.Release(); }
    }

    public async Task UpsertTourPoiMappingAsync(TourPoiMapping mapping)
    {
        await InitAsync();
        await _writeLock.WaitAsync();
        try
        {
            var existing = await _db!.Table<TourPoiMapping>().FirstOrDefaultAsync(tm =>
                tm.TourId == mapping.TourId && tm.PoiId == mapping.PoiId);
            if (existing == null)
            {
                await _db.InsertAsync(mapping);
            }
            else
            {
                existing.OrderIndex = mapping.OrderIndex;
                await _db.UpdateAsync(existing);
            }
        }
        finally { _writeLock.Release(); }
    }

    // SYS-W02 fix: Deterministic cleanup - an toàn không block thread UI
    public void Dispose()
    {
        if (_disposed) return;
        
        // Tránh sync-over-async deadlock bằng cách chạy trên thread pool với timeout
        if (_db != null)
        {
            try
            {
                // Dùng Task.Run để tránh block UI thread
                Task.Run(async () =>
                {
                    try
                    {
                        var closeTask = _db.CloseAsync();
                        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
                        await Task.WhenAny(closeTask, timeoutTask);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Database] Error closing DB: {ex.Message}");
                    }
                }).Wait(TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Database] Dispose warning: {ex.Message}");
            }
        }
        
        _initLock.Dispose();
        _writeLock.Dispose();
        _disposed = true;
    }
}
