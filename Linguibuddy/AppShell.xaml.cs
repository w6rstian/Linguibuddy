namespace Linguibuddy;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        App.RegisterRoutes();
    }
}