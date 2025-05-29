using Microsoft.AspNetCore.SignalR;

namespace WebVOD_Backend.Controllers;

public class WatchTogetherHub : Hub
{
    private static string currentVideoUrl = "";
    private static double lastKnownVideoTime = 0;
    private static bool isPlaying = false;
    private static int participants = 0;
    private static DateTime? playStartedAt = null;

    public async Task SetVideo(string videoUrl)
    {
        currentVideoUrl = videoUrl;
        lastKnownVideoTime = 0;
        isPlaying = false;
        playStartedAt = null;

        await Clients.All.SendAsync("VideoChanged", videoUrl);
    }

    public async Task PlayPause(bool playing)
    {
        isPlaying = playing;
        playStartedAt = playing ? DateTime.UtcNow : null;
        await Clients.All.SendAsync("PlayPause", playing);
    }

    public async Task Seek(double time)
    {
        lastKnownVideoTime = time;
        if (isPlaying)
            playStartedAt = DateTime.UtcNow;

        await Clients.All.SendAsync("Seek", time);
    }

    public override async Task OnConnectedAsync()
    {
        participants++;
        await Clients.All.SendAsync("Participants", participants);

        var currentVideoTime = GetCurrentVideoTime();
        await Clients.Caller.SendAsync("Initialize", currentVideoUrl, isPlaying, currentVideoTime);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        participants = Math.Max(0, participants - 1);
        await Clients.Others.SendAsync("Participants", participants);
        await base.OnDisconnectedAsync(exception);
    }

    private double GetCurrentVideoTime()
    {
        if (isPlaying && playStartedAt.HasValue)
        {
            var elapsed = (DateTime.UtcNow - playStartedAt.Value).TotalSeconds;
            return lastKnownVideoTime + elapsed;
        }

        return lastKnownVideoTime;
    }
}
