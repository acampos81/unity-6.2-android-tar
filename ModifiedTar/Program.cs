using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

class Program
{


    static int Main(string[] args)
    {
        var joinedArgs = string.Join(", ",args);
        //Console.WriteLine($"joined: {joinedArgs}");

        var pattern = @"([CD:][\w\d\.\:\-\\\/]+)";
        var regex = new Regex(pattern);
        var matches = regex.Matches(joinedArgs);

        var archive = matches[0].Value;
        var dest = matches[1].Value;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = @"C:\Windows\System32\tar.exe",
                Arguments = $"-x -f {archive} -C {dest}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled wrapper exception: " + ex);
            return 1;
        }

        return 0;
    }

    static async Task<int> MainAsync(string[] args)
    {
        try
        {
            // Remove occurrences of -a and /a flags (exact matches).
            // If you expect combined short flags like -ax, extend this logic accordingly.
            var filtered = args.Where(a => !string.Equals(a, "-a", StringComparison.OrdinalIgnoreCase)
                                         && !string.Equals(a, "/a", StringComparison.OrdinalIgnoreCase))
                               .ToArray();

            // Find the real system tar.exe
            string systemTarPath = FindSystemTarPath();

            if (systemTarPath == null || !File.Exists(systemTarPath))
            {
                Console.Error.WriteLine("ERROR: could not find the system tar.exe (checked System32 and Sysnative).");
                return 127;
            }

            // Build argument string safely
            string argumentString = BuildArgumentString(filtered);

            var psi = new ProcessStartInfo
            {
                FileName = systemTarPath,
                Arguments = argumentString,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory,
            };

            using (var proc = new Process { StartInfo = psi, EnableRaisingEvents = true })
            {
                var stdOutTcs = new TaskCompletionSource<bool>();
                var stdErrTcs = new TaskCompletionSource<bool>();

                proc.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null) stdOutTcs.TrySetResult(true);
                    else Console.Out.WriteLine(e.Data);
                };

                proc.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) stdErrTcs.TrySetResult(true);
                    else Console.Error.WriteLine(e.Data);
                };

                if (!proc.Start())
                {
                    Console.Error.WriteLine("ERROR: failed to start system tar.exe");
                    return 126;
                }

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await Task.WhenAll(stdOutTcs.Task, stdErrTcs.Task).ConfigureAwait(false);

                proc.WaitForExit();
                return proc.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled wrapper exception: " + ex);
            return 1;
        }
    }

    static string FindSystemTarPath()
    {
        // Try to find the real system tar in the System32 or Sysnative location.
        // If our wrapper is 32-bit and running on 64-bit Windows, accessing C:\Windows\System32 will be redirected to SysWOW64.
        // To reach the real System32 from a 32-bit process use C:\Windows\Sysnative\tar.exe.

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows); // e.g., C:\Windows
        string sysnative = Path.Combine(windows, "Sysnative", "tar.exe"); // works only from 32-bit process on 64-bit Windows
        string system32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe"); // usually C:\Windows\System32\tar.exe

        // Check Sysnative first (if present and accessible)
        if (File.Exists(sysnative))
            return sysnative;

        // Then check the normal System directory
        if (File.Exists(system32))
            return system32;

        // As a fallback, try PATH resolution for "tar.exe"
        try
        {
            var envPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var p in envPath.Split(Path.PathSeparator))
            {
                try
                {
                    var candidate = Path.Combine(p.Trim(), "tar.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch { }
            }
        }
        catch { /* ignore */ }

        return null;
    }

    static string BuildArgumentString(string[] args)
    {
        // Properly quote individual arguments if they contain spaces or quoting chars.
        // This simple quoting is compatible with CreateProcess command-line parsing rules for most use-cases.
        var sb = new StringBuilder();
        foreach (var a in args)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(QuoteArgument(a));
        }
        return sb.ToString();
    }

    static string QuoteArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        bool needQuotes = arg.Any(c => char.IsWhiteSpace(c)) || arg.Contains("\"");
        if (!needQuotes) return arg;

        // Escape quotes and backslashes per Windows rules
        var sb = new StringBuilder();
        sb.Append('"');
        int backslashes = 0;
        foreach (char c in arg)
        {
            if (c == '\\')
            {
                backslashes++;
            }
            else if (c == '"')
            {
                // output escaped backslashes (double them) then escape the quote
                sb.Append('\\', backslashes * 2 + 1);
                sb.Append('"');
                backslashes = 0;
            }
            else
            {
                if (backslashes > 0)
                {
                    sb.Append('\\', backslashes);
                    backslashes = 0;
                }
                sb.Append(c);
            }
        }
        if (backslashes > 0)
            sb.Append('\\', backslashes * 2);
        sb.Append('"');
        return sb.ToString();
    }
}
