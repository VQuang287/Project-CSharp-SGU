namespace TourMap
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            // Build UI exclusively in C# (Ignore XAML)
            var titleLabel = new Label
            {
                Text = "Audio Guide Tour",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 40, 0, 20)
            };

            var openMapBtn = new Button
            {
                Text = "Mở Bản Đồ",
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 220,
                Margin = new Thickness(0, 10)
            };
            openMapBtn.Clicked += OpenMap_Clicked;

            var openPoiListBtn = new Button
            {
                Text = "Danh Sách Trạm (POI)",
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 220,
                Margin = new Thickness(0, 10)
            };
            openPoiListBtn.Clicked += OpenPoiList_Clicked;

            Content = new VerticalStackLayout
            {
                Spacing = 15,
                Padding = new Thickness(20),
                Children = { titleLabel, openMapBtn, openPoiListBtn }
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RequestLocationPermissionAsync();
        }

        private async Task RequestLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
        }

        private async void OpenMap_Clicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(Pages.MapPage));
        }

        private async void OpenPoiList_Clicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(Pages.PoiListPage));
        }
    }
}
