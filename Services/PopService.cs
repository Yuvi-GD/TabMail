using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Pop3;
using MailKit.Security;
using MimeKit;
using Windows.Storage;
using System.Text.RegularExpressions;

namespace TabMail
{

    /// <summary>
    /// POP3 service with in-memory caching and inline image handling.
    /// </summary>
    public class PopService
    {
        private static SecureSocketOptions ToSslOption(bool useSsl) =>
            useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

        // ---------- Simple in-memory caches ----------
        private static List<MailHeader> _cachedHeaders = new();
        private static DateTime _headersCachedAt = DateTime.MinValue;
        private static readonly Dictionary<int, EmailContent> _messageCache = new(); // by POP index
        private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

        public static void ResetCache()
        {
            _cachedHeaders.Clear();
            _messageCache.Clear();
            _headersCachedAt = DateTime.MinValue;
        }

        public async Task<bool> TestConnectionAsync(string host, int port, bool useSsl, string username, string password)
        {
            using var client = new Pop3Client();
            try
            {
                await client.ConnectAsync(host, port, ToSslOption(useSsl));
                await client.AuthenticateAsync(username, password);

                AuthState.Save(host, port, useSsl, username, password);

                await client.DisconnectAsync(true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public class MailHeader
        {
            public int Index { get; set; }
            public string From { get; set; } = "";     // display-friendly
            public string Subject { get; set; } = "";
            public DateTimeOffset? Date { get; set; }
            public bool HasAttachments { get; set; }
            public string DateDisplay => Date?.ToString("yyyy-MM-dd HH:mm") ?? "";
        }


        /// <summary>
        /// Get cached headers if fresh; otherwise refresh from server.
        /// </summary>
        public async Task<List<MailHeader>> GetCachedHeadersAsync(bool forceRefresh = false, int maxCount = 50)
        {
            var freshEnough = DateTime.Now - _headersCachedAt < _cacheTtl;
            if (!forceRefresh && freshEnough && _cachedHeaders.Count > 0)
                return _cachedHeaders;

            var list = await GetLatestHeadersAsync(maxCount);
            _cachedHeaders = list;
            _headersCachedAt = DateTime.Now;
            return list;
        }

        /// <summary>
        /// Fetch latest headers from server (newest first).
        /// </summary>
        public async Task<List<MailHeader>> GetLatestHeadersAsync(int maxCount = 50)
        {
            if (!AuthState.IsReady)
                throw new InvalidOperationException("Not authenticated.");

            using var client = new Pop3Client();
            await client.ConnectAsync(AuthState.Host, AuthState.Port, ToSslOption(AuthState.UseSsl));
            await client.AuthenticateAsync(AuthState.Username, AuthState.Password);

            var count = client.Count;
            var result = new List<MailHeader>();

            if (count > 0)
            {
                int take = Math.Min(maxCount, count);
                int start = count - take;
                for (int i = count - 1; i >= start; i--)
                {
                    var msg = await client.GetMessageAsync(i);

                    // friendly sender: prefer DisplayName, else mailbox address
                    string from = "(unknown)";
                    var mb = msg.From?.Mailboxes?.FirstOrDefault();
                    if (mb != null) from = string.IsNullOrWhiteSpace(mb.Name) ? mb.Address : mb.Name;

                    result.Add(new MailHeader
                    {
                        Index = i,
                        From = from,
                        Subject = string.IsNullOrWhiteSpace(msg.Subject) ? "(no subject)" : msg.Subject,
                        Date = msg.Date,
                        HasAttachments = msg.Attachments != null && msg.Attachments.Any()
                    });
                }

            }

            await client.DisconnectAsync(true);
            return result;
        }

        /// <summary>
        /// Get full message content with inline images processed. Uses cache when available.
        /// </summary>
        public async Task<EmailContent> GetMessageContentAsync(int index)
        {
            if (_messageCache.TryGetValue(index, out var cached)
                && DateTime.Now - _headersCachedAt < _cacheTtl)
                return cached;

            if (!AuthState.IsReady)
                throw new InvalidOperationException("Not authenticated.");

            using var client = new Pop3Client();
            await client.ConnectAsync(AuthState.Host, AuthState.Port, ToSslOption(AuthState.UseSsl));
            await client.AuthenticateAsync(AuthState.Username, AuthState.Password);

            var msg = await client.GetMessageAsync(index);

            // Build EmailContent
            var content = new EmailContent
            {
                Index = index,
                Subject = string.IsNullOrWhiteSpace(msg.Subject) ? "(no subject)" : msg.Subject,
                From = msg.From?.ToString() ?? "(unknown)",
                DateDisplay = (msg.Date != default) ? msg.Date.ToString("yyyy-MM-dd HH:mm") : "",
                HtmlBody = msg.HtmlBody ?? "",
                TextBody = msg.TextBody ?? ""
            };

            // Attachments (also collect inline images)
            foreach (var part in msg.Attachments)
            {
                if (part is MimePart filePart)
                {
                    using var ms = new MemoryStream();
                    await filePart.Content.DecodeToAsync(ms);
                    var bytes = ms.ToArray();

                    content.Attachments.Add(new AttachmentItem
                    {
                        FileName = filePart.FileName ?? "attachment",
                        Size = bytes.LongLength,
                        Data = bytes
                    });
                }
            }

            // Inline images referenced via cid:... → embed as data URI
            content.HtmlBody = await ProcessInlineImagesAsync(msg, content.HtmlBody);

            await client.DisconnectAsync(true);

            _messageCache[index] = content;
            return content;
        }

        /// <summary>
        /// Replace cid:content-id references with https://assets/InlineCache/{file}
        /// The EmailViewer maps 'assets' → LocalFolder/InlineCache, so WebView2 can load them.
        /// </summary>
        private static async Task<string> ProcessInlineImagesAsync(MimeMessage msg, string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return html;

            var local = ApplicationData.Current.LocalFolder;
            var cache = await local.CreateFolderAsync("InlineCache", CreationCollisionOption.OpenIfExists);

            // src="cid:..."  OR  src='cid:...'
            var rx = new Regex("src\\s*=\\s*([\"'])cid:(.*?)\\1", RegexOptions.IgnoreCase);
            var matches = rx.Matches(html);
            if (matches.Count == 0) return html;

            foreach (Match m in matches)
            {
                var quote = m.Groups[1].Value;
                var cid = m.Groups[2].Value.Trim('<', '>', ' ');

                var part = msg.BodyParts.OfType<MimePart>().FirstOrDefault(p =>
                    string.Equals((p.ContentId ?? "").Trim('<', '>', ' '), cid, StringComparison.OrdinalIgnoreCase));

                if (part == null) continue;

                var ext = part.ContentType?.MimeType?.Split('/')?.LastOrDefault();
                if (string.IsNullOrWhiteSpace(ext)) ext = "bin";

                var safeName = Regex.Replace(cid, @"[^a-zA-Z0-9_\-\.]", "_");
                var file = await cache.CreateFileAsync($"inline_{safeName}.{ext}", CreationCollisionOption.ReplaceExisting);

                using (var ms = new MemoryStream())
                {
                    await part.Content.DecodeToAsync(ms);
                    await FileIO.WriteBytesAsync(file, ms.ToArray());
                }

                var mapped = $"https://assets/{cache.Name}/{file.Name}";
                // replace the WHOLE attribute value we matched (keeps original quote type)
                html = html.Replace(m.Value, $"src={quote}{mapped}{quote}", StringComparison.OrdinalIgnoreCase);
            }

            return html;
        }
    }
}
