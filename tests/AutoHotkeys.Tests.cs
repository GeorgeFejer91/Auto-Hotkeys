using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using AutoHotkeys;

internal static class Tests
{
    private static int checks;
    [STAThread]
    private static int Main()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), "AutoHotkeys.Tests." + Guid.NewGuid());
        Directory.CreateDirectory(testDirectory);
        AppPaths.StateDirectory = testDirectory;
        try
        {
            Assert(Settings.Load().AutoStart, "Fresh installation defaults to autostart On");
            Settings settings = Settings.Load();
            settings.AutoStart = false;
            settings.DisabledActions.Add("screenshot.rectangle.clipboard");
            settings.Save();
            Settings saved = Settings.Load();
            Assert(!saved.AutoStart && saved.DisabledActions.Contains("screenshot.rectangle.clipboard"), "Settings retain autostart and action switches");
            saved.AutoStart = true; saved.Save();
            Assert(Settings.Load().AutoStart, "Settings can be replaced atomically");
            File.WriteAllText(Path.Combine(testDirectory, "settings.json"), "{}");
            Assert(Settings.Load().AutoStart, "Older settings without autostart keep the default On");
            File.WriteAllText(Path.Combine(testDirectory, "settings.json"), "broken JSON");
            Assert(Settings.Load().AutoStart, "Invalid settings recover without preventing startup");
            Assert(StartupManager.IsMissing(new FileNotFoundException()), "Missing COM task mapped to FileNotFoundException is handled");
            Assert(!StartupManager.IsMissing(new UnauthorizedAccessException()), "Startup access errors are reported, not hidden");

            Size desktop = new Size(100,80);
            Assert(Selection.Rectangle(new Point(70,60),new Point(10,20),desktop) == new Rectangle(10,20,60,40), "Reverse dragging selects the expected rectangle");
            Assert(Selection.Rectangle(new Point(-20,-10),new Point(150,200),desktop) == new Rectangle(0,0,100,80), "Selection stays within the captured desktop");
            Rectangle click = Selection.Rectangle(new Point(5,5),new Point(5,5),desktop);
            Assert(click.Width == 0 && click.Height == 0, "A click without dragging has no area");

            XmlDocument logon = new XmlDocument(); logon.LoadXml(StartupManager.TaskXml(false));
            XmlDocument recovery = new XmlDocument(); recovery.LoadXml(StartupManager.TaskXml(true));
            XmlNamespaceManager ns = new XmlNamespaceManager(logon.NameTable); ns.AddNamespace("t","http://schemas.microsoft.com/windows/2004/02/mit/task");
            Assert(logon.SelectSingleNode("//t:LogonTrigger/t:UserId",ns).InnerText == AppPaths.UserSid, "Startup task belongs to the current Windows account");
            Assert(logon.SelectSingleNode("//t:Exec/t:Command",ns).InnerText == AppPaths.Executable && File.Exists(AppPaths.Executable), "Startup uses a real executable path");
            XmlNamespaceManager rn = new XmlNamespaceManager(recovery.NameTable); rn.AddNamespace("t","http://schemas.microsoft.com/windows/2004/02/mit/task");
            Assert(recovery.SelectSingleNode("//t:Repetition/t:Interval",rn).InnerText == "PT1M", "Background recovery runs every minute");

            using (Bitmap live = new Bitmap(40,30))
            {
                using (Graphics g = Graphics.FromImage(live)) g.Clear(Color.CornflowerBlue);
                using (Bitmap frozen = (Bitmap)live.Clone())
                {
                    using (Graphics g = Graphics.FromImage(live)) g.Clear(Color.Red);
                    Rectangle rectangle = Selection.Rectangle(new Point(25,20),new Point(5,10),live.Size);
                    using (Bitmap crop = frozen.Clone(rectangle,PixelFormat.Format32bppArgb))
                    {
                        Assert(crop.Width == 20 && crop.Height == 10 && crop.GetPixel(0,0).ToArgb() == Color.CornflowerBlue.ToArgb(), "Cropping uses the frozen image and original pixels");
                        CheckClipboard(crop);
                    }
                }
            }
            Assert(ActionCatalog.Create().Count == 1 && ActionCatalog.Create()[0].Id == "screenshot.rectangle.clipboard", "Catalog exposes the initial screenshot action");
            Console.WriteLine("Passed " + checks + " checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(testDirectory,true); }
    }

    private static void CheckClipboard(Bitmap crop)
    {
        DataObject previous = new DataObject();
        IDataObject old = Clipboard.GetDataObject();
        if (old != null)
            foreach (string format in old.GetFormats(false))
            {
                try
                {
                    object value = old.GetData(format,false);
                    Bitmap bitmap = value as Bitmap;
                    Stream stream = value as Stream;
                    if (bitmap != null) value = bitmap.Clone();
                    else if (stream != null) { MemoryStream copy = new MemoryStream(); stream.CopyTo(copy); copy.Position = 0; value = copy; }
                    if (value != null) previous.SetData(format,false,value);
                }
                catch { }
            }
        try
        {
            CaptureClipboard.Write(crop);
            using (Image result = Clipboard.GetImage())
            {
                Assert(result != null && result.Width == 20 && result.Height == 10, "Clipboard receives a pasteable cropped image");
                using (Bitmap pixels = new Bitmap(result)) Assert(pixels.GetPixel(0,0).ToArgb() == Color.CornflowerBlue.ToArgb(), "Clipboard retains captured pixels");
            }
            Assert(Clipboard.ContainsData("PNG"), "PNG is available for image-aware applications");
        }
        finally { if (previous.GetFormats(false).Length > 0) Clipboard.SetDataObject(previous,true,10,100); else Clipboard.Clear(); }
    }
    private static void Assert(bool condition,string label)
    {
        if (!condition) throw new Exception("FAILED: " + label);
        checks++; Console.WriteLine("PASS: " + label);
    }
}
