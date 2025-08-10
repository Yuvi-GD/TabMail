# TabMail

Tabbed WinUI 3 email client for Windows with multi‑account POP3, inline images, attachments, and a modern, compact layout.

## ✨ Features
- **Tabbed UI** in the title bar (open messages in new tabs)
- **Multi‑account** support (switch quickly from the title bar)
- **Inbox + reader** split view, month grouping, quick search
- **Inline images & attachments** (view and save)
- **Caching** for faster header loads
- **Reply** (basic SMTP) – simple compose dialog

## 📦 Tech
- .NET 8, **WinUI 3**
- **MailKit/MimeKit** (POP3/SMTP)
- **WebView2** for HTML emails

## 🚀 Getting started
1. **Requirements:** Visual Studio 2022, .NET 8, Windows App SDK / WinUI 3 workloads.
2. Clone:
   ```bash
   git clone https://github.com/Yuvi-GD/TabMail.git
   cd TabMail
3. Open the solution in Visual Studio and **Build → Rebuild**.
4. **Run (F5)** → Add your POP3 account (host, port, SSL, username, password).

> POP3 is receive‑focused. SMTP settings can be customized if your provider uses different server/ports.

## 🛠 Setup (Dev)

### Prereqs
- **Windows 10/11**
- **Visual Studio 2022** with workloads:
  - .NET Desktop Development
  - Windows App SDK / WinUI 3 tooling
- **.NET 8 SDK**
- **WebView2 Runtime** (usually already installed with Edge)

### Clone & Restore
```bash
git clone https://github.com/Yuvi-GD/TabMail.git
cd TabMail
dotnet restore

## 🧰 Configuration

* Accounts are stored locally in app settings (for now).
* Inline images are written to a local `InlineCache` and displayed via WebView2 virtual host mapping.

## 🗺️ Roadmap

* Detachable tabs (move tab to its own window)
* On‑disk header cache (SQLite) for instant startup
* Unread states, sender avatars
* Full composer (To/CC/BCC, attachments)
* IMAP support (optional)

## 🤝 Contributing

PRs and issues welcome! Please:

* File issues with repro steps / screenshots.
* Keep PRs focused and small when possible.

## 📄 License

[MIT](LICENSE)

## 🙏 Acknowledgements

* [MailKit/MimeKit](https://github.com/jstedfast/MailKit)
* WinUI 3 and WebView2 teams
