using ProjectCSharp.ViewModels;

namespace ProjectCSharp.Views;

public partial class MainPage : ContentPage
{
    // Yêu cầu DI Container cung cấp TourViewModel
    public MainPage(TourViewModel viewModel)
    {
        InitializeComponent();

        // Cực kỳ quan trọng: Gắn kết View và ViewModel
        BindingContext = viewModel;
    }
}
