using Microsoft.AspNetCore.Mvc;

namespace WebVOD_Backend.Controllers;

[ApiController]
[Route("/uploads")]
public class FilesController : ControllerBase
{
    private readonly string filesDirectory = "G:\\z\\WebVOD";

    [HttpGet("{*fileName}")]
    public IActionResult GetFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return BadRequest("Nie podano nazwy pliku.");

        var filePath = Path.GetFullPath(Path.Combine(filesDirectory, fileName));

        if (!filePath.StartsWith(filesDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = "application/octet-stream";
        var downloadName = Path.GetFileName(filePath);

        return File(stream, contentType, downloadName);
    }
}
