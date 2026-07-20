using System.Diagnostics;
using System.Text;

namespace WebLinux.Kernel.Core;

public class PythonRunner : IDisposable
{
    private Process? process;
    private readonly StringBuilder outputBuffer = new();
    private readonly object lockObj = new();
    private readonly TaskCompletionSource<string> outputReady = new();
    private bool disposed;
    private string pythonCmd = "python3";

    public bool IsRunning => process != null && !process.HasExited;

    public PythonRunner()
    {
        pythonCmd = FindPython();
    }

    public static string FindPython()
    {
        foreach (var cmd in new[] { "python3", "python", "py" })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    p.WaitForExit(5000);
                    if (p.ExitCode == 0) return cmd;
                }
            }
            catch { }
        }
        return "python3";
    }

    public string GetVersion()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonCmd,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return "python: not found";
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(5000);
            return string.IsNullOrEmpty(output) ? "python: not found" : output;
        }
        catch (Exception e)
        {
            return $"python: {e.Message}";
        }
    }

    public string GetPipVersion()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonCmd,
                Arguments = "-m pip --version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return "pip: not found";
            var output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit(10000);
            return string.IsNullOrEmpty(output) ? p.StandardError.ReadToEnd().Trim() : output;
        }
        catch (Exception e)
        {
            return $"pip: {e.Message}";
        }
    }

    public void Start(string workingDir)
    {
        lock (lockObj)
        {
            Stop();

            var psi = new ProcessStartInfo
            {
                FileName = pythonCmd,
                Arguments = "-i",
                WorkingDirectory = workingDir,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += OnOutputData;
            process.ErrorDataReceived += OnErrorData;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Thread.Sleep(300);
        }
    }

    private void OnOutputData(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (lockObj)
            {
                outputBuffer.AppendLine(e.Data);
            }
        }
    }

    private void OnErrorData(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (lockObj)
            {
                outputBuffer.AppendLine(e.Data);
            }
        }
    }

    public string Execute(string code)
    {
        lock (lockObj)
        {
            outputBuffer.Clear();
        }

        if (!IsRunning) return "Error: Python process not running";

        try
        {
            process!.StandardInput.WriteLine(code);
            process.StandardInput.Flush();
        }
        catch (Exception e)
        {
            return $"Error: {e.Message}";
        }

        Thread.Sleep(200);

        for (int i = 0; i < 50; i++)
        {
            string current;
            lock (lockObj)
            {
                current = outputBuffer.ToString();
            }

            if (current.Contains(">>> ") || current.Contains("... "))
                break;

            Thread.Sleep(50);
        }

        string result;
        lock (lockObj)
        {
            result = outputBuffer.ToString().TrimEnd();
            outputBuffer.Clear();
        }

        return result;
    }

    public string ExecuteBatch(string[] lines, string workingDir)
    {
        var combined = string.Join("\n", lines);
        var psi = new ProcessStartInfo
        {
            FileName = pythonCmd,
            Arguments = "-c -",
            WorkingDirectory = workingDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var p = Process.Start(psi);
        if (p == null) return "python: not found";

        p.StandardInput.Write(combined);
        p.StandardInput.Close();

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(30000);

        var output = stdout.TrimEnd();
        if (!string.IsNullOrEmpty(stderr))
            output += (output.Length > 0 ? "\n" : "") + stderr.TrimEnd();

        return output;
    }

    public void Stop()
    {
        try
        {
            if (process != null && !process.HasExited)
            {
                process.StandardInput.Close();
                process.WaitForExit(2000);
                if (!process.HasExited)
                    process.Kill();
            }
        }
        catch { }
        finally
        {
            process?.Dispose();
            process = null;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Stop();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
