using Microsoft.AspNetCore.Mvc;

namespace WebVOD_Backend.Services.Interfaces;

public interface IFilesService
{
    FileStreamResult GetFile(string fileName);
    Task SaveProfileImage(string fileName, IFormFile image);
    void DeleteProfileImage(string fileName);
    Task SaveThumbnail(string fileName, IFormFile thumbnail);
    void DeleteThumbnail(string fileName);
    Task SaveVideoChunk(string videoId, int chunkIndex, IFormFile videoChunk);
    void MergeVideoChunks(string videoId);
    void DeleteVideo(string fileName);
}
