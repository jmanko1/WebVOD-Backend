using WebVOD_Backend.Model.WatchTogether;

namespace WebVOD_Backend.Dtos.WatchTogether.Messages;

public class MessageDto
{
    public Participant Sender { get; set; }
    public string Message { get; set; }
    public string MessageType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum MessageType
{
    USER,
    SYSTEM
}
