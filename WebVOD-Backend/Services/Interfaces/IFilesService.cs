using Microsoft.AspNetCore.Mvc;

namespace WebVOD_Backend.Services.Interfaces;

public interface IFilesService
{
    FileStreamResult GetFile(string fileName);
    Task SaveProfileImage(string fileName, IFormFile image);
    void DeleteProfileImage(string fileName);
}
