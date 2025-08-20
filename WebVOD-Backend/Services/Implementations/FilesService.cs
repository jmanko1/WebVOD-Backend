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

    public void DeleteThumbnail(string fileName)
    {
        var filePath = Path.Combine(filesDirectory, "thumbnails", fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public void DeleteVideo(string id)
    {
        var directoryPath = Path.Combine(filesDirectory, "videos", id);

        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    public void DeleteVideoMP4(string id)
    {
        var filePath = Path.Combine(filesDirectory, "videos", id, $"{id}.mp4");

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

    public void MergeVideoChunks(string videoId)
    {
        var chunkDirectoryPath = Path.Combine(filesDirectory, "videos", videoId);
        var outputFilePath = Path.Combine(filesDirectory, "videos", videoId, $"{videoId}.mp4");

        if(!Directory.Exists(chunkDirectoryPath))
        {
            throw new RequestErrorException(400, "Fragmenty filmu nie istnieją.");
        }

        var chunkFiles = Directory.GetFiles(chunkDirectoryPath)
                                  .Where(f => Path.GetFileName(f).All(char.IsDigit))
                                  .OrderBy(f => int.Parse(Path.GetFileName(f)))
                                  .ToList();

        using (var outputStream = File.Create(outputFilePath))
        {
            foreach (var chunkFile in chunkFiles)
            {
                using (var inputStream = File.OpenRead(chunkFile))
                {
                    inputStream.CopyTo(outputStream);
                }
            }
        }

        foreach (var chunkFile in chunkFiles)
        {
            try
            {
                File.Delete(chunkFile);
            }
            catch (Exception ex)
            {
                ;
            }
        }
    }

    public async Task SaveProfileImage(string fileName, IFormFile image)
    {
        var filePath = Path.Combine(filesDirectory, "user_images", fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await image.CopyToAsync(stream);
        }
    }

    public async Task SaveThumbnail(string fileName, IFormFile thumbnail)
    {
        var filePath = Path.Combine(filesDirectory, "thumbnails", fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await thumbnail.CopyToAsync(stream);
        }
    }

    public async Task SaveVideoChunk(string videoId, int chunkIndex, IFormFile videoChunk)
    {
        Directory.CreateDirectory($"{filesDirectory}\\videos\\{videoId}");

        var filePath = Path.Combine(filesDirectory, "videos", videoId, chunkIndex.ToString());

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await videoChunk.CopyToAsync(stream);
        }
    }
}
