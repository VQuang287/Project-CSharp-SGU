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
            return new Window(new Pages.SplashPage());
        }
    }
}
