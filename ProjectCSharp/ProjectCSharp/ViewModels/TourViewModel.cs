using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProjectCSharp.Services;

namespace ProjectCSharp.ViewModels
{
    public class TourViewModel : INotifyPropertyChanged
    {
        private readonly ILocationService _locationService;
        private string _locationText = "Chưa có dữ liệu vị trí";

        // Biến này sẽ bind (trói buộc) trực tiếp với giao diện
        public string LocationText
        {
            get => _locationText;
            set
            {
                _locationText = value;
                OnPropertyChanged(); // Thông báo cho UI biết dữ liệu đã đổi
            }
        }

        // Lệnh được gọi khi người dùng bấm nút
        public ICommand GetLocationCommand { get; }

        public TourViewModel(ILocationService locationService)
        {
            _locationService = locationService;
            GetLocationCommand = new Command(async () => await FetchLocationAsync());
        }

        private async Task FetchLocationAsync()
        {
            LocationText = "Đang xin quyền và lấy vị trí...";

            // 1. Kiểm tra và yêu cầu quyền vị trí từ người dùng
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                LocationText = "Bạn chưa cấp quyền GPS cho ứng dụng!";
                return;
            }

            // 2. Gọi Service để lấy tọa độ
            var location = await _locationService.GetCurrentLocationAsync();

            if (location != null)
            {
                LocationText = $"Vĩ độ: {location.Latitude}\nKinh độ: {location.Longitude}";
            }
            else
            {
                LocationText = "Không thể lấy được vị trí hiện tại. Hãy kiểm tra lại kết nối/GPS.";
            }
        }

        // --- Boilerplate code cho MVVM ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}