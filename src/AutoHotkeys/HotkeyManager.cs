using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoHotkeys
{
    internal abstract class HotkeyAction
    {
        internal abstract string Id { get; }
        internal abstract string Label { get; }
        internal abstract string Shortcut { get; }
        internal abstract uint Modifiers { get; }
        internal abstract uint Key { get; }
        internal abstract void Execute(Action<string> completed);
    }
    internal static class ActionCatalog
    {
        internal static List<HotkeyAction> Create()
        {
            // Add future actions here. The window and tray populate from this catalog.
            return new List<HotkeyAction> { new ScreenshotAction() };
        }
    }
    internal sealed class HotkeyEntry
    {
        internal HotkeyAction Action;
        internal bool Enabled;
        internal bool Registered;
        internal string Status;
        internal int RegistrationId;
    }
    internal sealed class HotkeyManager : NativeWindow, IDisposable
    {
        internal readonly List<HotkeyEntry> Entries = new List<HotkeyEntry>();
        internal event Action Changed;
        internal event Action<string> Activity;
        private readonly System.Windows.Forms.Timer retry;

        internal HotkeyManager(Settings settings)
        {
            CreateHandle(new CreateParams { Caption = "Auto-Hotkeys background", Parent = new IntPtr(-3) });
            int id = 1;
            foreach (HotkeyAction action in ActionCatalog.Create())
                Entries.Add(new HotkeyEntry { Action = action, Enabled = !settings.DisabledActions.Contains(action.Id), RegistrationId = id++ });
            foreach (HotkeyEntry entry in Entries) Register(entry);
            retry = new System.Windows.Forms.Timer { Interval = 10000 };
            retry.Tick += delegate {
                foreach (HotkeyEntry entry in Entries.Where(e => e.Enabled && !e.Registered)) Register(entry);
            };
            retry.Start();
        }
        private void Register(HotkeyEntry entry)
        {
            if (!entry.Enabled) { entry.Status = "Off"; return; }
            entry.Registered = Native.RegisterHotKey(Handle, entry.RegistrationId, entry.Action.Modifiers | 0x4000, entry.Action.Key);
            entry.Status = entry.Registered ? "Active" : "Shortcut unavailable (" + Marshal.GetLastWin32Error() + ")";
            AppPaths.Log(entry.Action.Shortcut + ": " + entry.Status);
            if (Changed != null) Changed();
        }
        internal void SetEnabled(HotkeyEntry entry, bool enabled)
        {
            if (entry.Registered) Native.UnregisterHotKey(Handle, entry.RegistrationId);
            entry.Registered = false;
            entry.Enabled = enabled;
            Register(entry);
            if (Changed != null) Changed();
        }
        internal void Run(HotkeyEntry entry)
        {
            if (!entry.Enabled) return;
            try
            {
                entry.Action.Execute(delegate(string result) {
                    string message = entry.Action.Shortcut + " — " + result;
                    AppPaths.Log(message);
                    if (Activity != null) Activity(message);
                });
            }
            catch (Exception ex)
            {
                AppPaths.Log("Action failed: " + ex);
                if (Activity != null) Activity(entry.Action.Shortcut + " — " + ex.Message);
            }
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0312)
            {
                int id = message.WParam.ToInt32();
                HotkeyEntry entry = Entries.FirstOrDefault(e => e.RegistrationId == id);
                if (entry != null) Run(entry);
            }
            base.WndProc(ref message);
        }
        public void Dispose()
        {
            retry.Stop(); retry.Dispose();
            foreach (HotkeyEntry entry in Entries.Where(e => e.Registered)) Native.UnregisterHotKey(Handle, entry.RegistrationId);
            DestroyHandle();
        }
    }
}
