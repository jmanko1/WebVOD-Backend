using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using WebVOD_Backend.Dtos.WatchTogether.Messages;
using WebVOD_Backend.Dtos.WatchTogether.Participants;
using WebVOD_Backend.Dtos.WatchTogether.Room;
using WebVOD_Backend.Dtos.WatchTogether.Videos;
using WebVOD_Backend.Model.WatchTogether;
using WebVOD_Backend.Repositories.Interfaces;

namespace WebVOD_Backend.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WatchTogetherHub : Hub
{
    private static readonly ConcurrentDictionary<string, Room> Rooms = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionToRoom = new();
    private readonly IVideoRepository _videoRepository;
    private readonly IUserRepository _userRepository;

    public WatchTogetherHub(IVideoRepository videoRepository, IUserRepository userRepository)
    {
        _videoRepository = videoRepository;
        _userRepository = userRepository;
    }

    public async Task<RoomCreatedDto> CreateRoom()
    {
        var login = GetLoginOrThrow();

        var user = await _userRepository.FindByLogin(login);
        if (user == null)
        {
            throw new HubException("Zaloguj się ponownie.");
        }

        var roomId = GenerateRoomId();
        while (Rooms.ContainsKey(roomId))
        {
            roomId = GenerateRoomId();
        }

        var accessCode = GenerateAccessCode(6);

        var room = new Room(roomId, accessCode);

        lock (room.SyncRoot)
        {
            room.Participants[Context.ConnectionId] = new Participant
            {
                Login = user.Login,
                ImageUrl = user.ImageUrl
            };

            Rooms[roomId] = room;
        }

        ConnectionToRoom[Context.ConnectionId] = roomId;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var initialize = new InitializeConnection
        {
            VideoId = room.CurrentVideoId,
            VideoUrl = room.CurrentVideoUrl,
            VideoTitle = room.CurrentVideoTitle,
            InitialTime = room.GetCurrentVideoTime(),
            IsPlaying = room.IsPlaying,
            Participants = room.GetParticipants()
        };
        await Clients.Caller.SendAsync("Initialize", initialize);

        var createdRoom = new RoomCreatedDto
        {
            RoomId = roomId,
            AccessCode = accessCode
        };
        return createdRoom;
    }

    public async Task JoinRoom(string roomId, string accessCode)
    {
        var login = GetLoginOrThrow();
        var user = await _userRepository.FindByLogin(login);
        if (user == null)
        {
            throw new HubException("Zaloguj się ponownie.");
        }

        if (!Rooms.TryGetValue(roomId, out var room))
        {
            throw new HubException("Nie znaleziono pokoju.");
        }

        if (!room.VerifyAccessCode(accessCode))
        {
            throw new HubException("Nieprawidłowy kod dostępu.");
        }

        lock (room.SyncRoot)
        {
            if (room.Participants.Count >= 3 && !room.Participants.ContainsKey(Context.ConnectionId))
            {
                throw new HubException("Pokój jest pełny (maks. 3 uczestników).");
            }

            if (room.Participants.Any(p => p.Value.Login == login))
            {
                throw new HubException("Użytkownik już połączony z tym pokojem.");
            }

            room.Participants[Context.ConnectionId] = new Participant
            {
                Login = user.Login,
                ImageUrl = user.ImageUrl
            };

            Rooms[roomId] = room;
        }

        ConnectionToRoom[Context.ConnectionId] = roomId;

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        var initialize = new InitializeConnection
        {
            VideoId = room.CurrentVideoId,
            VideoUrl = room.CurrentVideoUrl,
            VideoTitle = room.CurrentVideoTitle,
            InitialTime = room.GetCurrentVideoTime(),
            IsPlaying = room.IsPlaying,
            Participants = room.GetParticipants(),
            Countdown = room.GetCountdown()
        };

        await Clients.Caller.SendAsync("Initialize", initialize);

        var systemJoinMessage = new MessageDto
        {
            Sender = new Participant
            {
                Login = "System",
                ImageUrl = string.Empty
            },
            Message = $"Użytkownik {login} dołączył do pokoju.",
            MessageType = MessageType.SYSTEM.ToString(),
            Timestamp = DateTime.UtcNow
        };

        await SendParticipantsList(roomId, systemJoinMessage);
    }

    public async Task SetVideo(string videoUrl)
    {
        var login = GetLoginOrThrow();
        var roomId = EnsureInRoom(login);

        if (!Rooms.TryGetValue(roomId, out var room))
        {
            throw new HubException("Nie znaleziono pokoju.");
        }

        var uri = new Uri(videoUrl);
        var videoId = Path.GetFileNameWithoutExtension(uri.AbsolutePath);

        if (!ObjectId.TryParse(videoId, out _))
        {
            throw new HubException("Nie znaleziono filmu.");
        }

        var video = await _videoRepository.FindById(videoId);
        if (video == null || video.Status != Model.VideoStatus.PUBLISHED)
        {
            throw new HubException("Nie znaleziono filmu.");
        }

        lock (room.SyncRoot)
        {
            room.IsPlaying = false;
            room.CurrentVideoId = video.Id ?? "";
            room.CurrentVideoUrl = video.VideoPath ?? "";
            room.CurrentVideoTitle = video.Title ?? "";
            room.PlayStartedAt = null;
            room.LastKnownVideoTime = 0;
            room.CountdownStartedAt = DateTime.UtcNow;

            Rooms[roomId] = room;
        }

        var videoChange = new VideoChangeDto
        {
            Id = room.CurrentVideoId,
            VideoUrl = room.CurrentVideoUrl,
            Title = room.CurrentVideoTitle
        };

        await Clients.Group(roomId).SendAsync("VideoChanged", videoChange);
    }

    public async Task PlayPause(bool playing, double time)
    {
        var login = GetLoginOrThrow();
        var roomId = EnsureInRoom(login);

        if (!Rooms.TryGetValue(roomId, out var room))
        {
            throw new HubException("Nie znaleziono pokoju.");
        }

        lock (room.SyncRoot)
        {
            room.LastKnownVideoTime = time;
            room.IsPlaying = playing;
            room.PlayStartedAt = playing ? DateTime.UtcNow : null;
            room.CountdownStartedAt = null;

            Rooms[roomId] = room;
        }

        await Clients.OthersInGroup(roomId).SendAsync("PlayPause", playing);
    }

    public async Task Seek(double time)
    {
        var login = GetLoginOrThrow();
        var roomId = EnsureInRoom(login);

        if (!Rooms.TryGetValue(roomId, out var room))
        {
            throw new HubException("Nie znaleziono pokoju.");
        }

        lock (room.SyncRoot)
        {
            room.LastKnownVideoTime = time;
            if (room.IsPlaying)
            {
                room.PlayStartedAt = DateTime.UtcNow;
            }

            Rooms[roomId] = room;
        }

        await Clients.OthersInGroup(roomId).SendAsync("Seek", time);
    }

    public async Task LeaveRoom()
    {
        if (!ConnectionToRoom.TryGetValue(Context.ConnectionId, out var roomId))
            return;

        await RemoveFromRoom(roomId, Context.ConnectionId);
    }

    public async Task SendMessage(string message)
    {
        var login = GetLoginOrThrow();
        var roomId = EnsureInRoom(login);
        var user = Rooms[roomId].Participants[Context.ConnectionId];

        if (message.Length == 0 || string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Podaj treść wiadomości");
        }

        if (message.Length > 200)
        {
            throw new HubException("Wiadomość może mieć maksymalnie 200 znaków.");
        }

        var messageDto = new MessageDto
        {
            Sender = new Participant
            {
                Login = user.Login,
                ImageUrl = user.ImageUrl
            },
            Message = message,
            MessageType = MessageType.USER.ToString(),
            Timestamp = DateTime.UtcNow
        };

        await Clients.OthersInGroup(roomId).SendAsync("ReceiveMessage", messageDto);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToRoom.TryGetValue(Context.ConnectionId, out var roomId))
        {
            await RemoveFromRoom(roomId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private string EnsureInRoom(string login)
    {
        if (!ConnectionToRoom.TryGetValue(Context.ConnectionId, out var roomId))
        {
            throw new HubException("Użytkownik nie jest połączony z żadnym pokojem.");
        }

        if (!Rooms.TryGetValue(roomId, out var room))
        {
            throw new HubException("Nie znaleziono pokoju.");
        }

        if (!room.Participants.TryGetValue(Context.ConnectionId, out var partcipant))
        {
            throw new HubException("Użytkownik nie jest członkiem tego pokoju.");
        }

        if (partcipant.Login != login)
        {
            throw new HubException("Nieprawidłowy login.");
        }

        return roomId;
    }

    private string GenerateRoomId(int length = 12)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);

        foreach (var b in bytes)
        {
            sb.Append(alphabet[b % alphabet.Length]);
        }

        return sb.ToString();
    }

    private string GenerateAccessCode(int length)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);

        foreach (var b in bytes)
        {
            sb.Append(alphabet[b % alphabet.Length]);
        }

        return sb.ToString();
    }

    private async Task SendParticipantsList(string roomId, MessageDto message)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;

        var dto = new ParticipantsUpdateDto
        {
            Participants = room.GetParticipants(),
            Message = message
        };

        await Clients.OthersInGroup(roomId).SendAsync("ParticipantsUpdate", dto);
    }

    private async Task RemoveFromRoom(string roomId, string connectionId)
    {
        if (!Rooms.TryGetValue(roomId, out var room))
            return;

        await Groups.RemoveFromGroupAsync(connectionId, roomId);

        ConnectionToRoom.TryRemove(connectionId, out _);

        var leavingLogin = string.Empty;

        lock (room.SyncRoot)
        {
            room.Participants.Remove(connectionId, out var participant);

            if (room.Participants.Count <= 0)
            {
                Rooms.TryRemove(roomId, out _);
                return;
            }

            Rooms[roomId] = room;
            leavingLogin = participant.Login;
        }

        var systemLeaveMessage = new MessageDto
        {
            Sender = new Participant
            {
                Login = "System",
                ImageUrl = string.Empty
            },
            Message = $"Użytkownik {leavingLogin} opuścił pokój.",
            MessageType = MessageType.SYSTEM.ToString(),
            Timestamp = DateTime.UtcNow
        };

        await SendParticipantsList(roomId, systemLeaveMessage);
    }

    private string GetLoginOrThrow()
    {
        var user = Context.User;
        var login = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (login == null)
        {
            throw new HubException("Użytkownik niezalogowany.");
        }

        return login;
    }
}
