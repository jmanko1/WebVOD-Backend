using WebVOD_Backend.Model.WatchTogether;

namespace WebVOD_Backend.Dtos.WatchTogether.Room;

public class InitializeConnection
{
    public string VideoId { get; set; }
    public string VideoUrl { get; set; }
    public string VideoTitle { get; set; }
    public double InitialTime { get; set; }
    public bool IsPlaying { get; set; }
    public double? Countdown { get; set; }
    public List<Participant> Participants { get; set; }
}
