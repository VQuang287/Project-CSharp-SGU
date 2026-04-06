using Microsoft.Extensions.DependencyInjection;

namespace TourMap
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Hiển thị SplashPage trước — nếu đã chọn ngôn ngữ rồi thì auto-skip sang AppShell
            return new Window(new Pages.SplashPage());
        }
    }
}
