using Microsoft.AspNetCore.Mvc;
using WebLinux.Kernel.Core;

namespace WebLinux.Kernel.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TerminalController : ControllerBase
{
    [HttpPost]
    public IActionResult Execute(CommandRequest request)
    {
        return Ok(
            Shell.Execute(request.Command)
        );
    }
}


public class CommandRequest
{
    public string Command { get; set; } = "";
}