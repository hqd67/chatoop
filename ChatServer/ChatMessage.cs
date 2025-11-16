using System;

public class ChatMessage
{
    public string Type { get; set; }
    public string Sender { get; set; }
    public string Recipient { get; set; }
    public string Text { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileData { get; set; }
    public DateTime Timestamp { get; set; }
}
