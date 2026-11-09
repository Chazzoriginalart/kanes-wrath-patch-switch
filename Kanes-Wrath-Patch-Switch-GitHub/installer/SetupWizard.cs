using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

class Setup : Form {
    readonly TextBox folder = new TextBox();
    readonly Button browse = new Button();
    readonly Button install = new Button();
    readonly Label status = new Label();
    readonly ProgressBar progress = new ProgressBar();
    bool busy;
    static readonly Color Ink = Color.FromArgb(18, 23, 21);
    static readonly Color Amber = Color.FromArgb(235, 191, 107);

    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Setup());
    }

    Setup() {
        Text = "Kane's Wrath Patch Switch Setup";
        ClientSize = new Size(714, 435);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Ink;
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;

        AddLabel("COMMAND & CONQUER 3  /  KANE'S WRATH", 28, 24, 650, 27, 11, Amber);
        AddLabel("KANE'S WRATH PATCH SWITCH", 27, 57, 660, 51, 26, Color.WhiteSmoke);
        AddLabel("R25j FIXES R3  •  COMMUNITY RELEASE", 29, 117, 650, 26, 12, Amber);
        var divider = new Panel { Bounds = new Rectangle(29, 153, 656, 2), BackColor = Amber };
        Controls.Add(divider);
        AddLabel("Install the repaired R25j core, official archives and four updated maps.", 29, 176, 653, 28, 10, Color.Gainsboro);
        AddLabel("Kane's Wrath game folder", 29, 221, 500, 25, 10, Color.WhiteSmoke);

        folder.SetBounds(29, 251, 543, 27);
        folder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            @"Steam\steamapps\common\Command and Conquer 3 - Kane's Wrath");
        Controls.Add(folder);
        StyleButton(browse, "Browse…", 583, 248, 102, 32, false);
        browse.Click += delegate {
            using (var dialog = new FolderBrowserDialog()) {
                dialog.Description = "Select the folder containing CNC3EP1.exe.";
                if (Directory.Exists(folder.Text)) dialog.SelectedPath = folder.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK) folder.Text = dialog.SelectedPath;
            }
        };
        status.SetBounds(29, 293, 656, 50);
        status.Text = "Close Kane's Wrath and WorldBuilder before installing.";
        status.ForeColor = Color.Gainsboro;
        Controls.Add(status);
        progress.SetBounds(29, 349, 656, 11);
        Controls.Add(progress);
        StyleButton(install, "Install patch", 539, 379, 146, 37, true);
        install.Click += async delegate { await Install(); };
        var close = new Button();
        StyleButton(close, "Close", 431, 379, 98, 37, false);
        close.Click += delegate { Close(); };
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) e.Cancel = true; };
        var welcome = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        welcome.Controls.Add(new Panel { Bounds = new Rectangle(0, 0, 10, 435), BackColor = Amber });
        var title = new Label { Text = "Welcome to Kane's Wrath Patch Switch Setup", Bounds = new Rectangle(32, 30, 650, 77), Font = new Font("Segoe UI", 23, FontStyle.Bold), ForeColor = Color.WhiteSmoke };
        welcome.Controls.Add(title);
        welcome.Controls.Add(new Label { Text = "R25j FIXES R3  •  COMMUNITY RELEASE", Bounds = new Rectangle(34, 107, 645, 28), ForeColor = Amber, Font = new Font("Segoe UI", 12, FontStyle.Bold) });
        welcome.Controls.Add(new Label {
            Text = "Repaired R25j core and official map archives\r\n\r\n  1v1 Map Pack   •   2v2 Map Pack\r\n  3v3 / 4v4 Map Pack   •   Legacy Map Pack\r\n\r\nIncludes four updated custom maps. 4K is optional.\r\n\r\nThanks to Masterleaf and the Kane's Wrath community.",
            Bounds = new Rectangle(34, 154, 640, 206), ForeColor = Color.Gainsboro
        });
        var next = new Button { Text = "Next >", Bounds = new Rectangle(539, 379, 146, 37), BackColor = Amber, ForeColor = Ink, FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Cancel", Bounds = new Rectangle(431, 379, 98, 37), FlatStyle = FlatStyle.Flat };
        next.Click += delegate { Controls.Remove(welcome); welcome.Dispose(); AcceptButton = install; folder.Focus(); };
        cancel.Click += delegate { Close(); };
        welcome.Controls.Add(next);
        welcome.Controls.Add(cancel);
        Controls.Add(welcome);
        welcome.BringToFront();
        AcceptButton = next;
    }

    void AddLabel(string text, int x, int y, int w, int h, float size, Color color) {
        Controls.Add(new Label { Text = text, Bounds = new Rectangle(x,y,w,h), ForeColor = color,
            Font = new Font("Segoe UI",size,FontStyle.Bold) });
    }

    void StyleButton(Button button, string text, int x, int y, int w, int h, bool primary) {
        button.Text = text;
        button.Bounds = new Rectangle(x,y,w,h);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Amber;
        button.BackColor = primary ? Amber : Color.FromArgb(37, 44, 40);
        button.ForeColor = primary ? Ink : Color.WhiteSmoke;
        Controls.Add(button);
    }

    async Task Install() {
        string game = folder.Text.Trim().Trim('"');
        string root = AppDomain.CurrentDomain.BaseDirectory;
        string helper = Path.Combine(root, "ReleaseInstall.exe");
        string payload = Path.Combine(root, "Payload");
        if (!File.Exists(helper) || !File.Exists(Path.Combine(payload,"payload.sha256"))) {
            status.Text = "Put this Setup window beside Install.cmd in the extracted R25j Fixes R3 folder.";
            return;
        }
        if (!File.Exists(Path.Combine(game,"CNC3EP1.exe")) || !Directory.Exists(Path.Combine(game,"Core"))) {
            status.Text = "Select the Kane's Wrath folder containing CNC3EP1.exe and Core.";
            return;
        }
        string maps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Command & Conquer 3 Kane's Wrath\Maps");
        string log = Path.Combine(Path.GetTempPath(),"KW-R3-" + Guid.NewGuid().ToString("N") + ".txt");
        busy = true;
        install.Enabled = browse.Enabled = folder.Enabled = false;
        progress.Style = ProgressBarStyle.Marquee;
        status.Text = "Verifying files, backing up the current patch and installing…";
        try {
            var start = new ProcessStartInfo(helper, Quote(payload) + " " + Quote(game) + " " + Quote(maps) + " " + Quote(log));
            start.UseShellExecute = true;
            start.Verb = "runas";
            start.WindowStyle = ProcessWindowStyle.Hidden;
            int code = await Task.Run(delegate {
                using (var process = Process.Start(start)) {
                    if (process == null) throw new InvalidOperationException("The installer did not start.");
                    process.WaitForExit();
                    return process.ExitCode;
                }
            });
            string details = File.Exists(log) ? File.ReadAllText(log).Trim() : "Installer result details were unavailable.";
            progress.Style = ProgressBarStyle.Blocks;
            progress.Value = code == 0 ? 100 : 0;
            status.Text = code == 0 ? "Installation complete. The patch and maps were verified." : "Installation stopped. See the details below.";
            MessageBox.Show(this,details,Text,MessageBoxButtons.OK,code == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        } catch (System.ComponentModel.Win32Exception e) {
            status.Text = e.NativeErrorCode == 1223 ? "Installation canceled at the Windows permission prompt." : e.Message;
        } catch (Exception e) {
            status.Text = e.Message;
        } finally {
            progress.Style = ProgressBarStyle.Blocks;
            busy = false;
            install.Enabled = browse.Enabled = folder.Enabled = true;
        }
    }

    static string Quote(string value) {
        return "\"" + value.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "\"";
    }
}
