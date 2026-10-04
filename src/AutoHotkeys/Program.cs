using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Auto-Hotkeys")]
[assembly: AssemblyDescription("Windows hotkey actions in the background")]
[assembly: AssemblyCompany("George Fejer")]
[assembly: AssemblyProduct("Auto-Hotkeys")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AutoHotkeys.Tests")]

namespace AutoHotkeys
{
    internal static class Program
    {
        internal const string MutexName = "Local\\AutoHotkeys.App";
        internal const string OpenEventName = "Local\\AutoHotkeys.Open";
        internal const string StopEventName = "Local\\AutoHotkeys.Stop";

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Contains("--prepare-uninstall"))
                {
                    StartupManager.Remove();
                    Signal(StopEventName);
                    WaitForExit();
                    return 0;
                }
                string configure = args.FirstOrDefault(a => a.StartsWith("--configure-autostart="));
                if (configure != null)
                {
                    bool on = configure.EndsWith("=on", StringComparison.OrdinalIgnoreCase);
                    if (!on && !configure.EndsWith("=off", StringComparison.OrdinalIgnoreCase)) return 2;
                    LegacyMigration.RemovePreviousCapture();
                    Settings settings = Settings.Load();
                    StartupManager.SetEnabled(on);
                    settings.AutoStart = on;
                    settings.Save();
                    return 0;
                }
                bool owner;
                using (Mutex mutex = new Mutex(true, MutexName, out owner))
                {
                    if (!owner)
                    {
                        if (!args.Contains("--background")) Signal(OpenEventName);
                        return 0;
                    }
                    try
                    {
                        try { Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); }
                        catch (EntryPointNotFoundException) { Native.SetProcessDPIAware(); }
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                        Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { AppPaths.Log("UI error: " + e.Exception); };
                        using (HotkeyApplication app = new HotkeyApplication(!args.Contains("--background")))
                            Application.Run(app);
                    }
                    finally { mutex.ReleaseMutex(); }
                }
                return 0;
            }
            catch (Exception ex)
            {
                AppPaths.Log("Startup error: " + ex);
                if (!args.Contains("--background") && !args.Contains("--prepare-uninstall") && !args.Any(a => a.StartsWith("--configure-autostart=")))
                    MessageBox.Show(ex.Message, "Auto-Hotkeys could not start", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void Signal(string name)
        {
            try { using (EventWaitHandle handle = EventWaitHandle.OpenExisting(name)) handle.Set(); }
            catch (WaitHandleCannotBeOpenedException) { }
        }
        private static void WaitForExit()
        {
            for (int i = 0; i < 50; i++)
            {
                try { using (Mutex mutex = Mutex.OpenExisting(MutexName)) { } }
                catch (WaitHandleCannotBeOpenedException) { return; }
                Thread.Sleep(100);
            }
        }
    }

    internal static class AppPaths
    {
        internal static string StateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Auto-Hotkeys");
        internal static string Executable { get { return Native.PhysicalPath(Assembly.GetExecutingAssembly().Location); } }
        internal static string UserSid { get { return WindowsIdentity.GetCurrent().User.Value; } }
        internal static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(StateDirectory);
                string file = Path.Combine(StateDirectory, "activity.log");
                if (File.Exists(file) && new FileInfo(file).Length > 262144) File.WriteAllText(file, "");
                File.AppendAllText(file, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
            }
            catch { }
        }
    }
}
