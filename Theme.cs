using System.Drawing.Drawing2D;

namespace TailorShop;

public static class Theme
{
    public static readonly Color DarkGrey   = Color.FromArgb(0x1F, 0x7A, 0x6C);
    public static readonly Color NormalGrey = Color.FromArgb(0xFB, 0xF7, 0xF0);
    public static readonly Color DarkGold   = Color.FromArgb(0x14, 0xA3, 0xC7);
    public static readonly Color DeleteAccent = Color.FromArgb(0x59, 0x87, 0x80);
    public static readonly Color RowAlt = Color.FromArgb(0x14, 0xA3, 0xC7);
    public static readonly Color TextOnRowAlt = Color.White;

    public static readonly Color TextInk = Color.FromArgb(0x2B, 0x2B, 0x2B);
    public static readonly Color TextOnWhite = Color.White;

    public static readonly Color TextOnDark   = TextOnWhite;
    public static readonly Color TextOnNormal = TextInk;
    public static readonly Color TextOnGold   = TextOnWhite;
    public static readonly Color TextOnDeleteAccent = TextOnWhite;

    // Matches the deep blue of the National Tailor logo artwork.
    public static readonly Color BrandBlue   = Color.FromArgb(0x0A, 0x28, 0x96);

    public static readonly Color AlertRed    = Color.FromArgb(253, 28, 3);
    public static readonly Color AlertOrange = Color.FromArgb(248, 128, 23);
    public static readonly Color TextOnAlert = Color.White;

    // "Urdu Typesetting" ships with Windows (complex-script/Arabic support since Vista) and
    // renders proper Nastaliq-style joined letterforms. If it's ever missing on a machine,
    // Font silently substitutes the nearest available font instead of throwing.
    public static readonly Font UrduFont      = new("Urdu Typesetting", 12f);

    // Nastaliq's diagonal, stacked letterforms need more line-height per point size than
    // Latin text — this smaller size is for tight fixed-height spots (checkbox chips) where
    // the full-size font would clip against the control's edge.
    public static readonly Font UrduFontSmall = new("Urdu Typesetting", 9f);

    public static Color Hover(Color baseColor) =>
        baseColor.GetBrightness() > 0.5f
            ? DeleteAccent
            : ControlPaint.Light(baseColor, 0.25f);

    public static Bitmap ToWhiteSilhouette(Image source) => ToSilhouette(source, Color.White);

    public static Bitmap ToSilhouette(Image source, Color tint)
    {
        var bmp = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) g.DrawImage(source, 0, 0, source.Width, source.Height);

        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                var p = bmp.GetPixel(x, y);
                var luma = (int)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
                var alpha = Math.Clamp((luma - 24) * 255 / 200, 0, 255);
                bmp.SetPixel(x, y, Color.FromArgb(alpha, tint.R, tint.G, tint.B));
            }

        return bmp;
    }

    public static Bitmap TrimUniformMargins(Image source, int tolerance = 20)
    {
        using var bmp = new Bitmap(source);
        var bg = bmp.GetPixel(0, 0);
        int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;

        for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                var p = bmp.GetPixel(x, y);
                if (Math.Abs(p.R - bg.R) <= tolerance &&
                    Math.Abs(p.G - bg.G) <= tolerance &&
                    Math.Abs(p.B - bg.B) <= tolerance) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

        if (maxX < 0) return new Bitmap(source);

        var rect = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        var cropped = new Bitmap(rect.Width, rect.Height);
        using var g = Graphics.FromImage(cropped);
        g.DrawImage(bmp, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        return cropped;
    }

    public static void PaintFieldBorders(Control container, params Control[] fields)
    {
        foreach (var f in fields)
            if (f is TextBox tb) tb.BorderStyle = BorderStyle.None;

        container.Paint += (_, e) =>
        {
            using var pen = new Pen(DarkGold, 1f);
            foreach (var f in fields)
            {
                if (f.Parent != container) continue;
                e.Graphics.DrawRectangle(pen, f.Left - 2, f.Top - 2, f.Width + 3, f.Height + 3);
            }
        };
    }

    public static void PaintFieldBand(Control container, int bandTop, int bandHeight, params Control[] fields)
    {
        foreach (var f in fields)
        {
            if (f is TextBox tb) tb.BorderStyle = BorderStyle.None;
            f.LocationChanged += (_, _) => container.Invalidate();
            f.SizeChanged     += (_, _) => container.Invalidate();
            f.VisibleChanged  += (_, _) => container.Invalidate();
        }

        container.Resize += (_, _) => container.Invalidate();

        container.Paint += (_, e) =>
        {
            using var pen = new Pen(DarkGold, 1f);
            foreach (var f in fields)
            {
                if (f.Parent != container) continue;
                if (!f.Visible) continue;
                e.Graphics.DrawRectangle(pen, f.Left - 3, bandTop - 1, f.Width + 5, bandHeight + 1);
            }
        };
    }

    public static void ApplyLightHover(Button btn)
    {
        var restFore = btn.ForeColor;
        btn.FlatAppearance.MouseOverBackColor = DeleteAccent;
        btn.MouseEnter += (_, _) => btn.ForeColor = TextOnWhite;
        btn.MouseLeave += (_, _) => btn.ForeColor = restFore;
    }

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

    // A simple chat-bubble glyph drawn in the palette color, instead of a WhatsApp
    // emoji/logo — emoji render as fixed multi-color glyphs regardless of the
    // control's ForeColor, breaking the 3-color rule.
    public enum Glyph
    {
        Search, Clear, Add, Edit, Delete, Backup, Restore, Stock,
        History, Print, Save, Cancel, Folder, Cloud, Close, Key, Copy
    }

    public static Bitmap Icon(Glyph glyph, Color color, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float s      = size;
        float stroke = Math.Max(1.4f, s / 9f);
        using var pen   = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);

        switch (glyph)
        {
            case Glyph.Search:
                g.DrawEllipse(pen, s * 0.14f, s * 0.14f, s * 0.52f, s * 0.52f);
                g.DrawLine(pen, s * 0.62f, s * 0.62f, s * 0.86f, s * 0.86f);
                break;

            case Glyph.Clear:
            case Glyph.Close:
            case Glyph.Cancel:
                g.DrawLine(pen, s * 0.22f, s * 0.22f, s * 0.78f, s * 0.78f);
                g.DrawLine(pen, s * 0.78f, s * 0.22f, s * 0.22f, s * 0.78f);
                break;

            case Glyph.Add:
                g.DrawLine(pen, s * 0.5f, s * 0.18f, s * 0.5f, s * 0.82f);
                g.DrawLine(pen, s * 0.18f, s * 0.5f, s * 0.82f, s * 0.5f);
                break;

            case Glyph.Edit:
                g.DrawLine(pen, s * 0.20f, s * 0.80f, s * 0.68f, s * 0.30f);
                g.DrawLine(pen, s * 0.68f, s * 0.30f, s * 0.82f, s * 0.44f);
                g.DrawLine(pen, s * 0.82f, s * 0.44f, s * 0.34f, s * 0.86f);
                g.DrawLine(pen, s * 0.20f, s * 0.80f, s * 0.34f, s * 0.86f);
                break;

            case Glyph.Delete:
                g.DrawLine(pen, s * 0.18f, s * 0.26f, s * 0.82f, s * 0.26f);
                g.DrawRectangle(pen, s * 0.26f, s * 0.26f, s * 0.48f, s * 0.58f);
                g.DrawLine(pen, s * 0.40f, s * 0.14f, s * 0.60f, s * 0.14f);
                g.DrawLine(pen, s * 0.44f, s * 0.42f, s * 0.44f, s * 0.70f);
                g.DrawLine(pen, s * 0.58f, s * 0.42f, s * 0.58f, s * 0.70f);
                break;

            case Glyph.Backup:
                g.DrawLine(pen, s * 0.5f, s * 0.16f, s * 0.5f, s * 0.62f);
                g.DrawLine(pen, s * 0.30f, s * 0.44f, s * 0.5f, s * 0.64f);
                g.DrawLine(pen, s * 0.70f, s * 0.44f, s * 0.5f, s * 0.64f);
                g.DrawLine(pen, s * 0.18f, s * 0.82f, s * 0.82f, s * 0.82f);
                break;

            case Glyph.Restore:
                g.DrawArc(pen, s * 0.16f, s * 0.16f, s * 0.68f, s * 0.68f, 40, 280);
                g.DrawLine(pen, s * 0.80f, s * 0.14f, s * 0.80f, s * 0.38f);
                g.DrawLine(pen, s * 0.80f, s * 0.38f, s * 0.56f, s * 0.38f);
                break;

            case Glyph.Stock:
                g.DrawRectangle(pen, s * 0.14f, s * 0.34f, s * 0.72f, s * 0.50f);
                g.DrawLine(pen, s * 0.14f, s * 0.50f, s * 0.86f, s * 0.50f);
                g.DrawLine(pen, s * 0.28f, s * 0.34f, s * 0.40f, s * 0.14f);
                g.DrawLine(pen, s * 0.72f, s * 0.34f, s * 0.60f, s * 0.14f);
                g.DrawLine(pen, s * 0.40f, s * 0.14f, s * 0.60f, s * 0.14f);
                break;

            case Glyph.History:
                g.DrawEllipse(pen, s * 0.16f, s * 0.16f, s * 0.68f, s * 0.68f);
                g.DrawLine(pen, s * 0.5f, s * 0.32f, s * 0.5f, s * 0.52f);
                g.DrawLine(pen, s * 0.5f, s * 0.52f, s * 0.68f, s * 0.62f);
                break;

            case Glyph.Print:
                g.DrawRectangle(pen, s * 0.30f, s * 0.12f, s * 0.40f, s * 0.22f);
                g.DrawRectangle(pen, s * 0.14f, s * 0.34f, s * 0.72f, s * 0.34f);
                g.DrawRectangle(pen, s * 0.30f, s * 0.60f, s * 0.40f, s * 0.28f);
                break;

            case Glyph.Save:
                g.DrawRectangle(pen, s * 0.16f, s * 0.16f, s * 0.68f, s * 0.68f);
                g.DrawRectangle(pen, s * 0.34f, s * 0.16f, s * 0.32f, s * 0.26f);
                g.DrawRectangle(pen, s * 0.30f, s * 0.56f, s * 0.40f, s * 0.28f);
                break;

            case Glyph.Folder:
                g.DrawLine(pen, s * 0.12f, s * 0.28f, s * 0.44f, s * 0.28f);
                g.DrawLine(pen, s * 0.44f, s * 0.28f, s * 0.52f, s * 0.38f);
                g.DrawLine(pen, s * 0.52f, s * 0.38f, s * 0.88f, s * 0.38f);
                g.DrawRectangle(pen, s * 0.12f, s * 0.28f, s * 0.76f, s * 0.52f);
                break;

            case Glyph.Cloud:
                g.DrawArc(pen, s * 0.10f, s * 0.34f, s * 0.40f, s * 0.40f, 90, 180);
                g.DrawArc(pen, s * 0.30f, s * 0.20f, s * 0.44f, s * 0.44f, 180, 180);
                g.DrawArc(pen, s * 0.54f, s * 0.36f, s * 0.36f, s * 0.36f, 270, 180);
                g.DrawLine(pen, s * 0.28f, s * 0.74f, s * 0.72f, s * 0.74f);
                break;

            case Glyph.Key:
                g.DrawEllipse(pen, s * 0.12f, s * 0.36f, s * 0.34f, s * 0.34f);
                g.DrawLine(pen, s * 0.44f, s * 0.53f, s * 0.88f, s * 0.53f);
                g.DrawLine(pen, s * 0.74f, s * 0.53f, s * 0.74f, s * 0.72f);
                g.DrawLine(pen, s * 0.86f, s * 0.53f, s * 0.86f, s * 0.68f);
                break;

            case Glyph.Copy:
                g.DrawRectangle(pen, s * 0.14f, s * 0.14f, s * 0.50f, s * 0.50f);
                g.DrawRectangle(pen, s * 0.34f, s * 0.34f, s * 0.52f, s * 0.52f);
                break;
        }

        return bmp;
    }

    public static void CenterButtons(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Button b)
            {
                b.TextAlign = ContentAlignment.MiddleCenter;
                if (b.Image != null)
                {
                    b.ImageAlign        = ContentAlignment.MiddleCenter;
                    b.TextImageRelation = TextImageRelation.ImageBeforeText;
                }
            }

            if (c.HasChildren) CenterButtons(c);
        }
    }

    public static void SetIcon(Button btn, Glyph glyph, int size = 16)
    {
        var painted = Color.Empty;

        btn.Image             = Icon(glyph, btn.ForeColor, size);
        painted               = btn.ForeColor;
        btn.ImageAlign        = ContentAlignment.MiddleCenter;
        btn.TextAlign         = ContentAlignment.MiddleCenter;
        btn.TextImageRelation = TextImageRelation.ImageBeforeText;

        btn.Paint += (_, _) =>
        {
            if (btn.ForeColor == painted) return;
            painted = btn.ForeColor;
            var old = btn.Image;
            btn.Image = Icon(glyph, painted, size);
            old?.Dispose();
        };
    }

    public static Bitmap CreateChatBubbleIcon(Color color, int size = 16)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float pad = size * 0.12f;
        var bubble = new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad - size * 0.16f);
        using var pen = new Pen(color, Math.Max(1f, size / 8f));
        g.DrawEllipse(pen, bubble);

        var tail = new[]
        {
            new PointF(bubble.Left + bubble.Width * 0.28f, bubble.Bottom - size * 0.03f),
            new PointF(bubble.Left + bubble.Width * 0.16f, bubble.Bottom + size * 0.18f),
            new PointF(bubble.Left + bubble.Width * 0.46f, bubble.Bottom - size * 0.06f)
        };
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, tail);

        return bmp;
    }
}
