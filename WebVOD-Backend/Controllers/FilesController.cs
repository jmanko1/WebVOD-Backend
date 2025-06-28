using Microsoft.AspNetCore.Mvc;
using WebVOD_Backend.Exceptions;
using WebVOD_Backend.Services.Interfaces;

namespace WebVOD_Backend.Controllers;

[ApiController]
[Route("/uploads")]
public class FilesController : ControllerBase
{
    private readonly IFilesService _filesService;

    public FilesController(IFilesService filesService)
    {
        _filesService = filesService;
    }

    [HttpGet("{*fileName}")]
    public IActionResult GetFile(string fileName)
    {
        try
        {
            var file = _filesService.GetFile(fileName);
            return file;
        }
        catch (RequestErrorException ex)
        {
            return StatusCode(ex.StatusCode, new { ex.Message });
        }
    }
}
