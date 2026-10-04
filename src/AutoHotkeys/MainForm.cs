using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AutoHotkeys
{
    internal sealed class MainForm : Form
    {
        private readonly HotkeyManager manager;
        private readonly DataGridView actions;
        private readonly CheckBox startup;
        private readonly Label status;
        private readonly ListBox activity;
        private bool updating;
        internal bool AllowClose;
        internal event Action<bool> StartupChanged;
        internal event Action<HotkeyEntry, bool> ActionEnabledChanged;

        internal MainForm(HotkeyManager hotkeys, Settings settings)
        {
            manager = hotkeys;
            Text = "Auto-Hotkeys";
            Font = new Font("Segoe UI", 10);
            ClientSize = new Size(730, 440);
            MinimumSize = new Size(620, 390);
            StartPosition = FormStartPosition.CenterScreen;
            using (System.IO.Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream("AutoHotkeys.App.ico"))
                Icon = stream == null ? SystemIcons.Application : new Icon(stream);
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 8 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            startup = new CheckBox { AutoSize = true, Text = "Start with Windows", Checked = settings.AutoStart, Anchor = AnchorStyles.Left };
            startup.CheckedChanged += delegate { if (!updating && StartupChanged != null) StartupChanged(startup.Checked); };
            actions = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle };
            actions.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "On", FillWeight = 32 });
            actions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Shortcut", HeaderText = "Shortcut", ReadOnly = true, FillWeight = 65 });
            actions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", ReadOnly = true, FillWeight = 180 });
            actions.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", ReadOnly = true, FillWeight = 95 });
            actions.CurrentCellDirtyStateChanged += delegate { if (actions.IsCurrentCellDirty) actions.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            actions.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args) {
                if (updating || args.RowIndex < 0 || args.ColumnIndex != 0) return;
                HotkeyEntry entry = (HotkeyEntry)actions.Rows[args.RowIndex].Tag;
                if (ActionEnabledChanged != null) ActionEnabledChanged(entry, Convert.ToBoolean(actions.Rows[args.RowIndex].Cells[0].Value));
            };
            activity = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true };
            FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            Button hide = new Button { Text = "Hide", AutoSize = true };
            hide.Click += delegate { Hide(); };
            Button run = new Button { Text = "Run selected", AutoSize = true };
            run.Click += delegate { if (actions.SelectedRows.Count > 0) manager.Run((HotkeyEntry)actions.SelectedRows[0].Tag); };
            footer.Controls.Add(hide); footer.Controls.Add(run);
            layout.Controls.Add(status, 0, 0);
            layout.Controls.Add(startup, 0, 1);
            layout.Controls.Add(actions, 0, 2);
            layout.Controls.Add(new Label { Text = "Alt+S: drag to crop, release to copy, then paste with Ctrl+V.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
            layout.Controls.Add(new Label { Text = "Recent activity", Dock = DockStyle.Fill }, 0, 4);
            layout.Controls.Add(activity, 0, 5);
            layout.Controls.Add(new Label { Text = "Closing this window keeps hotkeys running in the system tray.", Dock = DockStyle.Fill, ForeColor = SystemColors.GrayText, Font = new Font(Font.FontFamily, 9) }, 0, 6);
            layout.Controls.Add(footer, 0, 7);
            Controls.Add(layout);
            RefreshActions();
            FormClosing += delegate(object sender, FormClosingEventArgs args) {
                if (!AllowClose && args.CloseReason == CloseReason.UserClosing) { args.Cancel = true; Hide(); }
            };
        }
        internal void RefreshActions()
        {
            updating = true;
            actions.Rows.Clear();
            foreach (HotkeyEntry entry in manager.Entries)
            {
                int row = actions.Rows.Add(entry.Enabled, entry.Action.Shortcut, entry.Action.Label, entry.Status);
                actions.Rows[row].Tag = entry;
            }
            int count = manager.Entries.Count(e => e.Registered);
            status.Text = "Running — " + count + (count == 1 ? " active action" : " active actions");
            updating = false;
        }
        internal void SetStartup(bool on) { updating = true; startup.Checked = on; updating = false; }
        internal void RecordActivity(string result)
        {
            activity.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + result);
            while (activity.Items.Count > 50) activity.Items.RemoveAt(activity.Items.Count - 1);
        }
        internal void ShowError(string message) { RecordActivity(message); MessageBox.Show(this, message, "Auto-Hotkeys", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
