using Linguibuddy.Views;

namespace Linguibuddy
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(FlashcardsPage), typeof(FlashcardsPage));
            Routing.RegisterRoute(nameof(AudioQuizPage), typeof(AudioQuizPage));
        }
    }
}
