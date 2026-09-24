using Microsoft.Data.Sqlite;

namespace TailorShop;

public class BackupResult
{
    public bool    Success { get; set; }
    public string? Path    { get; set; }
    public string? Error   { get; set; }
}

public static class Backup
{
    private const int KeepCount    = 20;
    private const int IntervalDays = 7;

    private static string StampFile      => Path.Combine(AppPaths.DataFolder, "last-backup.txt");
    private static string EmailStampFile => Path.Combine(AppPaths.DataFolder, "last-backup-email.txt");

    public static BackupResult Create(string? destinationFolder = null)
    {
        try
        {
            var folder = destinationFolder ?? AppPaths.BackupFolder;
            Directory.CreateDirectory(folder);

            var name = $"golden_tailor_{DateTime.Now:yyyy-MM-dd_HHmm}.db";
            var path = Path.Combine(folder, name);

            using (var source = new SqliteConnection($"Data Source={AppPaths.DbFile}"))
            using (var target = new SqliteConnection($"Data Source={path}"))
            {
                source.Open();
                target.Open();
                source.BackupDatabase(target);
            }

            SqliteConnection.ClearAllPools();

            if (destinationFolder == null)
            {
                Prune(folder);
                File.WriteAllText(StampFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            return new BackupResult { Success = true, Path = path };
        }
        catch (Exception ex)
        {
            return new BackupResult { Success = false, Error = ex.Message };
        }
    }

    public static void RunIfDue()
    {
        try
        {
            if (!File.Exists(AppPaths.DbFile)) return;

            if (File.Exists(StampFile) &&
                DateTime.TryParse(File.ReadAllText(StampFile).Trim(), out var last) &&
                (DateTime.Now - last).TotalDays < IntervalDays)
                return;

            var result = Create();
            if (result.Success) EmailIfConfigured();
        }
        catch
        {
        }
    }

    private static void EmailIfConfigured()
    {
        try
        {
            var settings = EmailSettings.Load();
            if (!settings.IsConfigured) return;

            var sent = EmailBackup.Send(settings);
            File.WriteAllText(EmailStampFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{(sent.Success ? "sent" : "failed: " + sent.Error)}");
        }
        catch
        {
        }
    }

    public static DateTime? LastBackupTime()
    {
        try
        {
            if (File.Exists(StampFile) &&
                DateTime.TryParse(File.ReadAllText(StampFile).Trim(), out var last))
                return last;
        }
        catch
        {
        }

        return null;
    }

    public static string? LastEmailStatus()
    {
        try
        {
            if (!File.Exists(EmailStampFile)) return null;

            var parts = File.ReadAllText(EmailStampFile).Split('\t');
            if (parts.Length < 2 || !DateTime.TryParse(parts[0].Trim(), out var when)) return null;

            var outcome = parts[1].Trim();
            return outcome == "sent"
                ? $"Last auto-email: {when:dd MMM yyyy, hh:mm tt}"
                : $"Last auto-email failed ({when:dd MMM yyyy}): {outcome[(outcome.IndexOf(':') + 1)..].Trim()}";
        }
        catch
        {
            return null;
        }
    }

    public static bool Restore(string backupFile, out string error)
    {
        error = "";
        try
        {
            using (var probe = new SqliteConnection($"Data Source={backupFile};Mode=ReadOnly"))
            {
                probe.Open();
                var cmd = probe.CreateCommand();
                cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Customers'";
                if (Convert.ToInt64(cmd.ExecuteScalar()) == 0)
                {
                    error = "The selected file is not a Golden Tailor database.";
                    return false;
                }
            }

            SqliteConnection.ClearAllPools();

            if (File.Exists(AppPaths.DbFile))
            {
                var safety = Path.Combine(AppPaths.BackupFolder,
                    $"before_restore_{DateTime.Now:yyyy-MM-dd_HHmm}.db");
                Directory.CreateDirectory(AppPaths.BackupFolder);
                File.Copy(AppPaths.DbFile, safety, true);
            }

            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var extra = AppPaths.DbFile + suffix;
                if (File.Exists(extra)) File.Delete(extra);
            }

            File.Copy(backupFile, AppPaths.DbFile, true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void Prune(string folder)
    {
        try
        {
            var files = new DirectoryInfo(folder)
                .GetFiles("golden_tailor_*.db")
                .OrderByDescending(f => f.CreationTime)
                .Skip(KeepCount);

            foreach (var file in files) file.Delete();
        }
        catch
        {
        }
    }
}
