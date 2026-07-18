namespace WebLinux.Kernel.Core;

public class TerminalEntry
{
    public string Path { get; set; } = "~";

    public string Command { get; set; } = "";

    public string Output { get; set; } = "";
}