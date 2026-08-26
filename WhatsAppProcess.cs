using System.Diagnostics;
using System.Threading;

namespace TailorShop;

public static class WhatsAppProcess
{
    private static Process? _process;
    private static readonly object LogLock = new();

    private static string ServiceDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WhatsAppService");

    public static void Start()
    {
        try
        {
            var baseDir     = AppDomain.CurrentDomain.BaseDirectory;
            var serviceDir  = ServiceDir;
            var bundledNode = Path.Combine(serviceDir, "node", "node.exe");
            var exe         = File.Exists(bundledNode) ? bundledNode : "node";
            var logPath     = Path.Combine(baseDir, "whatsapp-service.log");

            if (!Directory.Exists(serviceDir)) return;

            var psi = new ProcessStartInfo
            {
                FileName               = exe,
                Arguments              = "index.js",
                WorkingDirectory       = serviceDir,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };

            _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) => AppendLog(logPath, e.Data);
            _process.ErrorDataReceived  += (_, e) => AppendLog(logPath, e.Data);
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch { }
    }

    private static void AppendLog(string path, string? line)
    {
        if (line == null) return;
        try { lock (LogLock) File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {line}\n"); } catch { }
    }

    public static void Stop()
    {
        try { _process?.Kill(entireProcessTree: true); } catch { }
    }

    public static void ResetSession()
    {
        Stop();
        TryDelete(Path.Combine(ServiceDir, ".wwebjs_auth"));
        TryDelete(Path.Combine(ServiceDir, ".wwebjs_cache"));
        Start();
    }

    private static void TryDelete(string dir)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
                return;
            }
            catch { Thread.Sleep(300); }
        }
    }
}
