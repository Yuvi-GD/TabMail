using System.Collections.Generic;

namespace TabMail
{
    public class AttachmentItem
    {
        public string FileName { get; set; } = "";
        public long Size { get; set; } = 0;
        public byte[]? Data { get; set; }
        public string SizeDisplay => Size >= 1024 ? $"{Size / 1024} KB" : $"{Size} B";
    }

    public class EmailContent
    {
        public int Index { get; set; }
        public string Subject { get; set; } = "";
        public string From { get; set; } = "";
        public string DateDisplay { get; set; } = "";
        public string HtmlBody { get; set; } = "";
        public string TextBody { get; set; } = "";
        public List<AttachmentItem> Attachments { get; set; } = new();
    }
}
