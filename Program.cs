namespace TailorShop;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!Activate()) return;

        Application.Run(new MainForm());
    }

    private static bool Activate()
    {
        var info = License.Check();
        if (info.IsValid) return true;

        using var form = new LicenseForm(info);
        return form.ShowDialog() == DialogResult.OK;
    }
}
