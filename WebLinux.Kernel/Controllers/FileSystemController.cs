using Microsoft.AspNetCore.Mvc;
using WebLinux.Kernel.Core;

namespace WebLinux.Kernel.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileSystemController : ControllerBase
{
    [HttpGet("tree")]
    public IActionResult GetTree()
    {
        return Content(FileSystem.ExportTreeJson(), "application/json");
    }

    [HttpGet("read")]
    public IActionResult ReadFile([FromQuery] string path)
    {
        try
        {
            var content = FileSystem.ReadFile(path);
            return Content(content, "text/plain");
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("sync")]
    public IActionResult Sync([FromBody] SyncRequest request)
    {
        if (request?.Files == null || request.Files.Count == 0)
            return Ok(new { synced = 0 });

        FileSystem.SyncFiles(request.Files);
        return Ok(new { synced = request.Files.Count });
    }
}

public class SyncRequest
{
    public List<FileSyncEntry> Files { get; set; } = new();
}
