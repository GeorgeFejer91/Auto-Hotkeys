using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AutoHotkeys
{
    internal static class Selection
    {
        internal static Rectangle Rectangle(Point anchor, Point current, Size bounds)
        {
            anchor = new Point(Math.Max(0, Math.Min(bounds.Width, anchor.X)), Math.Max(0, Math.Min(bounds.Height, anchor.Y)));
            current = new Point(Math.Max(0, Math.Min(bounds.Width, current.X)), Math.Max(0, Math.Min(bounds.Height, current.Y)));
            return System.Drawing.Rectangle.FromLTRB(Math.Min(anchor.X, current.X), Math.Min(anchor.Y, current.Y), Math.Max(anchor.X, current.X), Math.Max(anchor.Y, current.Y));
        }
    }
    internal static class CaptureClipboard
    {
        internal static void Write(Bitmap image)
        {
            using (MemoryStream png = new MemoryStream())
            {
                image.Save(png, ImageFormat.Png);
                png.Position = 0;
                DataObject data = new DataObject();
                data.SetImage(image);
                data.SetData("PNG", false, png);
                Clipboard.SetDataObject(data, true, 10, 100);
            }
        }
    }
}
