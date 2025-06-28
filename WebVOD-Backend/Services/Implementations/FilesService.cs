using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Services.Implementations;

public class FilesService : IFilesService
{
    private readonly string filesDirectory = "G:\\z\\WebVOD";

    public void DeleteProfileImage(string fileName)
    {
        var filePath = Path.Combine(filesDirectory, "user_images", fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public FileStreamResult GetFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new RequestErrorException(400, "Nie podano nazwy pliku.");

        var filePath = Path.GetFullPath(Path.Combine(filesDirectory, fileName));

        if (!filePath.StartsWith(filesDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestErrorException(400, "Niepoprawna ścieżka pliku.");
        }

        if (!File.Exists(filePath))
        {
            throw new RequestErrorException(404, "Plik nie istnieje.");
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = "application/octet-stream";
        var downloadName = Path.GetFileName(filePath);

        return new FileStreamResult(stream, contentType)
        {
            FileDownloadName = downloadName
        };
    }

    public async Task SaveProfileImage(string fileName, IFormFile image)
    {
        var filePath = Path.Combine(filesDirectory, "user_images", fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }
    }
}
