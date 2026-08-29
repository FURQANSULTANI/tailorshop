using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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

            KillOrphanOnPort(WhatsAppConfig.Port);
            WaitForPortFree(WhatsAppConfig.Port);

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

    private static void KillOrphanOnPort(int port)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "netstat",
                Arguments              = "-ano",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(2000);

            foreach (var line in output.Split('\n'))
            {
                if (!line.Contains($":{port} ", StringComparison.Ordinal)) continue;
                if (!line.Contains("LISTENING", StringComparison.Ordinal)) continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || !int.TryParse(parts[^1], out var pid)) continue;
                if (pid == Environment.ProcessId) continue;
                try { Process.GetProcessById(pid).Kill(entireProcessTree: true); } catch { }
            }
        }
        catch { }
    }

    private static void WaitForPortFree(int port, int timeoutMs = 3000)
    {
        var start = Environment.TickCount;
        while (Environment.TickCount - start < timeoutMs)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return;
            }
            catch (SocketException) { Thread.Sleep(200); }
        }
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
        KillOrphanOnPort(WhatsAppConfig.Port);
        TryDelete(Path.Combine(ServiceDir, ".wwebjs_auth"));
        TryDelete(Path.Combine(ServiceDir, ".wwebjs_cache"));
        Start();
    }

    private static void TryDelete(string dir)
    {
        for (int i = 0; i < 8; i++)
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
