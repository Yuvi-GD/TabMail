using Microsoft.UI.Xaml;
using System;
using Microsoft.UI.Xaml.Controls;

namespace TabMail
{
    public static class Dialogs
    {
        public static async System.Threading.Tasks.Task CopyableErrorAsync(
            Microsoft.UI.Xaml.FrameworkElement anchor, string title, string text)
        {
            var dlg = new ContentDialog
            {
                Title = title,
                Content = new ScrollViewer
                {
                    Content = new TextBox { Text = text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap }
                },
                PrimaryButtonText = "OK",
                XamlRoot = anchor.XamlRoot
            };
            await dlg.ShowAsync();
        }
    }
}
