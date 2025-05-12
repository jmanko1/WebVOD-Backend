using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IUserDeviceRepository
{
    Task Add(UserDevice device);
    Task UpdateDates(string id, DateTime registeredAt, DateTime validUntil);
    Task<UserDevice> FindByNameAndUserId(string name, string userId);
}
