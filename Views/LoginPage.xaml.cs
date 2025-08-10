using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

namespace TabMail
{
    public sealed partial class LoginPage : Page
    {
        private readonly ApplicationDataContainer _local = ApplicationData.Current.LocalSettings;

        public LoginPage()
        {
            InitializeComponent();
            PrefillFromLocal();
        }

        private void PrefillFromLocal()
        {
            ServerBox.Text = (_local.Values["Host"] as string) ?? "";
            PortBox.Text = (_local.Values["Port"] as int?)?.ToString() ?? "995";
            SslCheck.IsChecked = (_local.Values["UseSsl"] as bool?) ?? true;
            UserBox.Text = (_local.Values["User"] as string) ?? "";
            PassBox.Password = (_local.Values["Pass"] as string) ?? "";
        }

        private void SaveToLocal(string host, int port, bool useSsl, string user, string pass)
        {
            _local.Values["Host"] = host;
            _local.Values["Port"] = port;
            _local.Values["UseSsl"] = useSsl;
            _local.Values["User"] = user;
            _local.Values["Pass"] = pass;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            ErrorBar.IsOpen = false;

            if (string.IsNullOrWhiteSpace(ServerBox.Text) ||
                string.IsNullOrWhiteSpace(PortBox.Text) ||
                string.IsNullOrWhiteSpace(UserBox.Text) ||
                string.IsNullOrWhiteSpace(PassBox.Password))
            {
                ErrorBar.Message = "All fields are required.";
                ErrorBar.IsOpen = true;
                return;
            }

            if (!int.TryParse(PortBox.Text, out var port) || port <= 0)
            {
                ErrorBar.Message = "Port must be a valid number (e.g., 995).";
                ErrorBar.IsOpen = true; return;
            }

            try
            {
                var pop = new PopService();
                bool success = await pop.TestConnectionAsync(
                    ServerBox.Text.Trim(), port, SslCheck.IsChecked ?? true,
                    UserBox.Text.Trim(), PassBox.Password.Trim());

                if (success)
                {
                    SaveToLocal(ServerBox.Text.Trim(), port, SslCheck.IsChecked ?? true,
                                UserBox.Text.Trim(), PassBox.Password.Trim());

                    // Save this as an account and set as current
                    var acct = new AccountInfo
                    {
                        DisplayName = string.IsNullOrWhiteSpace(UserBox.Text) ? ServerBox.Text.Trim() : UserBox.Text.Trim(),
                        Host = ServerBox.Text.Trim(),
                        Port = port,
                        UseSsl = SslCheck.IsChecked ?? true,
                        Username = UserBox.Text.Trim(),
                        Password = PassBox.Password.Trim()
                    };
                    AccountsStore.AddOrUpdate(acct);
                    AccountsStore.SetCurrent(acct.Id);
                    (Application.Current as App)?.RootWindow?.RefreshAccountsUI();

                    // refresh the selector in the title bar on next show
                    (Application.Current as App)?.RootWindow?.OpenHomeTab();
                }
                else
                {
                    ErrorBar.Message = "Could not connect or authenticate. Check server/port/SSL and credentials.";
                    ErrorBar.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                await Dialogs.CopyableErrorAsync(this, "Login Error", ex.ToString());
            }
        }
    }
}
