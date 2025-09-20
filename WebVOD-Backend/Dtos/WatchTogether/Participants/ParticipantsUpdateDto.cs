using WebVOD_Backend.Dtos.WatchTogether.Messages;

namespace WebVOD_Backend.Dtos.WatchTogether.Participants;

public class ParticipantsUpdateDto
{
    public List<string> Participants { get; set; }
    public MessageDto Message { get; set; }
}
