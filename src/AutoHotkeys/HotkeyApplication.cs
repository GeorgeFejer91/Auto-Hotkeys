using System;
using System.Threading;
using System.Windows.Forms;

namespace AutoHotkeys
{
    internal sealed class HotkeyApplication : ApplicationContext
    {
        private readonly Settings settings;
        private readonly HotkeyManager manager;
        private readonly MainForm form;
        private readonly NotifyIcon tray;
        private readonly EventWaitHandle openEvent, stopEvent;
        private readonly System.Windows.Forms.Timer events;
        private readonly ToolStripMenuItem startupMenu;
        private bool disposed;

        internal HotkeyApplication(bool show)
        {
            settings = Settings.Load();
            string startupError = null;
            try { StartupManager.SetEnabled(settings.AutoStart); settings.Save(); }
            catch (Exception ex) { startupError = ex.Message; AppPaths.Log("Autostart error: " + ex); }
            manager = new HotkeyManager(settings);
            form = new MainForm(manager, settings);
            IntPtr formHandle = form.Handle;
            form.StartupChanged += SetStartup;
            form.ActionEnabledChanged += delegate(HotkeyEntry entry, bool enabled) {
                if (enabled) settings.DisabledActions.Remove(entry.Action.Id);
                else if (!settings.DisabledActions.Contains(entry.Action.Id)) settings.DisabledActions.Add(entry.Action.Id);
                try { settings.Save(); manager.SetEnabled(entry, enabled); }
                catch (Exception ex) { form.ShowError(ex.Message); }
            };
            tray = new NotifyIcon { Icon = form.Icon, Text = "Auto-Hotkeys — Alt+S screen capture", Visible = true };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open Auto-Hotkeys", null, delegate { ShowWindow(); });
            foreach (HotkeyEntry entry in manager.Entries)
            {
                HotkeyEntry current = entry;
                menu.Items.Add(current.Action.Label + " (" + current.Action.Shortcut + ")", null, delegate { manager.Run(current); });
            }
            menu.Items.Add(new ToolStripSeparator());
            startupMenu = new ToolStripMenuItem("Start with Windows") { Checked = settings.AutoStart };
            startupMenu.Click += delegate { SetStartup(!settings.AutoStart); };
            menu.Items.Add(startupMenu);
            menu.Items.Add("Quit until next sign-in", null, delegate {
                try { StartupManager.PauseRecovery(); ExitThread(); }
                catch (Exception ex) { form.ShowError(ex.Message); }
            });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowWindow(); };
            manager.Changed += delegate { form.BeginInvoke((Action)form.RefreshActions); };
            manager.Activity += delegate(string result) { form.RecordActivity(result); };
            openEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.OpenEventName);
            stopEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.StopEventName);
            events = new System.Windows.Forms.Timer { Interval = 250 };
            events.Tick += delegate {
                if (stopEvent.WaitOne(0)) { ExitThread(); return; }
                if (openEvent.WaitOne(0)) ShowWindow();
            };
            events.Start();
            AppPaths.Log("Auto-Hotkeys ready. PID=" + System.Diagnostics.Process.GetCurrentProcess().Id);
            if (show) ShowWindow();
            if (startupError != null)
            {
                form.RecordActivity("Could not set automatic startup: " + startupError);
                if (show) form.ShowError("Could not set automatic startup: " + startupError);
            }
        }

        private void SetStartup(bool on)
        {
            bool previous = settings.AutoStart;
            try
            {
                StartupManager.SetEnabled(on);
                settings.AutoStart = on;
                settings.Save();
                startupMenu.Checked = on;
                form.SetStartup(on);
                form.RecordActivity("Start with Windows: " + (on ? "On" : "Off"));
            }
            catch (Exception ex)
            {
                settings.AutoStart = previous;
                try { StartupManager.SetEnabled(previous); } catch { }
                form.SetStartup(previous);
                form.ShowError("Could not change automatic startup: " + ex.Message);
            }
        }
        private void ShowWindow() { form.Show(); form.WindowState = FormWindowState.Normal; form.Activate(); Native.SetForegroundWindow(form.Handle); }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                events.Stop(); events.Dispose();
                openEvent.Dispose(); stopEvent.Dispose();
                manager.Dispose();
                tray.Visible = false; tray.ContextMenuStrip.Dispose(); tray.Dispose();
                form.AllowClose = true; form.Close(); form.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
