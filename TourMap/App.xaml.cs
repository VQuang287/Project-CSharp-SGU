using Microsoft.Extensions.DependencyInjection;

namespace TourMap
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        // BUG-W02 fix: Resolve SplashPage from DI container
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var page = Handler?.MauiContext?.Services.GetService<Pages.SplashPage>()
                       ?? new Pages.SplashPage();
            return new Window(page);
        }
    }
}
