using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AutoHotkeys
{
    internal sealed class ScreenshotAction : HotkeyAction
    {
        private CaptureOverlay overlay;
        internal override string Id { get { return "screenshot.rectangle.clipboard"; } }
        internal override string Label { get { return "Capture area to clipboard"; } }
        internal override string Shortcut { get { return "Alt + S"; } }
        internal override uint Modifiers { get { return 1; } }
        internal override uint Key { get { return 0x53; } }
        internal override void Execute(Action<string> completed)
        {
            if (overlay != null) return;
            Rectangle bounds = SystemInformation.VirtualScreen;
            IntPtr previousWindow = Native.GetForegroundWindow();
            Bitmap frozen = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(frozen))
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
                overlay = new CaptureOverlay(frozen, bounds);
                overlay.Result += completed;
                overlay.FormClosed += delegate {
                    overlay = null;
                    if (previousWindow != IntPtr.Zero && Native.IsWindow(previousWindow)) Native.SetForegroundWindow(previousWindow);
                };
                overlay.Show();
                overlay.Activate();
                Native.SetForegroundWindow(overlay.Handle);
            }
            catch
            {
                if (overlay != null) { overlay.Dispose(); overlay = null; }
                else frozen.Dispose();
                throw;
            }
        }
    }
internal sealed class CaptureOverlay : Form
{
    private readonly Bitmap frozen;
    private bool selecting;
    private Point anchor;
    private Rectangle selection;
    internal event Action<string> Result;

    internal CaptureOverlay(Bitmap image, Rectangle desktopBounds)
    {
        frozen = image;
        Text = "Alt+S — select screenshot area";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Bounds = desktopBounds;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs args)
    {
        Graphics g = args.Graphics;
        g.DrawImageUnscaled(frozen, Point.Empty);
        using (Brush shade = new SolidBrush(Color.FromArgb(85, Color.Black)))
            g.FillRectangle(shade, ClientRectangle);
        if (selection.Width > 0 && selection.Height > 0)
        {
            g.DrawImage(frozen, selection, selection, GraphicsUnit.Pixel);
            using (Pen outer = new Pen(Color.Black, 3))
                g.DrawRectangle(outer, selection.X, selection.Y, selection.Width - 1, selection.Height - 1);
            using (Pen inner = new Pen(Color.White, 1))
                g.DrawRectangle(inner, selection.X, selection.Y, selection.Width - 1, selection.Height - 1);
        }
        else
        {
            string hint = "Drag to crop  •  Release to copy  •  Esc to cancel";
            using (Font font = new Font("Segoe UI", 12))
            using (Brush background = new SolidBrush(Color.FromArgb(220, 25, 25, 25)))
            {
                Point pointer = PointToClient(MousePosition);
                SizeF textSize = g.MeasureString(hint, font);
                float x = Math.Max(10, Math.Min(pointer.X + 20, ClientSize.Width - textSize.Width - 30));
                float y = Math.Max(10, Math.Min(pointer.Y + 25, ClientSize.Height - textSize.Height - 30));
                g.FillRectangle(background, x - 8, y - 6, textSize.Width + 16, textSize.Height + 12);
                g.DrawString(hint, font, Brushes.White, x, y);
            }
        }
    }

    private Point Clamp(Point point)
    {
        return new Point(Math.Max(0, Math.Min(frozen.Width, point.X)), Math.Max(0, Math.Min(frozen.Height, point.Y)));
    }
    private void UpdateSelection(Point point)
    {
        selection = Selection.Rectangle(anchor, point, frozen.Size);
        Invalidate();
    }
    protected override void OnMouseDown(MouseEventArgs args)
    {
        if (args.Button == MouseButtons.Right) { Close(); return; }
        if (args.Button != MouseButtons.Left) return;
        anchor = Clamp(args.Location);
        selecting = true;
        Capture = true;
        UpdateSelection(args.Location);
    }
    protected override void OnMouseMove(MouseEventArgs args)
    {
        if (selecting) UpdateSelection(args.Location);
    }
    protected override void OnMouseUp(MouseEventArgs args)
    {
        if (!selecting || args.Button != MouseButtons.Left) return;
        UpdateSelection(args.Location);
        selecting = false;
        Capture = false;
        if (selection.Width < 2 || selection.Height < 2) { selection = Rectangle.Empty; Invalidate(); return; }
        Hide();
        try
        {
            using (Bitmap crop = frozen.Clone(selection, PixelFormat.Format32bppArgb))
            using (MemoryStream png = new MemoryStream())
            {
                CaptureClipboard.Write(crop);
            }
            AppPaths.Log("Copied " + selection.Width + "x" + selection.Height + " image to clipboard.");
            if (Result != null) Result("Copied " + selection.Width + " × " + selection.Height + " screenshot");
        }
        catch (Exception ex)
        {
            AppPaths.Log("Clipboard error: " + ex);
            if (Result != null) Result("Clipboard busy; try again");
        }
        Close();
    }
    protected override void OnKeyDown(KeyEventArgs args)
    {
        if (args.KeyCode == Keys.Escape) { args.Handled = true; Close(); }
        base.OnKeyDown(args);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) frozen.Dispose();
        base.Dispose(disposing);
    }
}


}
