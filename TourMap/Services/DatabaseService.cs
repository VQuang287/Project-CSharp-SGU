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
                new Poi { Title = "Ngã 4 Hoàng Diệu", Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh, điểm khởi đầu của tuyến ẩm thực nổi tiếng Quận 4.", Latitude = 10.7618898, Longitude = 106.7020039, RadiusMeters = 30, Priority = 1 },
                new Poi { Title = "Ốc Oanh", Description = "Trọng điểm phố ẩm thực Vĩnh Khánh, nổi tiếng với các món ốc đặc sản Sài Gòn.", Latitude = 10.7608247, Longitude = 106.7034143, RadiusMeters = 40, Priority = 10 },
                new Poi { Title = "Ớt Xiêm Quán", Description = "Quán ăn nổi tiếng với hương vị cay nồng đặc trưng, thu hút đông đảo thực khách.", Latitude = 10.7611784, Longitude = 106.705375, RadiusMeters = 30, Priority = 5 },
                new Poi { Title = "Ngã 3 Tôn Đản", Description = "Giao lộ Tôn Đản - Vĩnh Khánh, nơi tập trung nhiều quán ăn vặt đêm.", Latitude = 10.760456, Longitude = 106.707236, RadiusMeters = 50, Priority = 2 }
            };
            await _db.InsertAllAsync(seedData);
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
}
