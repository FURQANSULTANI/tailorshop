using System.IO.Compression;
using System.Net;
using System.Net.Mail;

namespace TailorShop;

public class EmailResult
{
    public bool    Success  { get; set; }
    public string? Error    { get; set; }
    public long    ZipBytes { get; set; }

    public string SizeText => ZipBytes < 1024 * 1024
        ? $"{ZipBytes / 1024.0:N1} KB"
        : $"{ZipBytes / (1024.0 * 1024.0):N1} MB";
}

public static class EmailBackup
{
    public const long MaxAttachmentBytes = 25L * 1024 * 1024;

    public static EmailResult Send(EmailSettings settings)
    {
        var result = new EmailResult();

        if (!settings.IsConfigured)
        {
            result.Error = "Email settings are incomplete. Open Email Settings and fill them in first.";
            return result;
        }

        string? dbCopy = null;
        string? zip    = null;

        try
        {
            var stamp   = DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            var tempDir = Path.Combine(Path.GetTempPath(), "TailorShopBackup");
            Directory.CreateDirectory(tempDir);

            var snapshot = Backup.Create(tempDir);
            if (!snapshot.Success)
            {
                result.Error = snapshot.Error ?? "Could not create the backup file.";
                return result;
            }
            dbCopy = snapshot.Path!;

            zip = Path.Combine(tempDir, $"NationalTailor_Backup_{stamp}.zip");
            if (File.Exists(zip)) File.Delete(zip);

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
                archive.CreateEntryFromFile(dbCopy, Path.GetFileName(dbCopy), CompressionLevel.Optimal);

            result.ZipBytes = new FileInfo(zip).Length;

            if (result.ZipBytes > MaxAttachmentBytes)
            {
                result.Error = $"Backup is {result.SizeText}, which is larger than the 25 MB email limit. " +
                               "Use \"Save Copy To USB / Folder\" instead.";
                return result;
            }

            using var message = new MailMessage
            {
                From    = new MailAddress(settings.Username, settings.FromName),
                Subject = $"National Tailor Backup — {DateTime.Now:dd-MM-yyyy hh:mm tt}",
                Body    = $"Automatic database backup from National Tailor.{Environment.NewLine}{Environment.NewLine}" +
                          $"Created : {DateTime.Now:dd-MM-yyyy hh:mm tt}{Environment.NewLine}" +
                          $"Size    : {result.SizeText}{Environment.NewLine}{Environment.NewLine}" +
                          "Keep this file safe. To restore, open Backup & Restore in National Tailor, " +
                          "extract the .db file from this zip, and choose Restore From Backup."
            };

            foreach (var to in settings.SendTo.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var address = to.Trim();
                if (address.Length > 0) message.To.Add(address);
            }

            if (message.To.Count == 0)
            {
                result.Error = "No valid recipient email address.";
                return result;
            }

            using var attachmentStream = new FileStream(zip, FileMode.Open, FileAccess.Read);
            message.Attachments.Add(new Attachment(attachmentStream, Path.GetFileName(zip)));

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl             = settings.UseSsl,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials           = new NetworkCredential(settings.Username, settings.Password),
                Timeout               = 120000
            };

            client.Send(message);
            result.Success = true;
            return result;
        }
        catch (SmtpException ex)
        {
            result.Error = Explain(ex);
            return result;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            return result;
        }
        finally
        {
            TryDelete(dbCopy);
            TryDelete(zip);
        }
    }

    public static EmailResult TestConnection(EmailSettings settings)
    {
        var result = new EmailResult();

        if (settings.Host.Length == 0 || settings.Username.Length == 0 || settings.Password.Length == 0)
        {
            result.Error = "Fill in server, username and password first.";
            return result;
        }

        try
        {
            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl             = settings.UseSsl,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials           = new NetworkCredential(settings.Username, settings.Password),
                Timeout               = 30000
            };

            using var probe = new MailMessage
            {
                From    = new MailAddress(settings.Username, settings.FromName),
                Subject = "National Tailor — settings test (no backup attached)",
                Body    = "This is only a connection test, so no backup file is attached." + Environment.NewLine + Environment.NewLine +
                          "Your email settings are working." + Environment.NewLine + Environment.NewLine +
                          "To actually email a backup, close this window and click \"Email Backup\" " +
                          "on the Backup & Restore screen."
            };
            probe.To.Add(settings.SendTo.Length > 0 ? settings.SendTo.Split(',', ';')[0].Trim() : settings.Username);

            client.Send(probe);
            result.Success = true;
            return result;
        }
        catch (SmtpException ex)
        {
            result.Error = Explain(ex);
            return result;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            return result;
        }
    }

    private static string Explain(SmtpException ex) => ex.StatusCode switch
    {
        SmtpStatusCode.MailboxBusy or
        SmtpStatusCode.MailboxUnavailable => "The mail server rejected the message. Check the recipient address.",
        SmtpStatusCode.ClientNotPermitted or
        SmtpStatusCode.MustIssueStartTlsFirst => "Login was refused. For Gmail you must use an App Password, not your normal password.",
        _ => ex.Message.Contains("5.7.", StringComparison.Ordinal)
            ? "Login was refused. For Gmail you must use an App Password, not your normal password."
            : ex.Message
    };

    private static void TryDelete(string? path)
    {
        try
        {
            if (path != null && File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }
}
