namespace TabMail
{
    public static class AuthState
    {
        public static string Host { get; private set; } = "";
        public static int Port { get; private set; } = 995;
        public static bool UseSsl { get; private set; } = true;
        public static string Username { get; private set; } = "";
        public static string Password { get; private set; } = "";

        public static void Save(string host, int port, bool useSsl, string username, string password)
        {
            Host = host; Port = port; UseSsl = useSsl;
            Username = username; Password = password;
        }

        public static bool IsReady =>
            !string.IsNullOrWhiteSpace(Host) &&
            Port > 0 &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}
