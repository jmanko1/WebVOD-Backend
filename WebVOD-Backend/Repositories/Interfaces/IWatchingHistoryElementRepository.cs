using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface IWatchingHistoryElementRepository
{
    Task Add(WatchingHistoryElement watchingHistoryElement);
    Task<bool> ExistsByVideoIdAndViewerId(string videoId, string viewerId);
    Task UpdateViewedAt(string videoId, string viewerId);
    Task<List<WatchingHistoryElement>> FindByViewerId(string viewerId, int page, int size);
    Task DeleteByVideoId(string videoId);
}
