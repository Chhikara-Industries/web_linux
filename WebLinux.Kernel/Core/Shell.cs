namespace WebLinux.Kernel.Core;

public class Shell
{
    private static readonly Session session = new Session();
    private static int processCounter = 1000;

    public static TerminalResponse Execute(string command)
    {
        var trimmed = command.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return new TerminalResponse
            {
                Mode = "shell",
                CurrentPath = session.CurrentPath,
                History = session.History
            };
        }

        string output = "";

        var pipeIdx = trimmed.IndexOf('|');
        if (pipeIdx > 0)
        {
            var left = trimmed[..pipeIdx].Trim();
            var right = trimmed[(pipeIdx + 1)..].Trim();
            var leftResult = ExecuteSingle(left);
            var rightParts = right.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (rightParts.Length > 0)
            {
                output = PipeCommand(rightParts[0], rightParts.Skip(1).ToArray(), leftResult);
            }
            session.AddHistory(command, output);
            return new TerminalResponse
            {
                Mode = "shell",
                Output = output,
                CurrentPath = session.CurrentPath,
                History = session.History
            };
        }

        if (trimmed.StartsWith("code ") || trimmed == "code")
        {
            var codeParts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (codeParts.Length < 2)
            {
                session.AddHistory(command, "code: missing file operand");
                return new TerminalResponse
                {
                    Mode = "shell",
                    Output = "code: missing file operand",
                    CurrentPath = session.CurrentPath,
                    History = session.History
                };
            }

            var path = ResolveStaticPath(codeParts[1]);
            string content = "";
            string lang = "plaintext";
            try
            {
                content = FileSystem.ReadFile(path);
                lang = GetLanguageFromFilename(codeParts[1]);
            }
            catch (FileNotFoundException) { content = ""; }

            session.AddHistory(command, "");
            return new TerminalResponse
            {
                Mode = "editor",
                File = codeParts[1],
                Language = lang,
                Content = content,
                CurrentPath = session.CurrentPath,
                History = session.History
            };
        }

        output = ExecuteSingle(trimmed);
        session.AddHistory(command, output);
        return new TerminalResponse
        {
            Mode = "shell",
            Output = output,
            CurrentPath = session.CurrentPath,
            History = session.History
        };
    }

    private static string ExecuteSingle(string trimmed)
    {
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";

        var cmd = parts[0];
        var args = parts.Skip(1).ToArray();

        return cmd switch
        {
            "ls" => CmdLs(args),
            "cd" => CmdCd(args),
            "pwd" => CmdPwd(),
            "cat" => CmdCat(args),
            "echo" => CmdEcho(args),
            "touch" => CmdTouch(args),
            "mkdir" => CmdMkdir(args),
            "rm" => CmdRm(args),
            "cp" => CmdCp(args),
            "mv" => CmdMv(args),
            "clear" => "\x1b[2J\x1b[H",
            "whoami" => "user",
            "hostname" => "weblinux",
            "date" => DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy"),
            "uname" => CmdUname(args),
            "uptime" => CmdUptime(),
            "help" => CmdHelp(),
            "history" => CmdHistory(),
            "head" => CmdHead(args),
            "tail" => CmdTail(args),
            "wc" => CmdWc(args),
            "grep" => CmdGrep(args),
            "find" => CmdFind(args),
            "chmod" => CmdChmod(args),
            "chown" => "chown: operation not permitted (virtual filesystem)",
            "man" => CmdMan(args),
            "which" => CmdWhich(args),
            "type" => CmdType(args),
            "env" => CmdEnv(),
            "export" => CmdExport(args),
            "unset" => CmdUnset(args),
            "df" => CmdDf(),
            "du" => CmdDu(args),
            "free" => CmdFree(),
            "ps" => CmdPs(),
            "kill" => CmdKill(args),
            "top" => "top: simulated mode not available in WebLinux",
            "id" => "uid=1000(user) gid=1000(user) groups=1000(user)",
            "groups" => "user",
            "who" => $"user     pts/0        {DateTime.Now:yyyy-MM-dd HH:mm} (weblinux)",
            "w" => $"USER     TTY      FROM             LOGIN@   IDLE   JCPU   PCPU WHAT\nuser     pts/0    weblinux         {DateTime.Now:HH:mm}   0.00s  0.01s  0.00s w",
            "last" => "user     pts/0        weblinux         " + DateTime.Now.ToString("ddd MMM dd HH:mm") + "   still logged in",
            "logname" => "user",
            "sleep" => "",
            "true" => "",
            "false" => "",
            "seq" => CmdSeq(args),
            "lsblk" => "NAME   MAJ:MIN RM  SIZE RO TYPE MOUNTPOINTS\nvda    254:0    0   50G  0 disk /\nsr1    11:0     1 1024M  0 rom",
            "lscpu" => "Architecture:        x86_64\nCPU op-mode(s):      32-bit, 64-bit\nModel name:          WebLinux Virtual CPU\nCPU MHz:             3600.000\nCPU(s):              4",
            "lsusb" => "Bus 001 Device 001: ID 1d6b:0002 Linux Foundation 2.0 root hub",
            "lsmod" => "Module                  Size  Used by",
            "dmesg" => $"[    0.000000] Linux version 6.1.0-weblinux\n[{DateTime.Now:ss.ffffff}] WebLinux system ready",
            "mount" => "/dev/vda1 on / type ext4 (rw,relatime)\nproc on /proc type proc (rw,nosuid,nodev,noexec,relatime)",
            "umount" => "umount: not simulated",
            "fdisk" => "fdisk: use 'lsblk' to view disk info",
            "ping" => CmdPing(args),
            "curl" => "curl: simulated - use the browser for HTTP requests",
            "wget" => "wget: simulated - use the browser for downloads",
            "ip" => CmdIp(args),
            "ifconfig" => "eth0: flags=4163<UP,BROADCAST,RUNNING,MULTICAST>  mtu 1500\n        inet 192.168.1.100  netmask 255.255.255.0  broadcast 192.168.1.255\n        ether 02:42:ac:11:00:02  txqueuelen 0\nlo: flags=73<UP,LOOPBACK,RUNNING>  mtu 65536\n        inet 127.0.0.1  netmask 255.0.0.0",
            "netstat" => "Active Internet connections (servers and established)\nProto Recv-Q Send-Q Local Address           Foreign Address         State\ntcp        0      0 0.0.0.0:5245            0.0.0.0:*               LISTEN",
            "ss" => "Netid  State   Recv-Q  Send-Q   Local Address:Port    Peer Address:Port  Process\ntcp    LISTEN  0       128      0.0.0.0:5245          0.0.0.0:*",
            "tar" => CmdTar(args),
            "zip" => CmdZip(args),
            "unzip" => CmdUnzip(args),
            "git" => CmdGit(args),
            "node" => "Welcome to Node.js v20.11.0.\nType \".help\" for more information.\n> WebLinux simulated Node.js",
            "npm" => CmdNpm(args),
            "python" => "Python 3.11.6 (main, Oct  2 2023, 00:00:00)\n[GCC 12.3.0] on linux\nType \"help\" for more info.\n>>> WebLinux simulated Python",
            "python3" => "Python 3.11.6 (main, Oct  2 2023, 00:00:00)\n[GCC 12.3.0] on linux\nType \"help\" for more info.\n>>> WebLinux simulated Python",
            "pip" => CmdPip(args),
            "pip3" => CmdPip(args),
            "gcc" => "gcc: simulated compilation - no actual compiler available\ngcc: fatal error: no input files\ncompilation terminated.",
            "g++" => "g++: simulated compilation - no actual compiler available\ng++: fatal error: no input files\ncompilation terminated.",
            "make" => "make: Nothing to be done for 'all'.",
            "vim" => "vim: simulated - use 'code <file>' to open editor",
            "vi" => "vi: simulated - use 'code <file>' to open editor",
            "nano" => "nano: simulated - use 'code <file>' to open editor",
            "emacs" => "emacs: simulated - use 'code <file>' to open editor",
            "less" => CmdCat(args),
            "more" => CmdCat(args),
            "tree" => CmdTree(args),
            "stat" => CmdStat(args),
            "diff" => "diff: requires two files as arguments",
            "ln" => "ln: symbolic links not supported in WebLinux",
            "readlink" => "readlink: symbolic links not supported in WebLinux",
            "realpath" => CmdRealpath(args),
            "dirname" => args.Length > 0 ? Path.GetDirectoryName(args[0])?.Replace('\\', '/') ?? "." : "dirname: missing operand",
            "basename" => args.Length > 0 ? Path.GetFileName(args[0]) : "basename: missing operand",
            "nl" => CmdNl(args),
            "rev" => CmdRev(args),
            "sort" => "sort: simulated - pipe output to sort",
            "uniq" => "uniq: simulated - pipe output to uniq",
            "cut" => "cut: simulated - pipe output to cut",
            "tr" => "tr: simulated - pipe output to tr",
            "tee" => "tee: simulated - pipe output to tee",
            "xargs" => "xargs: simulated",
            "awk" => "awk: simulated - use grep instead",
            "sed" => "sed: simulated - use grep instead",
            "jq" => "jq: simulated - JSON processor not available",
            "screen" => "screen: simulated terminal multiplexer not available",
            "tmux" => "tmux: simulated terminal multiplexer not available",
            "ssh" => "ssh: simulated - no SSH server available",
            "scp" => "scp: simulated - no SSH server available",
            "rsync" => "rsync: simulated - no rsync server available",
            "docker" => "docker: simulated - no Docker daemon available",
            "systemctl" => CmdSystemctl(args),
            "service" => "Usage: service <service> <command>",
            "journalctl" => $"-- Logs begin at {DateTime.Now:yyyy-MM-dd HH:mm:ss}, end now.\n[{DateTime.Now:HH:mm:ss}] weblinux systemd[1]: Started WebLinux.",
            "crontab" => "no crontab for user",
            "at" => "at: simulated - no atd available",
            "shutdown" => "System is going down for poweroff NOW!\nWebLinux cannot be shut down.",
            "reboot" => "System is going down for reboot NOW!\nWebLinux cannot be rebooted.",
            "halt" => "System is going down for halt NOW!",
            "poweroff" => "System is going down for poweroff NOW!",
            "su" => "su: authentication not simulated",
            "sudo" => "user is not in the sudoers file. This incident will be reported.",
            "passwd" => "Changing password for user.\n(current) Unix password: \npasswd: authentication not simulated",
            "useradd" => "useradd: simulated - user management not available",
            "userdel" => "userdel: simulated - user management not available",
            "adduser" => "adduser: simulated - user management not available",
            "deluser" => "deluser: simulated - user management not available",
            "groupadd" => "groupadd: simulated - group management not available",
            "groupdel" => "groupdel: simulated - group management not available",
            "newgrp" => "newgrp: simulated - group management not available",
            "apt" => CmdApt(args),
            "apt-get" => CmdApt(args),
            "mktemp" => $"/tmp/tmp.{Guid.NewGuid().ToString("N")[..8]}",
            "file" => "file: simulated",
            "strings" => "strings: simulated",
            "od" => "od: simulated",
            "hexdump" => "hexdump: simulated",
            "xxd" => "xxd: simulated",
            "base64" => "base64: simulated",
            "openssl" => "openssl: simulated",
            "gpg" => "gpg: simulated",
            "shred" => "shred: simulated",
            "sync" => "sync: simulated",
            "lspci" => "00:00.0 Host bridge: WebLinux Virtual Host Bridge",
            "blkid" => "/dev/vda1: UUID=\"weblinux-1234\" TYPE=\"ext4\"",
            "fsck" => "fsck: simulated",
            "mkfs" => "mkfs: simulated",
            "hdparm" => "hdparm: simulated",
            "smartctl" => "smartctl: simulated",
            "dmidecode" => "dmidecode: simulated",
            "lshw" => "lshw: simulated - use 'lscpu' for CPU info",
            _ => $"bash: {cmd}: command not found"
        };
    }

    private static string PipeCommand(string cmd, string[] args, string input)
    {
        return cmd switch
        {
            "grep" => PipeGrep(args, input),
            "wc" => PipeWc(input),
            "head" => PipeHead(args, input),
            "tail" => PipeTail(args, input),
            "sort" => string.Join("\n", input.Split('\n').OrderBy(x => x)),
            "uniq" => string.Join("\n", input.Split('\n').Distinct()),
            "rev" => string.Join("\n", input.Split('\n').Select(l => new string(l.Reverse().ToArray()))),
            "cat" => input,
            _ => input
        };
    }

    private static string PipeGrep(string[] args, string input)
    {
        var pattern = args.FirstOrDefault(a => !a.StartsWith('-')) ?? "";
        var lines = input.Split('\n');
        var ignoreCase = args.Any(a => a.Contains('i'));

        var matches = ignoreCase
            ? lines.Where(l => l.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            : lines.Where(l => l.Contains(pattern));

        return string.Join("\n", matches);
    }

    private static string PipeWc(string input)
    {
        var lines = input.Split('\n');
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var chars = input.Length;
        return $"  {lines.Length}   {words} {chars}";
    }

    private static string PipeHead(string[] args, string input)
    {
        int n = 10;
        var nIdx = Array.IndexOf(args, "-n");
        if (nIdx >= 0 && nIdx + 1 < args.Length) int.TryParse(args[nIdx + 1], out n);
        else if (args.Length > 0 && int.TryParse(args[0].TrimStart('-'), out var parsed)) n = parsed;
        return string.Join("\n", input.Split('\n').Take(n));
    }

    private static string PipeTail(string[] args, string input)
    {
        int n = 10;
        var nIdx = Array.IndexOf(args, "-n");
        if (nIdx >= 0 && nIdx + 1 < args.Length) int.TryParse(args[nIdx + 1], out n);
        else if (args.Length > 0 && int.TryParse(args[0].TrimStart('-'), out var parsed)) n = parsed;
        return string.Join("\n", input.Split('\n').TakeLast(n));
    }

    private static string ResolveStaticPath(string path)
    {
        if (path.StartsWith("~/"))
            return "/home/user/" + path[2..];
        if (path == "~")
            return "/home/user";
        if (path.StartsWith("/"))
            return path;
        return session.CurrentPath + "/" + path;
    }

    private static string ResolvePath(string path)
    {
        return ResolveStaticPath(path);
    }

    private static string GetLanguageFromFilename(string filename)
    {
        return filename switch
        {
            var f when f.EndsWith(".py") => "python",
            var f when f.EndsWith(".js") => "javascript",
            var f when f.EndsWith(".ts") => "typescript",
            var f when f.EndsWith(".html") || f.EndsWith(".htm") => "html",
            var f when f.EndsWith(".css") => "css",
            var f when f.EndsWith(".json") => "json",
            var f when f.EndsWith(".xml") => "xml",
            var f when f.EndsWith(".md") => "markdown",
            var f when f.EndsWith(".yml") || f.EndsWith(".yaml") => "yaml",
            var f when f.EndsWith(".cs") => "csharp",
            var f when f.EndsWith(".java") => "java",
            var f when f.EndsWith(".go") => "go",
            var f when f.EndsWith(".rs") => "rust",
            var f when f.EndsWith(".rb") => "ruby",
            var f when f.EndsWith(".php") => "php",
            var f when f.EndsWith(".sql") => "sql",
            var f when f.EndsWith(".sh") || f.EndsWith(".bash") => "shell",
            var f when f.EndsWith(".c") || f.EndsWith(".h") => "c",
            var f when f.EndsWith(".cpp") || f.EndsWith(".cc") => "cpp",
            var f when f.EndsWith(".txt") => "plaintext",
            _ => "plaintext"
        };
    }

    private static string CmdLs(string[] args)
    {
        bool showAll = args.Contains("-a") || args.Contains("-la") || args.Contains("-al");
        bool longFormat = args.Contains("-l") || args.Contains("-la") || args.Contains("-al");
        bool almostAll = args.Contains("-A");

        string target = session.CurrentPath;
        var nonFlags = args.Where(a => !a.StartsWith('-')).ToArray();
        if (nonFlags.Length > 0)
            target = ResolvePath(nonFlags[0]);

        if (!FileSystem.PathExists(target))
            return $"ls: cannot access '{nonFlags.FirstOrDefault() ?? target}': No such file or directory";

        var result = FileSystem.List(target, showAll || almostAll, longFormat);

        if (string.IsNullOrEmpty(result) && (showAll || almostAll))
        {
            if (longFormat)
                return $"total 0\n drwxr-xr-x 1 user user  4096 {DateTime.Now:MMM dd HH:mm} .\ndrwxr-xr-x 1 user user  4096 {DateTime.Now:MMM dd HH:mm} ..";
            return ". ..";
        }

        return result;
    }

    private static string CmdCd(string[] args)
    {
        var target = args.Length > 0 ? args[0] : "~";
        return session.ChangeDirectory(target);
    }

    private static string CmdPwd() => session.CurrentPath;

    private static string CmdCat(string[] args)
    {
        if (args.Length == 0) return "cat: missing file operand";

        var output = new List<string>();
        foreach (var arg in args)
        {
            if (arg.StartsWith('-')) continue;
            var path = ResolvePath(arg);
            try
            {
                output.Add(FileSystem.ReadFile(path));
            }
            catch (Exception ex)
            {
                output.Add(ex.Message);
            }
        }
        return string.Join("\n", output);
    }

    private static string CmdEcho(string[] args)
    {
        var result = string.Join(" ", args);
        if (result.StartsWith('\'') && result.EndsWith('\''))
            result = result[1..^1];
        else if (result.StartsWith('"') && result.EndsWith('"'))
            result = result[1..^1];

        result = result.Replace("\\n", "\n").Replace("\\t", "\t");
        return result;
    }

    private static string CmdTouch(string[] args)
    {
        if (args.Length == 0) return "touch: missing file operand";
        foreach (var arg in args)
        {
            if (arg.StartsWith('-')) continue;
            var path = ResolvePath(arg);
            if (!FileSystem.FileExists(path))
                FileSystem.WriteFile(path, "");
        }
        return "";
    }

    private static string CmdMkdir(string[] args)
    {
        if (args.Length == 0) return "mkdir: missing operand";
        foreach (var arg in args)
        {
            if (arg.StartsWith('-')) continue;
            var path = ResolvePath(arg);
            if (!FileSystem.CreateDirectory(path))
                return $"mkdir: cannot create directory '{arg}': No such file or directory";
        }
        return "";
    }

    private static string CmdRm(string[] args)
    {
        if (args.Length == 0) return "rm: missing operand";
        bool recursive = args.Contains("-r") || args.Contains("-rf") || args.Contains("-fr");
        bool force = args.Contains("-f") || args.Contains("-rf") || args.Contains("-fr");

        foreach (var arg in args)
        {
            if (arg.StartsWith('-')) continue;
            var path = ResolvePath(arg);
            if (!FileSystem.Remove(path, recursive))
            {
                if (!force)
                    return $"rm: cannot remove '{arg}': No such file or directory";
            }
        }
        return "";
    }

    private static string CmdCp(string[] args)
    {
        var nonFlags = args.Where(a => !a.StartsWith('-')).ToArray();
        if (nonFlags.Length < 2) return "cp: missing file operand";
        var src = ResolvePath(nonFlags[0]);
        var dst = ResolvePath(nonFlags[1]);
        if (!FileSystem.Copy(src, dst))
            return $"cp: cannot copy '{nonFlags[0]}' to '{nonFlags[1]}'";
        return "";
    }

    private static string CmdMv(string[] args)
    {
        var nonFlags = args.Where(a => !a.StartsWith('-')).ToArray();
        if (nonFlags.Length < 2) return "mv: missing file operand";
        var src = ResolvePath(nonFlags[0]);
        var dst = ResolvePath(nonFlags[1]);
        if (!FileSystem.Move(src, dst))
            return $"mv: cannot move '{nonFlags[0]}' to '{nonFlags[1]}'";
        return "";
    }

    private static string CmdUname(string[] args)
    {
        if (args.Contains("-a"))
            return "Linux weblinux 6.1.0-weblinux #1 SMP x86_64 GNU/Linux";
        if (args.Contains("-r")) return "6.1.0-weblinux";
        if (args.Contains("-n")) return "weblinux";
        if (args.Contains("-m")) return "x86_64";
        if (args.Contains("-s")) return "Linux";
        return "Linux";
    }

    private static string CmdUptime()
    {
        var up = DateTime.Now - new DateTime(2026, 1, 1);
        var hours = (int)up.TotalHours;
        var mins = up.Minutes;
        return $" {DateTime.Now:HH:mm:ss} up {hours}:{mins:D2},  1 user,  load average: 0.00, 0.01, 0.05";
    }

    private static string CmdHelp()
    {
        return "WebLinux Terminal - Available Commands\n\n" +
            "File Operations:\n" +
            "  ls [-l] [-a] [dir]    List directory contents\n" +
            "  cd [dir]              Change directory\n" +
            "  pwd                   Print working directory\n" +
            "  cat <file>            Display file contents\n" +
            "  touch <file>          Create empty file\n" +
            "  mkdir <dir>           Create directory\n" +
            "  rm [-rf] <file>       Remove file or directory\n" +
            "  cp <src> <dst>        Copy file\n" +
            "  mv <src> <dst>        Move/rename file\n" +
            "  head [-n N] <file>    Show first N lines\n" +
            "  tail [-n N] <file>    Show last N lines\n" +
            "  wc <file>             Word, line, char count\n" +
            "  grep <pattern> <file> Search file contents\n" +
            "  find <dir> <pattern>  Find files by name\n" +
            "  chmod <perm> <file>   Change file permissions\n" +
            "  stat <file>           Show file info\n" +
            "  tree [dir]            Show directory tree\n\n" +
            "Text Processing:\n" +
            "  echo <text>           Print text\n" +
            "  nl <file>             Number lines\n" +
            "  rev <file>            Reverse lines\n\n" +
            "System Info:\n" +
            "  whoami                Current user\n" +
            "  hostname              System hostname\n" +
            "  uname [-a]            System information\n" +
            "  uptime                System uptime\n" +
            "  date                  Current date/time\n" +
            "  id                    User/group IDs\n" +
            "  df                    Disk usage\n" +
            "  du <dir>              Directory size\n" +
            "  free                  Memory usage\n" +
            "  ps                    Running processes\n" +
            "  lscpu                 CPU information\n" +
            "  lsblk                 Block devices\n" +
            "  env                   Environment variables\n" +
            "  export KEY=VALUE      Set environment variable\n" +
            "  unset KEY             Remove environment variable\n\n" +
            "Network:\n" +
            "  ping <host>           Ping host\n" +
            "  ip [addr|route]       Network info\n" +
            "  ifconfig              Network interfaces\n\n" +
            "Package Management:\n" +
            "  apt <cmd>             Package manager (simulated)\n" +
            "  npm <cmd>             Node package manager (simulated)\n" +
            "  pip <cmd>             Python package manager (simulated)\n" +
            "  git <cmd>             Git version control (simulated)\n\n" +
            "Process Management:\n" +
            "  ps                    List processes\n" +
            "  kill <pid>            Kill process\n\n" +
            "Utilities:\n" +
            "  clear                 Clear terminal\n" +
            "  history               Command history\n" +
            "  code <file>           Open file in editor\n" +
            "  man <cmd>             Manual page\n" +
            "  which <cmd>           Locate command\n" +
            "  type <cmd>            Command type\n" +
            "  sleep <sec>           Sleep\n" +
            "  seq <start> <end>     Number sequence\n" +
            "  mktemp                Create temp file\n" +
            "  dirname <path>        Directory name\n" +
            "  basename <path>       Base filename\n" +
            "  realpath <path>       Resolve full path\n\n" +
            "Archives:\n" +
            "  tar                   Archive (simulated)\n" +
            "  zip                   Compress (simulated)\n" +
            "  unzip                 Decompress (simulated)\n\n" +
            "Services:\n" +
            "  systemctl <cmd>       System services\n" +
            "  journalctl            System logs";
    }



    private static string CmdHistory()
    {
        return string.Join("\n", session.History.Select((e, i) => $"  {i + 1}  {e.Command}"));
    }

    private static string CmdHead(string[] args)
    {
        int n = 10;
        var nonFlags = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-n" && i + 1 < args.Length)
                int.TryParse(args[++i], out n);
            else if (!args[i].StartsWith('-'))
                nonFlags.Add(args[i]);
        }

        if (nonFlags.Count == 0) return "head: missing file operand";
        var path = ResolvePath(nonFlags[0]);
        try
        {
            var content = FileSystem.ReadFile(path);
            return string.Join("\n", content.Split('\n').Take(n));
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string CmdTail(string[] args)
    {
        int n = 10;
        var nonFlags = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-n" && i + 1 < args.Length)
                int.TryParse(args[++i], out n);
            else if (!args[i].StartsWith('-'))
                nonFlags.Add(args[i]);
        }

        if (nonFlags.Count == 0) return "tail: missing file operand";
        var path = ResolvePath(nonFlags[0]);
        try
        {
            var content = FileSystem.ReadFile(path);
            return string.Join("\n", content.Split('\n').TakeLast(n));
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string CmdWc(string[] args)
    {
        if (args.Length == 0) return "wc: missing file operand";
        var path = ResolvePath(args[0]);
        try
        {
            var content = FileSystem.ReadFile(path);
            var lines = content.Split('\n').Length;
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var chars = content.Length;
            return $"  {lines}   {words} {chars} {args[0]}";
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string CmdGrep(string[] args)
    {
        if (args.Length < 2) return "usage: grep <pattern> <file>";
        string pattern = args[0];
        var path = ResolvePath(args[1]);
        try
        {
            var result = FileSystem.Grep(path, pattern);
            return string.IsNullOrEmpty(result) ? "(no matches)" : result;
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string CmdFind(string[] args)
    {
        var startPath = args.Length > 0 ? ResolvePath(args[0]) : session.CurrentPath;
        var pattern = args.Length > 1 ? args[1] : "";
        var results = FileSystem.FindFiles(startPath, pattern);
        if (results.Count == 0) return "(no results)";
        return string.Join("\n", results);
    }

    private static string CmdChmod(string[] args)
    {
        if (args.Length < 2) return "chmod: missing operand";
        var path = ResolvePath(args[1]);
        if (!FileSystem.SetNodePermissions(path, args[0]))
            return $"chmod: cannot access '{args[1]}': No such file or directory";
        return "";
    }

    private static string CmdMan(string[] args)
    {
        if (args.Length == 0) return "What manual page do you want?";
        return $"No manual entry for {args[0]}\nSee 'help' for available commands.";
    }

    private static string CmdWhich(string[] args)
    {
        if (args.Length == 0) return "which: missing argument";
        var builtins = new[] { "ls", "cd", "pwd", "cat", "echo", "touch", "mkdir", "rm", "cp", "mv", "grep", "find", "head", "tail", "wc", "chmod", "clear", "help", "history", "code", "whoami", "hostname", "date", "uname", "uptime", "df", "du", "free", "ps", "kill", "env", "export", "ping", "git", "node", "python", "pip", "npm", "gcc", "make", "tar", "zip", "unzip", "tree", "stat", "diff" };
        if (builtins.Contains(args[0])) return $"/usr/bin/{args[0]}";
        return $"{args[0]} not found";
    }

    private static string CmdType(string[] args)
    {
        if (args.Length == 0) return "type: missing argument";
        return $"{args[0]} is /usr/bin/{args[0]}";
    }

    private static string CmdEnv()
    {
        return string.Join("\n", session.Environment.Select(e => $"{e.Key}={e.Value}"));
    }

    private static string CmdExport(string[] args)
    {
        if (args.Length == 0) return CmdEnv();
        var assignment = args[0];
        var eqIdx = assignment.IndexOf('=');
        if (eqIdx > 0)
        {
            session.Environment[assignment[..eqIdx]] = assignment[(eqIdx + 1)..];
            return "";
        }
        return "export: invalid syntax. Usage: export KEY=VALUE";
    }

    private static string CmdUnset(string[] args)
    {
        if (args.Length == 0) return "unset: missing variable name";
        session.Environment.Remove(args[0]);
        return "";
    }

    private static string CmdDf()
    {
        return "Filesystem     1K-blocks    Used Available Use% Mounted on\n" +
            "/dev/vda1       51200000 2048000  46976000   5% /\n" +
            "tmpfs            8192000       0   8192000   0% /dev/shm\n" +
            "tmpfs            8192000       0   8192000   0% /tmp";
    }

    private static string CmdDu(string[] args)
    {
        var path = args.Length > 0 ? ResolvePath(args[0]) : session.CurrentPath;
        var size = FileSystem.GetDirectorySize(path);
        var kb = size / 1024;
        return $"  {kb}\t{(args.Length > 0 ? args[0] : ".")}";
    }

    private static string CmdFree()
    {
        return "               total        used        free      shared  buff/cache   available\n" +
            "Mem:       16384000     4096000     8192000      512000     4096000    12288000\n" +
            "Swap:       2097152           0     2097152";
    }

    private static string CmdPs()
    {
        return $"  PID TTY          TIME CMD\n    1 pts/0    00:00:00 bash\n{processCounter} pts/0    00:00:00 ps";
    }

    private static string CmdKill(string[] args)
    {
        if (args.Length == 0) return "kill: missing PID";
        var pid = args[^1];
        if (pid == "1") return "kill: cannot kill PID 1: Operation not permitted";
        return $"bash: kill: ({pid}) - No such process";
    }

    private static string CmdPing(string[] args)
    {
        if (args.Length == 0) return "ping: missing host";
        var host = args[0];
        var lines = new List<string> { $"PING {host} (127.0.0.1) 56(84) bytes of data." };
        for (int i = 0; i < 4; i++)
        {
            var ms = (new Random().NextDouble() * 10 + 1).ToString("F1");
            lines.Add($"64 bytes from 127.0.0.1: icmp_seq={i + 1} ttl=64 time={ms} ms");
        }
        lines.Add($"--- {host} ping statistics ---");
        lines.Add("4 packets transmitted, 4 received, 0% packet loss, time 3003ms");
        return string.Join("\n", lines);
    }

    private static string CmdIp(string[] args)
    {
        if (args.Length == 0 || args[0] == "addr" || args[0] == "a")
            return "1: lo: <LOOPBACK,UP,LOWER_UP> mtu 65536 qdisc noqueue state UNKNOWN\n" +
                "    inet 127.0.0.1/8 scope host lo\n" +
                "2: eth0: <BROADCAST,MULTICAST,UP,LOWER_UP> mtu 1500 qdisc noqueue state UP\n" +
                "    inet 192.168.1.100/24 brd 192.168.1.255 scope global eth0";
        if (args[0] == "route" || args[0] == "r")
            return "default via 192.168.1.1 dev eth0\n192.168.1.0/24 dev eth0 scope link";
        return $"ip: '{args[0]}' is not an ip command";
    }

    private static string CmdTar(string[] args)
    {
        if (args.Length < 2) return "tar: missing operands\nTry 'tar --help' for more information.";
        return $"tar: simulated - archive operations not available in WebLinux\nAttempted: tar {string.Join(" ", args)}";
    }

    private static string CmdZip(string[] args)
    {
        if (args.Length < 2) return "zip: missing file operand";
        return $"zip: simulated - compression not available in WebLinux\nCreated: {args[0]} (simulated)";
    }

    private static string CmdUnzip(string[] args)
    {
        if (args.Length == 0) return "unzip: missing file operand";
        return $"unzip: simulated - decompression not available in WebLinux\nExtracted: {args[0]} (simulated)";
    }

    private static string CmdGit(string[] args)
    {
        if (args.Length == 0) return "usage: git <command> [<args>]";
        return args[0] switch
        {
            "init" => "Initialized empty Git repository in /home/user/project/.git/",
            "status" => "On branch main\nnothing to commit, working tree clean",
            "add" => "",
            "commit" => $"[main (root-commit) {Guid.NewGuid().ToString("N")[..7]}] initial commit\n 1 file changed, 1 insertion(+)",
            "log" => $"commit {Guid.NewGuid().ToString("N")[..40]}\nAuthor: user <user@weblinux>\nDate:   {DateTime.Now}\n\n    initial commit",
            "branch" => "* main",
            "clone" => args.Length > 1 ? $"Cloning into '{args[1].Split('/').Last().Replace(".git", "")}'...\ndone." : "clone: missing repository URL",
            "push" => "Enumerating objects: 3, done.\nto https://github.com/user/repo.git\n * [new branch]      main -> main",
            "pull" => "Already up to date.",
            "diff" => "",
            "merge" => "merge: simulated",
            "checkout" => $"Switched to branch '{args.ElementAtOrDefault(1) ?? "main"}'",
            _ => $"git: '{args[0]}' is not a git command. See 'git --help'."
        };
    }

    private static string CmdNpm(string[] args)
    {
        if (args.Length == 0) return "usage: npm <command>";
        return args[0] switch
        {
            "init" => "Wrote to package.json",
            "install" => args.Length > 1
                ? $"added {new Random().Next(5, 50)} packages in {new Random().Next(1, 10)}s"
                : "up to date, audited 0 packages",
            "start" => "simulated npm start",
            "test" => "simulated npm test",
            "run" => $"simulated npm run {args.ElementAtOrDefault(1) ?? ""}",
            "list" => "(empty)",
            "version" => "{ npm: '10.2.0', node: '20.11.0', v8: '11.0.2', modules: '115', openssl: '3.0.11' }",
            _ => $"npm ERR! Unknown command: \"{args[0]}\""
        };
    }

    private static string CmdPip(string[] args)
    {
        if (args.Length == 0) return "usage: pip <command>";
        return args[0] switch
        {
            "install" => args.Length > 1
                ? $"Successfully installed {string.Join(" ", args.Skip(1))}"
                : "ERROR: You gave no requirements, and no packages were installed.",
            "list" => "Package    Version\n---------- -------\npip        23.2.1\nsetuptools 68.0.0",
            "freeze" => "",
            "uninstall" => args.Length > 1
                ? $"Found existing installation: {args[1]}\nSuccessfully uninstalled {args[1]}"
                : "ERROR: You must specify a package to uninstall",
            _ => $"ERROR: unknown command \"{args[0]}\""
        };
    }

    private static string CmdNl(string[] args)
    {
        if (args.Length == 0) return "nl: missing file operand";
        var path = ResolvePath(args[0]);
        try
        {
            var content = FileSystem.ReadFile(path);
            var lines = content.Split('\n');
            return string.Join("\n", lines.Select((l, i) => $"     {i + 1}\t{l}"));
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string CmdRev(string[] args)
    {
        if (args.Length == 0) return "rev: missing file operand";
        var path = ResolvePath(args[0]);
        try
        {
            var content = FileSystem.ReadFile(path);
            return string.Join("\n", content.Split('\n').Select(l => new string(l.Reverse().ToArray())));
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string CmdTree(string[] args)
    {
        var path = args.Length > 0 ? ResolvePath(args[0]) : session.CurrentPath;
        var output = new List<string> { path };

        void BuildTree(string currentPath, string prefix)
        {
            var contents = FileSystem.GetContents(currentPath);
            var filtered = contents.Where(c => !c.StartsWith('.')).ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                var childPath = currentPath == "/" ? "/" + filtered[i] : currentPath + "/" + filtered[i];
                var isDir = FileSystem.IsDirectory(childPath);
                var connector = i == filtered.Count - 1 ? "\u2514\u2500\u2500 " : "\u251C\u2500\u2500 ";
                var suffix = isDir ? "/" : "";
                output.Add($"{prefix}{connector}{filtered[i]}{suffix}");

                if (isDir)
                {
                    var newPrefix = prefix + (i == filtered.Count - 1 ? "    " : "\u2502   ");
                    BuildTree(childPath, newPrefix);
                }
            }
        }

        BuildTree(path, "");
        var dirCount = FileSystem.GetContents(path).Count(c => FileSystem.IsDirectory((path == "/" ? "/" : path + "/") + c));
        var fileCount = FileSystem.GetContents(path).Count - dirCount;
        output.Add("");
        output.Add($"{dirCount} directories, {fileCount} files");
        return string.Join("\n", output);
    }

    private static string CmdStat(string[] args)
    {
        if (args.Length == 0) return "stat: missing operand";
        var path = ResolvePath(args[0]);
        if (!FileSystem.PathExists(path))
            return $"stat: cannot stat '{args[0]}': No such file or directory";

        var isDir = FileSystem.IsDirectory(path);
        var size = isDir ? FileSystem.GetDirectorySize(path) : long.Parse(FileSystem.GetFileSize(path));
        var perm = FileSystem.GetNodePermissions(path);
        var type = isDir ? "directory" : "regular file";

        return $"  File: {args[0]}\n  Size: {size}\t\tBlocks: 8          IO Block: 4096   {type}\n" +
            $"Access: ({perm})  Uid: ( 1000/    user)   Gid: ( 1000/    user)\n" +
            $"Access: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}\n" +
            $"Modify: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}\n" +
            $"Change: {DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff}";
    }

    private static string CmdRealpath(string[] args)
    {
        if (args.Length == 0) return "realpath: missing operand";
        return ResolvePath(args[0]);
    }

    private static string CmdSeq(string[] args)
    {
        if (args.Length == 1 && int.TryParse(args[0], out int e))
            return string.Join("\n", Enumerable.Range(1, e));
        if (args.Length == 2 && int.TryParse(args[0], out int s) && int.TryParse(args[1], out int end))
            return string.Join("\n", Enumerable.Range(s, end - s + 1));
        return "seq: invalid arguments";
    }

    private static string CmdSystemctl(string[] args)
    {
        if (args.Length == 0) return "usage: systemctl <command> [service]";
        return args[0] switch
        {
            "status" => args.Length > 1
                ? $"● {args[1]}.service - {args[1]} service\n     Loaded: loaded (/usr/lib/systemd/system/{args[1]}.service; enabled)\n     Active: active (running) since {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : "● weblinux.service - WebLinux System\n     Loaded: loaded (/usr/lib/systemd/system/weblinux.service; enabled)\n     Active: active (running)",
            "list-units" => "UNIT                    LOAD   ACTIVE SUB     DESCRIPTION\nweblinux.service        loaded active running WebLinux Terminal Service",
            "start" => args.Length > 1 ? $"Started {args[1]}." : "start: missing service name",
            "stop" => args.Length > 1 ? $"Stopped {args[1]}." : "stop: missing service name",
            "restart" => args.Length > 1 ? $"Restarted {args[1]}." : "restart: missing service name",
            "enable" => args.Length > 1 ? $"Created symlink from /etc/systemd/system/multi-user.target.wants/{args[1]}.service" : "enable: missing service name",
            "disable" => args.Length > 1 ? $"Removed /etc/systemd/system/multi-user.target.wants/{args[1]}.service" : "disable: missing service name",
            _ => $"Unknown operation '{args[0]}'."
        };
    }

    private static string CmdApt(string[] args)
    {
        if (args.Length == 0) return "usage: apt <command>";
        return args[0] switch
        {
            "update" => "Reading package lists... Done\nBuilding dependency tree... Done\nAll packages are up to date.",
            "upgrade" => "Reading package lists... Done\nBuilding dependency tree... Done\nCalculating upgrade... Done\n0 upgraded, 0 newly installed, 0 to remove and 0 not upgraded.",
            "install" => args.Length > 1
                ? $"Reading package lists... Done\nBuilding dependency tree... Done\n{args[1]} is already the newest version.\n0 upgraded, 0 newly installed, 0 to remove and 0 not upgraded."
                : "E: Invalid operation install",
            "remove" => args.Length > 1
                ? $"Reading package lists... Done\nBuilding dependency tree... Done\nThe following packages will be REMOVED:\n  {args[1]}\n0 upgraded, 0 newly installed, 1 to remove and 0 not upgraded."
                : "E: Invalid operation remove",
            "search" => args.Length > 1
                ? $"Sorting... Done\nFull Text Search... Done\n{args[1]}_1.0.0 - simulated package"
                : "E: Invalid operation search",
            "list" => "Listing... Done\napt/now 2.6.1 amd64 [installed]\nbash/now 5.2.15 amd64 [installed]\ncurl/now 8.4.0 amd64 [installed]",
            "autoremove" => "Reading package lists... Done\nBuilding dependency tree... Done\n0 upgraded, 0 newly installed, 0 to remove and 0 not upgraded.",
            "clean" => "",
            _ => $"E: The operation {args[0]} is not permitted in WebLinux."
        };
    }
}
