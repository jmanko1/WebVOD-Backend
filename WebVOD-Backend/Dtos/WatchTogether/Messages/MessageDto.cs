namespace WebVOD_Backend.Dtos.WatchTogether.Messages;

public class MessageDto
{
    public string Login { get; set; }
    public string Message { get; set; }
    public string MessageType { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum MessageType
{
    USER,
    SYSTEM
}
