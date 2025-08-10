using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TabMail
{
    public class MailHeaderVM : INotifyPropertyChanged
    {
        public int Index { get; set; }
        public string From { get; set; } = "";
        public string Subject { get; set; } = "";
        public string DateDisplay { get; set; } = "";
        public string GroupKey { get; set; } = "";
        public bool HasAttachments { get; set; }

        private string _preview = "";
        public string Preview { get => _preview; set { _preview = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }


    // Simple grouping model for XAML
    public class Group<T> : ObservableCollection<T>
    {
        public string Key { get; set; }
        public new IList<T> Items => this; // WinUI grouping expects this
        public Group(string key, IEnumerable<T> items) : base(items) { Key = key; }
    }



    public sealed partial class HomePage : Page
    {
        private readonly List<MailHeaderVM> _all = new();
        private readonly PopService _pop = new();

        // Expose a CollectionViewSource to XAML for grouping
        public CollectionViewSource GroupedSource { get; } = new() { IsSourceGrouped = true, ItemsPath = new PropertyPath("Items") };

        public HomePage()
        {
            InitializeComponent();

            // initial pane width
            UpdateListPane();

            // listen for global show/hide
            AppUiState.Changed += (_, __) => UpdateListPane();

            _ = LoadAsync(forceRefresh: false, count: 50);
        }

        private void UpdateListPane()
        {
            // collapse/expand left column
            RootGrid.ColumnDefinitions[0].Width = AppUiState.ListVisible ? new GridLength(3, GridUnitType.Star) : new GridLength(0);
        }

        public async Task OpenMessageAsync(int index) => await ShowInReaderAsync(index);

        private async Task FillPreviewsAsync(int maxItems)
        {
            var targets = _all.Take(maxItems).ToList();
            foreach (var vm in targets)
            {
                try
                {
                    var msg = await _pop.GetMessageContentAsync(vm.Index);
                    // Prefer TextBody snippet; else strip HTML tags crudely
                    var text = !string.IsNullOrWhiteSpace(msg.TextBody)
                        ? msg.TextBody
                        : System.Text.RegularExpressions.Regex.Replace(msg.HtmlBody ?? "", "<.*?>", " ");
                    vm.Preview = new string(text.Trim().Take(120).ToArray());
                }
                catch { /* ignore preview failure */ }
            }
        }


        private async Task LoadAsync(bool forceRefresh, int count)
        {
            ErrorBar.IsOpen = false;
            Busy.IsActive = true;

            try
            {
                _all.Clear();

                var headers = await _pop.GetCachedHeadersAsync(forceRefresh, count);

                // sort: newest by Date, then by Index
                foreach (var h in headers
                         .OrderByDescending(h => h.Date ?? DateTimeOffset.MinValue)
                         .ThenByDescending(h => h.Index))
                {
                    _all.Add(new MailHeaderVM
                    {
                        Index = h.Index,
                        From = h.From,
                        Subject = h.Subject,
                        DateDisplay = h.Date?.ToString("yyyy-MM-dd HH:mm") ?? "",
                        GroupKey = (h.Date?.ToString("MMM yyyy")) ?? "Unknown",
                        HasAttachments = h.HasAttachments,
                        Preview = ""   // we’ll fill a few previews in background
                    });
                }

                ApplyFilter(SearchBox.Text);

                // lazily fetch previews for the first few items (does not block the UI)
                //FillPreviewsAsync(10);
            }
            catch (Exception ex)
            {
                await Dialogs.CopyableErrorAsync(this, "Load Error", ex.ToString());
            }
            finally
            {
                Busy.IsActive = false;
            }
        }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            var raw = (CountBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "50";
            int.TryParse(raw, out var count); if (count <= 0) count = 50;
            await LoadAsync(forceRefresh: true, count: count);
        }

        private async void InboxList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MailHeaderVM vm) await ShowInReaderAsync(vm.Index);
        }

        private void InboxList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (GetSelected() is MailHeaderVM vm)
                (Application.Current as App)?.RootWindow?.OpenHomeTabAndOpenMessage(vm.Index);
        }

        private void InboxList_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var p = e.GetCurrentPoint(InboxList);
            if (!p.Properties.IsMiddleButtonPressed) return;
            if (GetSelected() is MailHeaderVM vm)
                (Application.Current as App)?.RootWindow?.OpenHomeTabAndOpenMessage(vm.Index);
            e.Handled = true;
        }

        private MailHeaderVM? GetSelected() => (InboxList.SelectedItem as MailHeaderVM) ?? null;

        private async Task ShowInReaderAsync(int index)
        {
            Busy.IsActive = true;
            try
            {
                var content = await _pop.GetMessageContentAsync(index);
                var viewer = new EmailViewer();
                await viewer.ShowAsync(content);
                ReaderHost.Content = viewer;
            }
            catch (Exception ex)
            {
                await Dialogs.CopyableErrorAsync(this, "Open Error", ex.ToString());
            }
            finally { Busy.IsActive = false; }
        }

        // ---------- Search + Grouping ----------
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter(SearchBox.Text);

        private void ApplyFilter(string? text)
        {
            text = (text ?? "").Trim().ToLowerInvariant();
            IEnumerable<MailHeaderVM> src = _all;

            if (!string.IsNullOrEmpty(text))
            {
                src = _all.Where(x =>
                    (x.Subject ?? "").ToLowerInvariant().Contains(text) ||
                    (x.From ?? "").ToLowerInvariant().Contains(text));
            }

            // group by Month
            var groups = src
                .GroupBy(x => x.GroupKey)
                .OrderByDescending(g => g.Key) // most recent month first
                .Select(g => new Group<MailHeaderVM>(g.Key, g.ToList()))
                .ToList();

            GroupedSource.Source = groups;
        }
    }
}
