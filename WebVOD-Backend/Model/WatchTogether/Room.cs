namespace WebVOD_Backend.Model.WatchTogether;

public class Room
{
    public string Id { get; }
    public string AccessCode { get; }

    public string CurrentVideoUrl { get; set; } = string.Empty;
    public string CurrentVideoTitle { get; set; } = string.Empty;
    public double LastKnownVideoTime { get; set; } = 0;
    public bool IsPlaying { get; set; } = false;
    public DateTime? PlayStartedAt { get; set; }
    public DateTime? CountdownStartedAt { get; set; }

    public Dictionary<string, string> Participants { get; set; } = new();
    public object SyncRoot { get; } = new();

    public Room(string id, string accessCode)
    {
        Id = id;
        AccessCode = accessCode;
    }

    public double? GetCountdown()
    {
        if (!CountdownStartedAt.HasValue) return null;

        var countdown = 3 - (DateTime.UtcNow - CountdownStartedAt.Value).TotalSeconds;
        return Math.Max(countdown, 0);
    }

    public double GetCurrentVideoTime()
    {
        if (IsPlaying && PlayStartedAt.HasValue)
        {
            var elapsed = (DateTime.UtcNow - PlayStartedAt.Value).TotalSeconds;
            return LastKnownVideoTime + elapsed;
        }

        return LastKnownVideoTime;
    }

    public bool VerifyAccessCode(string code) => string.Equals(code, AccessCode, StringComparison.Ordinal);

    public List<string> GetParticipants()
    {
        lock (SyncRoot)
        {
            return Participants.Values.ToList();
        }
    }
}
