using WebVOD_Backend.Dtos.WatchTogether.Messages;
using WebVOD_Backend.Model.WatchTogether;

namespace WebVOD_Backend.Dtos.WatchTogether.Participants;

public class ParticipantsUpdateDto
{
    public List<Participant> Participants { get; set; }
    public MessageDto Message { get; set; }
}
