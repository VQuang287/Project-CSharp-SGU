using SQLite;
using TourMap.Models;

namespace TourMap.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _db;

    public DatabaseService()
    {
    }

    private async Task InitAsync()
    {
        if (_db is not null)
            return;

        // Đổi tên file db để ép app tạo mới Database (Tọa độ chính xác từ người dùng)
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TourMap_VinhKhanh_v5.db3");
        _db = new SQLiteAsyncConnection(databasePath);

        // Tạo bảng nếu chưa có
        await _db.CreateTableAsync<Poi>();

        // Chèn dữ liệu mẫu Phố ẩm thực Vĩnh Khánh theo tọa độ thủ công
        var count = await _db.Table<Poi>().CountAsync();
        if (count == 0)
        {
            var seedData = new List<Poi>
            {
                new Poi { Title = "Ngã 4 Hoàng Diệu", Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh", Latitude = 10.7618898, Longitude = 106.7020039, RadiusMeters = 30, Priority = 1 },
                new Poi { Title = "Ốc Oanh", Description = "Trọng điểm phố ẩm thực", Latitude = 10.7608247, Longitude = 106.7034143, RadiusMeters = 40, Priority = 10 },
                new Poi { Title = "Ớt Xiêm Quán", Description = "Quán ăn nổi tiếng", Latitude = 10.7611784, Longitude = 106.705375, RadiusMeters = 30, Priority = 5 },
                new Poi { Title = "Ngã 3 Tôn Đản", Description = "Giao lộ Tôn Đản - Vĩnh Khánh", Latitude = 10.760456, Longitude = 106.707236, RadiusMeters = 50, Priority = 2 }
            };
            await _db.InsertAllAsync(seedData);
        }
    }

    public async Task<List<Poi>> GetPoisAsync()
    {
        await InitAsync();
        return await _db.Table<Poi>().ToListAsync();
    }
}
