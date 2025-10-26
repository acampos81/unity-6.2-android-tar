using System.Diagnostics;
using System.Text.RegularExpressions;

class Program
{
    static int Main(string[] args)
    {
        // Join the args into a single string
        var joinedArgs = string.Join(", ",args);

        // Define a regular expression to capture archive path and destination path
        var pattern = @"([C:][\w\d\.\:\-\\\/]+)";
        var regex = new Regex(pattern);
        var matches = regex.Matches(joinedArgs);
        var archive = matches[0].Value;
        var dest = matches[1].Value;

        // Invoke the system's tar.exe without the -a flag and pass in the archive path and destination paths
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
}
