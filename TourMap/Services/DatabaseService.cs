using SQLite;
using TourMap.Models;

namespace TourMap.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _db;

    public DatabaseService()
    {
    }

    private async Task InitAsync()
    {
        if (_db is not null)
            return;

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TourMap_v6.db3");
        _db = new SQLiteAsyncConnection(databasePath);

        // Tạo bảng nếu chưa có
        await _db.CreateTableAsync<Poi>();
        await _db.CreateTableAsync<PlaybackHistoryEntry>();

        // Chèn dữ liệu mẫu nếu chưa có (fallback khi chưa sync server)
        var count = await _db.Table<Poi>().CountAsync();
        if (count == 0)
        {
            var seedData = new List<Poi>
            {
                new Poi { 
                    Title = "Ngã 4 Hoàng Diệu", 
                    Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh, điểm khởi đầu của tuyến ẩm thực nổi tiếng Quận 4.", 
                    Latitude = 10.7618898, Longitude = 106.7020039, RadiusMeters = 30, Priority = 1,
                    // TTS scripts for demonstration
                    TtsScriptVi = "Ngã tư Hoàng Diệu Vĩnh Khánh. Đây là điểm khởi đầu của tuyến ẩm thực nổi tiếng Quận 4. Bạn sẽ tìm thấy vô vàn món ngon từ các quán vỉa hè đến nhà hàng sang trọng.",
                    TtsScriptEn = "Hoang Dieu and Vinh Khanh intersection. This is the starting point of the famous District 4 food street. You'll find countless delicious dishes from street food stalls to upscale restaurants.",
                    TtsScriptZh = "黄迪文与永庆路口。这是第四郡著名美食街的起点。您会发现无数美味佳肴，从街边小摊到高档餐厅。",
                    TtsScriptKo = "황디에우와 빈칸 교차로. 이것은 4구의 유명한 음식거리의 시작점입니다. 길거리 음식부터 고급 레스토랑까지 수많은 맛있는 요리를 찾을 수 있습니다.",
                    TtsScriptJa = "ホアンディエウとヴィンカンの交差点。これは4区の有名な美食街の出発点です。屋台から高級レストランまで、数え切れないほどの美味しい料理が見つかります。",
                    TtsScriptFr = "Intersection Hoang Dieu et Vinh Khanh. C'est le point de départ de la célèbre rue gastronomique du 4ème arrondissement. Vous trouverez d'innombrables plats délicieux, des étals de rue aux restaurants haut de gamme."
                },
                new Poi { 
                    Title = "Ốc Oanh", 
                    Description = "Trọng điểm phố ẩm thực Vĩnh Khánh, nổi tiếng với các món ốc đặc sản Sài Gòn.", 
                    Latitude = 10.7608247, Longitude = 106.7034143, RadiusMeters = 40, Priority = 10,
                    TtsScriptVi = "Ốc Oanh. Đây là một trong những quán ốc nổi tiếng nhất phố Vĩnh Khánh. Quán phục vụ đa dạng các món ốc tươi ngon được chế biến theo nhiều cách khác nhau.",
                    TtsScriptEn = "Oc Oanh restaurant. This is one of the most famous seafood restaurants on Vinh Khanh Street. They serve a variety of fresh shellfish prepared in many different styles.",
                    TtsScriptZh = "阿文海鲜店。这是永庆街上最著名的海鲜餐厅之一。他们提供各种新鲜贝类，以多种不同风格烹制。",
                    TtsScriptKo = "옥오안 식당. 이것은 빈칸 거리에서 가장 유명한 해산물 레스토랑 중 하나입니다. 다양한 스타일로 준비된 신선한 조개류를 제공합니다.",
                    TtsScriptJa = "オックオアン食堂。これはヴィンカン通りで最も有名な海鮮料理店の一つです。様々なスタイルで調理された新鮮な貝類を提供しています。",
                    TtsScriptFr = "Restaurant Oc Oanh. C'est l'un des restaurants de fruits de mer les plus célèbres de la rue Vinh Khanh. Ils servent une variété de coquillages frais préparés selon de nombreux styles différents."
                },
                new Poi { 
                    Title = "Ớt Xiêm Quán", 
                    Description = "Quán ăn nổi tiếng với hương vị cay nồng đặc trưng, thu hút đông đảo thực khách.", 
                    Latitude = 10.7611784, Longitude = 106.705375, RadiusMeters = 30, Priority = 5,
                    TtsScriptVi = "Ớt Xiêm Quán. Quán ăn nổi tiếng với hương vị cay nồng đặc trưng từ ớt xiêm đảo Phú Quốc. Món ngon nhất là lẩu hải sản cay với nước lèo đậm đà.",
                    TtsScriptEn = "Ot Xiem restaurant. Famous for its distinctive spicy flavor from Phu Quoc island peppers. The best dish is spicy seafood hotpot with rich broth.",
                    TtsScriptZh = "阿仙餐厅。以富国岛辣椒的独特辛辣风味而闻名。最好的菜是辣海鲜火锅，汤底浓郁。",
                    TtsScriptKo = "옫씨엠 식당. 푸꾸옥 섬 고추의 독특한 매운 맛으로 유명합니다. 가장 좋은 요리는 진한 국물의 매운 해산물 전골입니다.",
                    TtsScriptJa = "オットシエム食堂。フーコック島の唐辛子の特徴的な辛さで有名です。最高の料理は濃厚な出汁の辛い海鮮鍋です。",
                    TtsScriptFr = "Restaurant Ot Xiem. Célèbre pour sa saveur épicée distinctive des poivrons de l'île de Phu Quoc. Le meilleur plat est le pot-au-feu de fruits de mer épicé avec un bouillon riche."
                },
                new Poi { 
                    Title = "Ngã 3 Tôn Đản", 
                    Description = "Giao lộ Tôn Đản - Vĩnh Khánh, nơi tập trung nhiều quán ăn vặt đêm.", 
                    Latitude = 10.760456, Longitude = 106.707236, RadiusMeters = 50, Priority = 2,
                    TtsScriptVi = "Ngã ba Tôn Đản Vĩnh Khánh. Đây là nơi tập trung nhiều quán ăn vặt đêm nổi tiếng. Đặc biệt nổi tiếng là các món nướng và lẩu đêm.",
                    TtsScriptEn = "Ton Dan and Vinh Khanh three-way intersection. This area is known for its famous late-night snack stalls. Especially popular are grilled dishes and late-night hotpot.",
                    TtsScriptZh = " Tôn Đản与永庆三岔路口。这个地区以著名的深夜小吃摊而闻名。特别受欢迎的是烧烤和深夜火锅。",
                    TtsScriptKo = "톤단과 빈칸 삼거리. 이 지역은 유명한 야간 간식摊으로 알려져 있습니다. 특히 인기 있는 것은 구운 요리와 심야 전골입니다.",
                    TtsScriptJa = "トンダンとヴィンカンの三叉路。この地域は有名な深夜の屋台で知られています。特に人気があるのは焼き料理と深夜の鍋料理です。",
                    TtsScriptFr = "Intersection à trois voies Ton Dan et Vinh Khanh. Ce quartier est connu pour ses célèbres étals de snacks nocturnes. Les plats grillés et le pot-au-feu nocturne sont particulièrement populaires."
                }
            };
            await _db.InsertAllAsync(seedData);
            Console.WriteLine("[Database] Seeded 4 POIs with multilingual TTS scripts");
        }
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        await InitAsync();
        return await _db!.Table<Poi>().ToListAsync();
    }

    /// <summary>Lấy 1 POI theo ID.</summary>
    public async Task<Poi?> GetPoiByIdAsync(string id)
    {
        await InitAsync();
        return await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>Upsert: cập nhật nếu đã có, thêm mới nếu chưa có.</summary>
    public async Task UpsertPoiAsync(Poi poi)
    {
        await InitAsync();
        var existing = await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == poi.Id);
        if (existing != null)
        {
            await _db.UpdateAsync(poi);
        }
        else
        {
            await _db.InsertAsync(poi);
        }
    }

    public async Task AddPlaybackHistoryAsync(PlaybackHistoryEntry history)
    {
        await InitAsync();
        await _db!.InsertAsync(history);
    }

    public async Task<List<PlaybackHistoryEntry>> GetPlaybackHistoryAsync()
    {
        await InitAsync();
        return await _db!.Table<PlaybackHistoryEntry>()
            .OrderByDescending(x => x.PlayedAtUtc)
            .ToListAsync();
    }
    
    /// <summary>Cập nhật TTS script cho POI theo ngôn ngữ</summary>
    public async Task UpdatePoiTtsScriptAsync(string poiId, string languageCode, string ttsScript)
    {
        await InitAsync();
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
    
    /// <summary>Lấy TTS script cho POI theo ngôn ngữ</summary>
    public async Task<string?> GetPoiTtsScriptAsync(string poiId, string languageCode)
    {
        await InitAsync();
        var poi = await _db!.Table<Poi>().FirstOrDefaultAsync(p => p.Id == poiId);
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
    
    /// <summary>Cập nhật toàn bộ TTS scripts cho POI</summary>
    public async Task UpdatePoiTtsScriptsAsync(string poiId, Dictionary<string, string> ttsScripts)
    {
        await InitAsync();
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
}
