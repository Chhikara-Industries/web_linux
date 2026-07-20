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
            Shell.Execute(request.Command, request.Mode)
        );
    }
}


public class CommandRequest
{
    public string Command { get; set; } = "";
    public string Mode { get; set; } = "shell";
}
