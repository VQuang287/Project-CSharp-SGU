using TourMap.Models;

namespace TourMap.Services;

/// <summary>
/// Trạng thái Narration Engine (theo PRD).
/// </summary>
public enum NarrationState
{
    /// <summary>Không phát gì — sẵn sàng nhận trigger.</summary>
    Idle,

    /// <summary>Đang phát TTS/audio cho 1 POI.</summary>
    Playing,

    /// <summary>Đã phát xong — đang trong thời gian chờ.</summary>
    Cooldown
}

/// <summary>
/// Narration Engine — quản lý vòng đời audio.
/// Nhận tín hiệu từ Geofence → phát TTS → chuyển trạng thái IDLE → PLAYING → COOLDOWN.
/// </summary>
public class NarrationEngine
{
    private readonly ITtsService _ttsService;
    private NarrationState _state = NarrationState.Idle;
    private Poi? _currentPoi;

    /// <summary>Sự kiện khi trạng thái thay đổi (cho UI cập nhật).</summary>
    public event Action<NarrationState, Poi?>? StateChanged;

    public NarrationState CurrentState => _state;
    public Poi? CurrentPoi => _currentPoi;

    public NarrationEngine(ITtsService ttsService)
    {
        _ttsService = ttsService;
        _ttsService.SpeechCompleted += OnSpeechCompleted;
    }

    /// <summary>
    /// Gọi khi Geofence Engine phát hiện user đi vào vùng POI.
    /// </summary>
    public async Task OnPOITriggeredAsync(Poi poi)
    {
        // Nếu đang PLAYING → chỉ chấp nhận POI có priority cao hơn
        if (_state == NarrationState.Playing)
        {
            if (_currentPoi != null && poi.Priority > _currentPoi.Priority)
            {
                Console.WriteLine($"[Narration] ⏩ Ngắt POI \"{_currentPoi.Title}\" " +
                                  $"→ chuyển sang POI ưu tiên cao hơn: \"{poi.Title}\"");
                _ttsService.Stop();
                // Sẽ phát POI mới ngay bên dưới
            }
            else
            {
                Console.WriteLine($"[Narration] ⏭️ Đang phát \"{_currentPoi?.Title}\", " +
                                  $"bỏ qua POI \"{poi.Title}\" (priority thấp hơn)");
                return;
            }
        }

        // Nếu đang COOLDOWN → bỏ qua
        if (_state == NarrationState.Cooldown)
        {
            Console.WriteLine($"[Narration] ⏳ Đang cooldown, bỏ qua POI \"{poi.Title}\"");
            return;
        }

        // === Phát TTS ===
        _currentPoi = poi;
        SetState(NarrationState.Playing);

        // Tạo nội dung đọc: Tên + Mô tả
        var speechText = $"{poi.Title}. {poi.Description}";
        Console.WriteLine($"[Narration] 🔊 Phát TTS: \"{speechText}\"");

        await _ttsService.SpeakAsync(speechText);
    }

    /// <summary>Dừng phát thủ công.</summary>
    public void Stop()
    {
        if (_state == NarrationState.Playing)
        {
            _ttsService.Stop();
            SetState(NarrationState.Idle);
            _currentPoi = null;
        }
    }

    /// <summary>Callback khi TTS đọc xong.</summary>
    private void OnSpeechCompleted()
    {
        Console.WriteLine($"[Narration] ✅ Đọc xong POI \"{_currentPoi?.Title}\" → COOLDOWN");
        SetState(NarrationState.Cooldown);

        // Sau 10 giây cooldown (giảm từ 10 phút của Geofence, vì Narration Engine chỉ cần ngắt tạm) 
        // → quay lại IDLE. Lưu ý: cooldown 10 phút thực sự đã được xử lý bởi GeofenceEngine.
        _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
        {
            if (_state == NarrationState.Cooldown)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _currentPoi = null;
                    SetState(NarrationState.Idle);
                    Console.WriteLine("[Narration] 🔄 Cooldown xong → IDLE, sẵn sàng nhận trigger mới");
                });
            }
        });
    }

    private void SetState(NarrationState newState)
    {
        _state = newState;
        StateChanged?.Invoke(_state, _currentPoi);
    }
}
