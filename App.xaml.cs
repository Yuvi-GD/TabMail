using Microsoft.UI.Xaml;

namespace TabMail
{
    public partial class App : Application
    {
        public MainWindow? RootWindow { get; private set; }

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            RootWindow = new MainWindow();
            RootWindow.Activate();

            // 🔁 Attempt auto‑login; if it fails, it will show Login
            RootWindow.TryAutoLoginAsync();
        }

    }
}
