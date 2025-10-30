using WebVOD_Backend.Model;

namespace WebVOD_Backend.Repositories.Interfaces;

public interface ITagsPropositionRepository
{
    Task<TagsProposition> FindById(string id);
    Task Add(TagsProposition tagsProposition);
    Task<bool> ExistsByVideoIdAndUserId(string videoId, string userId);
    Task<bool> ExistsById(string id);
    Task<List<TagsProposition>> FindByVideoId(string videoId, int page, int size);
    Task DeleteById(string id);
    Task DeleteByVideoId(string videoId);
}
