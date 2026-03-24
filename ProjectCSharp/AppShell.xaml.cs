namespace TourMap
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Pages.MapPage), typeof(Pages.MapPage));
            Routing.RegisterRoute(nameof(Pages.PoiListPage), typeof(Pages.PoiListPage));
        }
    }
}
