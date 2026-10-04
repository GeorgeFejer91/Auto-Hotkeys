using System;
using System.Collections.Generic;
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
        private readonly List<string> history = new List<string>();
        private readonly TableLayoutPanel layout;
        private readonly Label instructions, closing;
        private readonly FlowLayoutPanel footer;
        private readonly Button previous, next;
        private const int PageSize = 3;
        private int page;
        private bool arranging;
        private float readableFontSize = 10;
        private Font adaptiveFont;
        private bool updating;
        internal bool AllowClose;
        internal event Action<bool> StartupChanged;
        internal event Action<HotkeyEntry, bool> ActionEnabledChanged;

        internal MainForm(HotkeyManager hotkeys, Settings settings)
        {
            manager = hotkeys;
            Text = "Auto-Hotkeys";
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(730, 520);
            MinimumSize = SizeFromClientSize(new Size(640, 440));
            StartPosition = FormStartPosition.CenterScreen;
            using (System.IO.Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream("AutoHotkeys.App.ico"))
                Icon = stream == null ? SystemIcons.Application : new Icon(stream);
            layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 11, AutoScroll = false };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            startup = new CheckBox { AutoSize = true, Text = "Start with Windows", Checked = settings.AutoStart, Anchor = AnchorStyles.Left };
            startup.CheckedChanged += delegate { if (!updating && StartupChanged != null) StartupChanged(startup.Checked); };
            actions = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, ScrollBars = ScrollBars.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle, AccessibleName = "Hotkey actions" };
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
            activity = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = false, AccessibleName = "Three latest activity results; full text is available in Activity history" };
            footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            Button hide = new Button { Text = "Hide", AutoSize = true };
            hide.Click += delegate { Hide(); };
            Button run = new Button { Text = "Run selected", AutoSize = true };
            run.Click += delegate { if (actions.SelectedRows.Count > 0) manager.Run((HotkeyEntry)actions.SelectedRows[0].Tag); };
            Button details = new Button { Text = "Details", AutoSize = true };
            details.Click += delegate {
                if (actions.SelectedRows.Count == 0) return;
                HotkeyEntry entry = (HotkeyEntry)actions.SelectedRows[0].Tag;
                ShowDetails(entry.Action.Label, entry.Action.Shortcut + Environment.NewLine + entry.Status);
            };
            Button activityDetails = new Button { Text = "Activity history", AutoSize = true };
            activityDetails.Click += delegate { ShowDetails("Activity history", history.Count == 0 ? "No activity yet." : string.Join(Environment.NewLine, history)); };
            previous = new Button { Text = "Previous", AutoSize = true, Visible = false };
            next = new Button { Text = "Next", AutoSize = true, Visible = false };
            previous.Click += delegate { page--; RefreshActions(); };
            next.Click += delegate { page++; RefreshActions(); };
            footer.Controls.Add(run); footer.Controls.Add(details); footer.Controls.Add(activityDetails);
            footer.Controls.Add(previous); footer.Controls.Add(next); footer.Controls.Add(hide);
            layout.Controls.Add(status, 0, 0);
            layout.Controls.Add(startup, 0, 1);
            instructions = new Label { Text = "Alt+S: drag to crop, release to copy, then paste with Ctrl+V.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            closing = new Label { Text = "Closing this window keeps hotkeys running in the system tray.", Dock = DockStyle.Fill, ForeColor = SystemColors.GrayText };
            layout.Controls.Add(actions, 0, 3);
            layout.Controls.Add(instructions, 0, 4);
            layout.Controls.Add(new Label { Text = "Recent activity", Dock = DockStyle.Fill }, 0, 6);
            layout.Controls.Add(activity, 0, 7);
            layout.Controls.Add(closing, 0, 9);
            layout.Controls.Add(footer, 0, 10);
            Controls.Add(layout);
            SizeChanged += delegate { ArrangeStretch(); };
            FontChanged += delegate { if (!arranging) { readableFontSize = Math.Max(10, Font.SizeInPoints); ArrangeStretch(); } };
            RefreshActions();
            FormClosing += delegate(object sender, FormClosingEventArgs args) {
                if (!AllowClose && args.CloseReason == CloseReason.UserClosing) { args.Cancel = true; Hide(); }
            };
        }
        internal void RefreshActions()
        {
            updating = true;
            int lastPage = Math.Max(0, (manager.Entries.Count - 1) / PageSize);
            page = Math.Max(0, Math.Min(page, lastPage));
            actions.Rows.Clear();
            foreach (HotkeyEntry entry in manager.Entries.Skip(page * PageSize).Take(PageSize))
            {
                string briefStatus = entry.Registered ? "Active" : entry.Enabled ? "Unavailable" : "Off";
                int row = actions.Rows.Add(entry.Enabled, entry.Action.Shortcut, entry.Action.Label, briefStatus);
                actions.Rows[row].Tag = entry;
                actions.Rows[row].Cells[3].ToolTipText = entry.Status;
            }
            previous.Visible = next.Visible = lastPage > 0;
            previous.Enabled = page > 0; next.Enabled = page < lastPage;
            int count = manager.Entries.Count(e => e.Registered);
            status.Text = "Running — " + count + (count == 1 ? " active action" : " active actions");
            updating = false;
            ArrangeStretch();
        }
        internal void SetStartup(bool on) { updating = true; startup.Checked = on; updating = false; }
        internal void RecordActivity(string result)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + result;
            history.Insert(0, line);
            while (history.Count > 50) history.RemoveAt(history.Count - 1);
            activity.Items.Insert(0, line);
            while (activity.Items.Count > 3) activity.Items.RemoveAt(activity.Items.Count - 1);
        }
        // Allocate the native window first; surplus height belongs between groups.
        // Native text measurement is used here because this is not an HTML surface.
        private void ArrangeStretch()
        {
            if (arranging || footer == null) return;
            arranging = true;
            layout.SuspendLayout();
            try
            {
                using (Graphics canvas = CreateGraphics())
                {
                    float dpi = canvas.DpiX / 96f;
                    float budget = Math.Min(ClientSize.Width / (730f * dpi), ClientSize.Height / (520f * dpi));
                    float typeSize = readableFontSize + Math.Max(0, Math.Min(2, (float)Math.Floor((budget - 1) * 2)));
                    if (Math.Abs(Font.SizeInPoints - typeSize) > .1f)
                    {
                        Font old = adaptiveFont;
                        adaptiveFont = new Font(Font.FontFamily, typeSize, FontStyle.Regular);
                        Font = adaptiveFont;
                        if (old != null) old.Dispose();
                    }
                    int lineHeight = TextRenderer.MeasureText(canvas, "Ag", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
                    int inset = (int)Math.Round(Math.Max(12, Math.Min(24, 16 * budget)) * dpi);
                    layout.Padding = new Padding(inset);
                    int width = Math.Max(1, ClientSize.Width - inset * 2 - 6);
                    TextFormatFlags wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                    layout.RowStyles[0].Height = lineHeight + 8 * dpi;
                    layout.RowStyles[1].Height = lineHeight + 12 * dpi;
                    actions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                    actions.ColumnHeadersHeight = lineHeight + (int)(12 * dpi);
                    foreach (DataGridViewRow row in actions.Rows) row.Height = lineHeight + (int)(12 * dpi);
                    layout.RowStyles[3].Height = actions.ColumnHeadersHeight + actions.Rows.Cast<DataGridViewRow>().Sum(r => r.Height) + 8 * dpi;
                    layout.RowStyles[4].Height = TextRenderer.MeasureText(canvas, instructions.Text, Font, new Size(width, int.MaxValue), wrap).Height + 8 * dpi;
                    layout.RowStyles[6].Height = lineHeight + 8 * dpi;
                    layout.RowStyles[7].Height = activity.ItemHeight * 3 + 12 * dpi;
                    layout.RowStyles[9].Height = TextRenderer.MeasureText(canvas, closing.Text, Font, new Size(width, int.MaxValue), wrap).Height + 8 * dpi;
                    int buttonWidth = 0, buttonHeight = 0;
                    foreach (Button button in footer.Controls.OfType<Button>())
                    {
                        if ((button == next || button == previous) && manager.Entries.Count <= PageSize) continue;
                        Size preferred = button.GetPreferredSize(Size.Empty);
                        buttonWidth += preferred.Width + button.Margin.Horizontal;
                        buttonHeight = Math.Max(buttonHeight, preferred.Height + button.Margin.Vertical);
                    }
                    layout.RowStyles[10].Height = buttonHeight + 8 * dpi;
                    int requiredHeight = (int)Math.Ceiling(layout.RowStyles.Cast<RowStyle>().Where(r => r.SizeType == SizeType.Absolute).Sum(r => r.Height)) + inset * 2 + (int)(36 * dpi);
                    MinimumSize = SizeFromClientSize(new Size(Math.Max((int)(640 * dpi), buttonWidth + inset * 2 + 12), Math.Max((int)(440 * dpi), requiredHeight)));
                }
            }
            finally { layout.ResumeLayout(true); arranging = false; }
        }
        private void ShowDetails(string title, string text)
        {
            using (Form details = new Form { Text = title, Font = Font, ClientSize = new Size(600, 360), MinimumSize = new Size(420, 260), StartPosition = FormStartPosition.CenterParent })
            {
                TextBox fullText = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true, Text = text, AccessibleName = title };
                details.Controls.Add(fullText);
                details.ShowDialog(this);
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && adaptiveFont != null) { adaptiveFont.Dispose(); adaptiveFont = null; }
        }
        internal void ShowError(string message) { RecordActivity(message); MessageBox.Show(this, message, "Auto-Hotkeys", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
