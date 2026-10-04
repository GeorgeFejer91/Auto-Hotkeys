using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace AutoHotkeys
{
    internal static class Native
    {
        [DllImport("user32.dll", SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern uint GetFinalPathNameByHandle(IntPtr handle, StringBuilder path, uint size, uint flags);
        internal static string PhysicalPath(string path)
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                StringBuilder result = new StringBuilder(32768);
                uint size = GetFinalPathNameByHandle(stream.SafeFileHandle.DangerousGetHandle(), result, (uint)result.Capacity, 0);
                if (size == 0 || size >= result.Capacity) throw new System.ComponentModel.Win32Exception();
                string physical = result.ToString();
                if (physical.StartsWith("\\\\?\\UNC\\")) return "\\\\" + physical.Substring(8);
                return physical.StartsWith("\\\\?\\") ? physical.Substring(4) : physical;
            }
        }
    }
}
