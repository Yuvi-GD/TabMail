using System.Linq;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using System; // <-- fixes CS0246 for Exception


namespace TabMail
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TryEnableMica();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DragRegion);
            LoadAccountsIntoUI();
            TrySelectCurrentAccount();

            PaneToggle.IsChecked = AppUiState.ListVisible;
            AppUiState.Changed += (_, __) =>
            {
                // inform current page to reflect new state
                if (TopTabs.SelectedItem is TabViewItem sel && sel.Tag is Page page)
                    RootFrame.Content = page; // forces layout refresh in page
            };
        }

        // ---- helpers ----

        public void RefreshAccountsUI()
        {
            LoadAccountsIntoUI();
            TrySelectCurrentAccount();
        }


        private void LoadAccountsIntoUI()
        {
            AccountBox.ItemsSource = AccountsStore.GetAll();
            AccountBox.DisplayMemberPath = "DisplayName";
        }

        private void TrySelectCurrentAccount()
        {
            var current = AccountsStore.GetCurrent();
            if (current == null)
                return;

            AccountBox.SelectedItem = AccountsStore.GetAll().FirstOrDefault(a => a.Id == current.Id);
            // apply creds to AuthState
            AuthState.Save(current.Host, current.Port, current.UseSsl, current.Username, current.Password);
        }

        private async void AddAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            try { RootFrame.Content = new LoginPage(); }
            catch (Exception ex) { await Dialogs.CopyableErrorAsync(this.Content as FrameworkElement, "Add Account Error", ex.ToString()); }
        }


        // account switched by user
        private void AccountBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountBox.SelectedItem is not AccountInfo acct) return;

            AccountsStore.SetCurrent(acct.Id);
            AuthState.Save(acct.Host, acct.Port, acct.UseSsl, acct.Username, acct.Password);

            PopService.ResetCache();                 // ✅ clear previous account cache

            TopTabs.TabItems.Clear();
            OpenHomeTab();
        }


        private void PaneToggle_Click(object sender, RoutedEventArgs e)
        {
            AppUiState.ListVisible = PaneToggle.IsChecked ?? true;
        }

        private void TryEnableMica()
        {
            try { SystemBackdrop = new MicaBackdrop(); } catch { }
        }

        // Called at app start
        public void NavigateToLogin() => RootFrame.Content = new LoginPage();

        // Create a new Home tab and show it
        public void OpenHomeTab()
        {
            var home = new HomePage();                 // make the page
            var tab = new TabViewItem { Header = "Home", IsClosable = true, Tag = home }; // store it in Tag
            TopTabs.TabItems.Add(tab);
            TopTabs.SelectedItem = tab;                // triggers SelectionChanged → shows in RootFrame
        }

        // Create a new Home tab and open a specific message
        public async void OpenHomeTabAndOpenMessage(int index)
        {
            var home = new HomePage();
            await home.OpenMessageAsync(index);
            var tab = new TabViewItem { Header = "Mail", IsClosable = true, Tag = home };
            TopTabs.TabItems.Add(tab);
            TopTabs.SelectedItem = tab;
        }

        private void TopTabs_AddTabButtonClick(TabView sender, object args) => OpenHomeTab();

        private void TopTabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
            if (sender.TabItems.Count == 0) OpenHomeTab();
            if (TopTabs.SelectedItem is TabViewItem sel && sel.Tag is Page page) RootFrame.Content = page;
        }

        private void TopTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TopTabs.SelectedItem is TabViewItem sel && sel.Tag is Page page)
                RootFrame.Content = page;
        }

        // ✅ Auto‑login: skip Login if saved creds work
        public async void TryAutoLoginAsync()
        {
            var local = ApplicationData.Current.LocalSettings.Values;
            var host = local["Host"] as string ?? "";
            var user = local["User"] as string ?? "";
            var pass = local["Pass"] as string ?? "";
            var port = (local["Port"] as int?) ?? 995;
            var useSsl = (local["UseSsl"] as bool?) ?? true;

            if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            {
                var pop = new PopService();
                var ok = await pop.TestConnectionAsync(host, port, useSsl, user, pass);
                if (ok) { OpenHomeTab(); return; }
            }
            // fallback to login
            NavigateToLogin();
        }
    }
}
