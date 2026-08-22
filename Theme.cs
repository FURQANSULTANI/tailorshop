using System.Drawing.Drawing2D;

namespace TailorShop;

public static class Theme
{
    // The only 3 colors used anywhere in the UI.
    public static readonly Color DarkGrey   = Color.FromArgb(55, 55, 57);    // #373739 — 55%, primary surfaces
    public static readonly Color NormalGrey = Color.FromArgb(208, 208, 206); // #D0D0CE — 30%, secondary surfaces
    public static readonly Color DarkGold   = Color.FromArgb(176, 141, 66);  // #B08D42 — 15%, accent only

    // Text colors — always one of the 3 palette colors, never white or black.
    public static readonly Color TextOnDark   = NormalGrey;
    public static readonly Color TextOnNormal = DarkGrey;
    public static readonly Color TextOnGold   = DarkGrey;

    // "Urdu Typesetting" ships with Windows (complex-script/Arabic support since Vista) and
    // renders proper Nastaliq-style joined letterforms. If it's ever missing on a machine,
    // Font silently substitutes the nearest available font instead of throwing.
    public static readonly Font UrduFont = new("Urdu Typesetting", 12f);

    public static Color Hover(Color baseColor) =>
        baseColor.GetBrightness() > 0.5f
            ? ControlPaint.Dark(baseColor, 0.1f)
            : ControlPaint.Light(baseColor, 0.25f);

    // A barely-there tint of the same palette color — not a new hue, just enough
    // to separate alternating rows without breaking the 3-color rule.
    public static Color Shade(Color baseColor, float factor) =>
        ControlPaint.Dark(baseColor, factor);

    public static void RoundCorners(Control control, int radius = 6)
    {
        void Apply()
        {
            if (control.Width <= 0 || control.Height <= 0) return;
            var path = new GraphicsPath();
            var rect = new Rectangle(0, 0, control.Width, control.Height);
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            control.Region = new Region(path);
        }
        Apply();
        control.Resize += (_, _) => Apply();
    }

    public static Panel AccentDivider(DockStyle dock, int thickness = 3) => new()
    {
        Dock      = dock,
        Height    = thickness,
        Width     = thickness,
        BackColor = DarkGold
    };
}
