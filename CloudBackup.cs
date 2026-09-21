namespace TailorShop;

public class CloudTarget
{
    public string Provider { get; set; } = "";
    public string Path     { get; set; } = "";

    public override string ToString() => $"{Provider}  —  {Path}";
}

public static class CloudBackup
{
    private const string FolderName = "National Tailor Backups";

    private static string SettingFile => Path.Combine(AppPaths.DataFolder, "cloud-folder.txt");

    public static string? SavedFolder
    {
        get
        {
            try
            {
                if (!File.Exists(SettingFile)) return null;
                var value = File.ReadAllText(SettingFile).Trim();
                return value.Length == 0 ? null : value;
            }
            catch
            {
                return null;
            }
        }
    }

    public static void SaveFolder(string folder)
    {
        try
        {
            File.WriteAllText(SettingFile, folder);
        }
        catch
        {
        }
    }

    public static void ClearFolder()
    {
        try
        {
            if (File.Exists(SettingFile)) File.Delete(SettingFile);
        }
        catch
        {
        }
    }

    public static List<CloudTarget> Detect()
    {
        var found = new List<CloudTarget>();
        var seen  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string provider, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (!SafeExists(path)) return;
            if (!seen.Add(path)) return;
            found.Add(new CloudTarget { Provider = provider, Path = path });
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var drive in DriveLetters())
        {
            Add("Google Drive", Path.Combine(drive, "My Drive"));
            Add("Google Drive", Path.Combine(drive, "Shared drives"));
        }

        Add("Google Drive", Path.Combine(home, "Google Drive"));
        Add("Google Drive", Path.Combine(home, "Google Drive", "My Drive"));

        Add("OneDrive", Environment.GetEnvironmentVariable("OneDriveConsumer"));
        Add("OneDrive", Environment.GetEnvironmentVariable("OneDriveCommercial"));
        Add("OneDrive", Environment.GetEnvironmentVariable("OneDrive"));
        Add("OneDrive", Path.Combine(home, "OneDrive"));

        Add("Dropbox", Path.Combine(home, "Dropbox"));

        return found;
    }

    public static BackupResult Run(string cloudRoot)
    {
        try
        {
            var folder = Path.Combine(cloudRoot, FolderName);
            Directory.CreateDirectory(folder);

            var result = Backup.Create(folder);
            if (result.Success) SaveFolder(cloudRoot);
            return result;
        }
        catch (Exception ex)
        {
            return new BackupResult { Success = false, Error = ex.Message };
        }
    }

    private static IEnumerable<string> DriveLetters()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch
        {
            yield break;
        }

        foreach (var d in drives)
        {
            string root;
            try
            {
                root = d.RootDirectory.FullName;
            }
            catch
            {
                continue;
            }

            yield return root;
        }
    }

    private static bool SafeExists(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}
