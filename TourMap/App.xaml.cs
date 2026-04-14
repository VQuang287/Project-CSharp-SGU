namespace TourMap
{
    public partial class App : Application
    {
        private readonly Pages.SplashPage _splashPage;

        public App(Pages.SplashPage splashPage)
        {
            _splashPage = splashPage;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_splashPage);
        }
    }
}
