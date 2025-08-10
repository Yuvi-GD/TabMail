using System;
using Windows.Storage;

namespace TabMail
{
    public static class AppUiState
    {
        private const string Key = "ListVisible";
        private static bool _listVisible = true;

        static AppUiState()
        {
            var ls = ApplicationData.Current.LocalSettings.Values;
            if (ls.ContainsKey(Key) && ls[Key] is bool b) _listVisible = b;
        }

        public static bool ListVisible
        {
            get => _listVisible;
            set
            {
                if (_listVisible == value) return;
                _listVisible = value;
                ApplicationData.Current.LocalSettings.Values[Key] = value;
                Changed?.Invoke(null, EventArgs.Empty);
            }
        }

        public static void Toggle() => ListVisible = !ListVisible;
        public static event EventHandler? Changed;
    }
}
