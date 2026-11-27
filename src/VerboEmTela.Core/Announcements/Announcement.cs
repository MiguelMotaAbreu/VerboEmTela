namespace VerboEmTela.Core.Announcements;

public sealed class Announcement
{
    internal Announcement(string text, long detectedTimestamp, long visualStartTimestamp)
    {
        Text = text;
        DetectedTimestamp = detectedTimestamp;
        VisualStartTimestamp = visualStartTimestamp;
    }

    public string Text { get; }
    public long DetectedTimestamp { get; }
    public long VisualStartTimestamp { get; }
}
