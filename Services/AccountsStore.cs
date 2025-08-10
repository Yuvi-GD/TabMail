using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Windows.Storage;

namespace TabMail
{
    public class AccountInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DisplayName { get; set; } = "";   // e.g., you@domain (shows in UI)
        public string Host { get; set; } = "";
        public int Port { get; set; } = 995;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public static class AccountsStore
    {
        private const string ACCOUNTS_KEY = "AccountsJson";
        private const string CURRENT_KEY = "CurrentAccountId";

        private static readonly ApplicationDataContainer _ls = ApplicationData.Current.LocalSettings;

        public static List<AccountInfo> GetAll()
        {
            if (_ls.Values[ACCOUNTS_KEY] is string json && !string.IsNullOrWhiteSpace(json))
            {
                try { return JsonSerializer.Deserialize<List<AccountInfo>>(json) ?? new(); }
                catch { return new(); }
            }
            return new();
        }

        public static void SaveAll(List<AccountInfo> list)
        {
            _ls.Values[ACCOUNTS_KEY] = JsonSerializer.Serialize(list);
        }

        public static void AddOrUpdate(AccountInfo account)
        {
            var list = GetAll();
            var existing = list.FirstOrDefault(a => a.Username.Equals(account.Username, StringComparison.OrdinalIgnoreCase)
                                                 && a.Host.Equals(account.Host, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // update
                existing.DisplayName = account.DisplayName;
                existing.Port = account.Port;
                existing.UseSsl = account.UseSsl;
                existing.Password = account.Password;
            }
            else
            {
                list.Add(account);
            }
            SaveAll(list);
        }

        public static void SetCurrent(string accountId) => _ls.Values[CURRENT_KEY] = accountId;

        public static AccountInfo? GetCurrent()
        {
            var id = _ls.Values[CURRENT_KEY] as string;
            if (string.IsNullOrWhiteSpace(id)) return null;
            return GetAll().FirstOrDefault(a => a.Id == id);
        }
    }
}
