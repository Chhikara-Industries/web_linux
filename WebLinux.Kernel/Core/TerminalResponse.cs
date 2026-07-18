namespace WebLinux.Kernel.Core;

public class TerminalResponse
{
    public string Mode { get; set; } = "shell";
    public string Output { get; set; } = "";
    public string CurrentPath { get; set; } = "~";

    public List<TerminalEntry> History { get; set; } = new();
    
    public string? File { get; set; }
    public string? Language { get; set; }
    public string? Content { get; set; }
}
