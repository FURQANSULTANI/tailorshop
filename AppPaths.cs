namespace TailorShop;

public static class AppPaths
{
    public const string DbFileName = "golden_tailor.db";

    public static string DataFolder { get; } = Resolve();

    public static string DbFile => Path.Combine(DataFolder, DbFileName);

    public static string BackupFolder => Path.Combine(DataFolder, "Backups");

    private static string Resolve()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "TailorShop");

        try
        {
            Directory.CreateDirectory(folder);
            MigrateLegacyDatabase(folder);
            return folder;
        }
        catch
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }

    private static void MigrateLegacyDatabase(string folder)
    {
        var target = Path.Combine(folder, DbFileName);
        if (File.Exists(target)) return;

        var legacy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);
        if (!File.Exists(legacy)) return;

        try
        {
            File.Copy(legacy, target);
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var extra = legacy + suffix;
                if (File.Exists(extra)) File.Copy(extra, target + suffix, true);
            }

            File.Move(legacy, legacy + ".migrated", true);
        }
        catch
        {
        }
    }
}
