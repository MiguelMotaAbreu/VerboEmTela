namespace VerboEmTela.Core.Announcements;

internal static class AnnouncementLifetime
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(15);

    public static bool HasExpired(TimeProvider clock, long detectedTimestamp, long now) =>
        Remaining(clock, detectedTimestamp, now) <= TimeSpan.Zero;

    public static TimeSpan Remaining(TimeProvider clock, long detectedTimestamp, long now)
    {
        var age = clock.GetElapsedTime(detectedTimestamp, now);
        if (age < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(detectedTimestamp), "Detection cannot be in the future.");
        }

        return MaximumAge - age;
    }
}
