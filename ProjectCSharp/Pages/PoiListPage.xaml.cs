using TourMap.ViewModels;

namespace TourMap.Pages;

public partial class PoiListPage : ContentPage
{
    private readonly MainViewModel _vm;

    private CollectionView PoisView;

    public PoiListPage(MainViewModel vm)
    {
        PoisView = new CollectionView
        {
            ItemTemplate = new DataTemplate(() =>
            {
                var nameLabel = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold };
                nameLabel.SetBinding(Label.TextProperty, "Name");
                
                var descLabel = new Label { FontSize = 14, TextColor = Colors.Gray };
                descLabel.SetBinding(Label.TextProperty, "Description");

                var layout = new VerticalStackLayout { Padding = 10, Children = { nameLabel, descLabel } };
                return layout;
            })
        };

        var grid = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }
        };
        grid.Add(new Label { Text = "Danh sách các trạm Audio Guide", FontSize = 22, FontAttributes = FontAttributes.Bold, Padding = 10 }, 0, 0);
        grid.Add(PoisView, 0, 1);
        Content = grid;

        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        PoisView.ItemsSource = _vm.Pois;
    }
}