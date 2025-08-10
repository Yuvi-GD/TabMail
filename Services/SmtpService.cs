using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TabMail
{
    public static class SmtpService
    {
        public static async Task SendAsync(string from, string to, string subject, string body)
        {
            var msg = new MimeMessage();
            msg.From.Add(MailboxAddress.Parse(from));
            msg.To.Add(MailboxAddress.Parse(to));
            msg.Subject = subject;
            msg.Body = new TextPart("plain") { Text = body };

            using var smtp = new SmtpClient();
            // Often SMTP host is the same domain; if yours differs, expose this in settings
            await smtp.ConnectAsync(AuthState.Host, 587, SecureSocketOptions.StartTlsWhenAvailable);
            await smtp.AuthenticateAsync(AuthState.Username, AuthState.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);
        }
    }
}
