using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Windows.Storage;
using System;

namespace TabMail
{
    public sealed partial class EmailViewer : UserControl
    {
        private EmailContent? _content;
        private bool _mapped;

        public EmailViewer() { InitializeComponent(); }

        private async Task EnsureWebViewReadyAsync()
        {
            if (HtmlView.CoreWebView2 == null)
                await HtmlView.EnsureCoreWebView2Async();

            // Map a virtual host to our inline images cache folder exactly once
            if (!_mapped)
            {
                var local = ApplicationData.Current.LocalFolder;
                var cache = await local.CreateFolderAsync("InlineCache", CreationCollisionOption.OpenIfExists);
                // All files under InlineCache are exposed at: https://assets/
                if (HtmlView.CoreWebView2 == null) return;
                HtmlView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "assets", cache.Path,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                _mapped = true;
            }
        }

        public async Task ShowAsync(EmailContent content)
        {
            _content = content;

            SubjectText.Text = content.Subject;
            MetaText.Text = $"{content.From} • {content.DateDisplay}";

            // Attachments list
            if (content.Attachments != null && content.Attachments.Any())
            {
                AttachPanel.Visibility = Visibility.Visible;
                AttachList.ItemsSource = content.Attachments;
            }
            else
            {
                AttachPanel.Visibility = Visibility.Collapsed;
                AttachList.ItemsSource = null;
            }

            if (!string.IsNullOrWhiteSpace(content.HtmlBody))
            {
                await EnsureWebViewReadyAsync();

                HtmlView.Visibility = Visibility.Visible;
                TextScroll.Visibility = Visibility.Collapsed;

                // IMPORTANT: HtmlBody now contains links like: src="https://assets/InlineCache/...."
                var html = $"<!doctype html><html><head><meta charset='utf-8'></head>" +
                           $"<body style='font-family:Segoe UI,Arial,sans-serif;margin:16px;color:#ddd;background:#0F1116'>{content.HtmlBody}</body></html>";

                HtmlView.NavigateToString(html);
            }
            else
            {
                HtmlView.Visibility = Visibility.Collapsed;
                TextScroll.Visibility = Visibility.Visible;
                TextBody.Text = string.IsNullOrWhiteSpace(content.TextBody) ? "(no content)" : content.TextBody;
            }
        }

        private async void SaveAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (_content == null) return;
            if ((sender as Button)?.DataContext is not AttachmentItem item || item.Data == null) return;

            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle((Application.Current as App)?.RootWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedFileName = item.FileName;
            var ext = System.IO.Path.GetExtension(item.FileName).Trim('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = "bin";
            picker.FileTypeChoices.Add("File", new System.Collections.Generic.List<string> { "." + ext });

            var file = await picker.PickSaveFileAsync();
            if (file != null) await Windows.Storage.FileIO.WriteBytesAsync(file, item.Data);
        }

        private async void Reply_Click(object sender, RoutedEventArgs e)
        {
            if (_content == null) return;

            // Simple compose dialog
            var dlg = new ContentDialog
            {
                Title = "Reply",
                XamlRoot = this.XamlRoot,
                PrimaryButtonText = "Send",
                CloseButtonText = "Cancel"
            };

            var box = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 160
            };
            box.Text = "\n\n----- Original -----\n" + (_content.TextBody ?? "");

            dlg.Content = box;

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    // Reply to the original sender; From = your account username (email)
                    await SmtpService.SendAsync(
                        AuthState.Username,
                        _content.From.Contains("<") ? _content.From.Split('<', '>')[1] : _content.From,
                        "Re: " + _content.Subject,
                        box.Text);

                    await Dialogs.CopyableErrorAsync(this, "Sent", "Reply sent successfully.");
                }
                catch (System.Exception ex)
                {
                    await Dialogs.CopyableErrorAsync(this, "Send Error", ex.ToString());
                }
            }
        }

    }
}
