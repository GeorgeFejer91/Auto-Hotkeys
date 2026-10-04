using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace AutoHotkeys
{
    internal static class StartupManager
    {
        internal static string LogonName { get { return "Auto-Hotkeys-" + AppPaths.UserSid; } }
        internal static string RecoveryName { get { return "Auto-Hotkeys-Recovery-" + AppPaths.UserSid; } }

        internal static void SetEnabled(bool enabled)
        {
            if (!enabled) { Remove(); return; }
            dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
            service.Connect();
            dynamic folder = service.GetFolder("\\");
            try
            {
                folder.RegisterTask(LogonName, TaskXml(false), 6, AppPaths.UserSid, null, 3, null);
                folder.RegisterTask(RecoveryName, TaskXml(true), 6, AppPaths.UserSid, null, 3, null);
            }
            catch
            {
                TryDelete(folder, LogonName);
                TryDelete(folder, RecoveryName);
                throw;
            }
            finally { Marshal.FinalReleaseComObject(folder); Marshal.FinalReleaseComObject(service); }
            AppPaths.Log("Autostart On; sign-in and recovery tasks registered.");
        }

        internal static void Remove()
        {
            WithFolder(delegate(dynamic folder) { TryDelete(folder, RecoveryName); TryDelete(folder, LogonName); });
            AppPaths.Log("Autostart Off; startup tasks removed.");
        }
        internal static void PauseRecovery()
        {
            WithFolder(delegate(dynamic folder) { TryDelete(folder, RecoveryName); });
        }
        internal static bool IsEnabled()
        {
            bool enabled = false;
            WithFolder(delegate(dynamic folder) {
                try { enabled = (bool)folder.GetTask(LogonName).Enabled; }
                catch (Exception ex) { if (!IsMissing(ex)) throw; }
            });
            return enabled;
        }
        private static void WithFolder(Action<dynamic> action)
        {
            dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
            service.Connect();
            dynamic folder = service.GetFolder("\\");
            try { action(folder); }
            finally { Marshal.FinalReleaseComObject(folder); Marshal.FinalReleaseComObject(service); }
        }
        internal static bool IsMissing(Exception ex) { return ex.HResult == unchecked((int)0x80070002) || ex.HResult == unchecked((int)0x8004130F); }
        private static void TryDelete(dynamic folder, string name)
        {
            try { folder.DeleteTask(name, 0); }
            catch (Exception ex) { if (!IsMissing(ex)) throw; }
        }
        internal static string TaskXml(bool recovery)
        {
            string sid = SecurityElement.Escape(AppPaths.UserSid);
            string trigger = recovery
                ? "<TimeTrigger><Repetition><Interval>PT1M</Interval><StopAtDurationEnd>false</StopAtDurationEnd></Repetition><StartBoundary>" + DateTime.Now.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ss") + "</StartBoundary><Enabled>true</Enabled></TimeTrigger>"
                : "<LogonTrigger><Enabled>true</Enabled><UserId>" + sid + "</UserId></LogonTrigger>";
            return "<?xml version=\"1.0\" encoding=\"UTF-16\"?><Task version=\"1.3\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">"
                + "<RegistrationInfo><Description>Auto-Hotkeys: global shortcut actions in the background.</Description></RegistrationInfo>"
                + "<Triggers>" + trigger + "</Triggers><Principals><Principal id=\"User\"><UserId>" + sid + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals>"
                + "<Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><StartWhenAvailable>true</StartWhenAvailable><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><RestartOnFailure><Interval>PT1M</Interval><Count>3</Count></RestartOnFailure></Settings>"
                + "<Actions Context=\"User\"><Exec><Command>" + SecurityElement.Escape(AppPaths.Executable) + "</Command><Arguments>--background</Arguments><WorkingDirectory>" + SecurityElement.Escape(System.IO.Path.GetDirectoryName(AppPaths.Executable)) + "</WorkingDirectory></Exec></Actions></Task>";
        }
    }

    internal static class LegacyMigration
    {
        internal static void RemovePreviousCapture()
        {
            dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
            service.Connect();
            dynamic folder = service.GetFolder("\\");
            try
            {
                try
                {
                    dynamic task = folder.GetTask("AltS Screen Capture");
                    // Only migrate the specific prototype created for this application.
                    string xml = task.Xml;
                    if (xml.Contains("AltSScreenCapture.exe") || xml.Contains("AltS-ScreenSnip.ahk"))
                    { task.Stop(0); folder.DeleteTask("AltS Screen Capture", 0); }
                }
                catch (Exception ex) { if (!StartupManager.IsMissing(ex)) AppPaths.Log("Legacy task: " + ex.Message); }
            }
            finally { Marshal.FinalReleaseComObject(folder); Marshal.FinalReleaseComObject(service); }
            foreach (Process process in Process.GetProcessesByName("AltSScreenCapture"))
            {
                try { if (process.MainModule.FileName.Equals("C:\\ProgramData\\AltSScreenCapture\\AltSScreenCapture.exe", StringComparison.OrdinalIgnoreCase)) { process.Kill(); process.WaitForExit(3000); } }
                catch (Exception ex) { AppPaths.Log("Legacy process: " + ex.Message); }
                finally { process.Dispose(); }
            }
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    string value = key.GetValue("AltSScreenSnip") as string;
                    if (value != null && value.Contains("AltS-ScreenSnip.ahk")) key.DeleteValue("AltSScreenSnip", false);
                }
            }
        }
    }
}
