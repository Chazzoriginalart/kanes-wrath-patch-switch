using System.IO.Compression;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class PatchSwitcher : Form
{
    private readonly Label current = new Label { AutoSize = true, Text = "Selected patch: checking..." };
    private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill };
    private readonly Label status = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
    private readonly ComboBox extras = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
    private readonly Button applyExtra = new Button { AutoSize = true, Text = "Switch" };
    private readonly Button add = new Button { AutoSize = true, Text = "Add patch folder..." };
    private bool busy;
    private bool dark = Path.GetFileNameWithoutExtension(Application.ExecutablePath).EndsWith(" Dark", StringComparison.OrdinalIgnoreCase);

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new PatchSwitcher());
    }

    private PatchSwitcher()
    {
        Text = "Kane's Wrath Patch Switcher";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(760, 325);
        MinimumSize = new Size(760, 325);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 8 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Insert(2, new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Insert(5, new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(current, 0, 0);
        layout.Controls.Add(new Label { AutoSize = true, Margin = new Padding(3, 8, 3, 6), Text = "Close Kane's Wrath and World Builder before switching." }, 0, 1);
        layout.Controls.Add(new Label { AutoSize = true, Text = "Supported patch profiles", Margin = new Padding(3, 7, 3, 2) }, 0, 2);
        var extraRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        extraRow.Controls.Add(extras);
        extraRow.Controls.Add(applyExtra);
        extraRow.Controls.Add(add);
        var maps = new Button { AutoSize = true, Text = "Map Manager...", FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(165, 32, 43), ForeColor = Color.White, UseVisualStyleBackColor = false, Padding = new Padding(8, 3, 8, 3) };
        maps.Click += (sender, args) => ShowMaps();
        extraRow.Controls.Add(maps);
        layout.Controls.Add(extraRow, 0, 4);
        var launchRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var launchMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 155 };
        launchMode.Items.AddRange(new object[] { "Steam", "Installed game" });
        launchMode.SelectedIndex = AppDomain.CurrentDomain.BaseDirectory.IndexOf("steamapps", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1;
        var launch = new Button { AutoSize = true, Text = "Launch Game" };
        launch.Click += (sender, args) => LaunchGame(launchMode.SelectedIndex == 0);
        launchRow.Controls.AddRange(new Control[] { launchMode, launch });
        layout.RowCount = 9;
        layout.RowStyles.Insert(5, new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(launchRow, 0, 5);
        layout.Controls.Add(new Label { AutoSize = true, Text = "Switch changes the game patch. The Community Repack installer adds the six custom maps.", Margin = new Padding(3, 8, 3, 6) }, 0, 6);
        layout.Controls.Add(progress, 0, 7);
        layout.Controls.Add(status, 0, 8);
        var theme = new CheckBox { AutoSize = true, Text = "Dark mode", Checked = dark, Anchor = AnchorStyles.Right };
        var header = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Remove(current);
        header.Controls.Add(current, 0, 0);
        header.Controls.Add(theme, 1, 0);
        layout.Controls.Add(header, 0, 0);
        theme.CheckedChanged += (sender, args) =>
        {
            dark = theme.Checked;
            ApplyTheme();
        };
        ApplyTheme();
        Controls.Add(layout);

        LoadExtraProfiles();
        Shown += async (sender, args) => await Run(null);
        extras.SelectedIndexChanged += (sender, args) =>
        {
            applyExtra.Enabled = !busy && extras.SelectedIndex >= 0;
            HighlightActivePatch();
        };
        applyExtra.Click += async (sender, args) =>
        {
            string profile = extras.SelectedItem as string;
            if (profile != null) await Run(profile == RepackProfile ? RepackCore : profile);
        };
        add.Click += async (sender, args) =>
        {
            using (var picker = new FolderBrowserDialog { Description = "Select a named patch folder containing config.txt and its BIG files.", ShowNewFolderButton = false })
            {
                if (picker.ShowDialog(this) == DialogResult.OK)
                    await Run(null, picker.SelectedPath.TrimEnd(Path.DirectorySeparatorChar));
            }
        };
        FormClosing += (sender, args) => { if (busy) args.Cancel = true; };
    }

    private void LaunchGame(bool steam)
    {
        if (busy)
        {
            status.Text = "Wait for the patch switch to finish before launching.";
            return;
        }
        try
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string target = steam ? "steam://rungameid/24810" : Path.Combine(folder, "CNC3EP1.exe");
            if (!steam && !File.Exists(target))
                throw new FileNotFoundException("Keep Patch Switcher beside CNC3EP1.exe in your game folder.");
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = folder });
            status.Text = steam ? "Launch requested through Steam." : "Launch requested through the installed game.";
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, Text); }
    }

    private void ShowMaps()
    {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Command & Conquer 3 Kane's Wrath", "Maps");
        using (var images = new ImageList { ImageSize = new Size(128, 128), ColorDepth = ColorDepth.Depth32Bit })
        using (var dialog = new Form { Text = "C&C Map Manager", StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(970, 650), BackColor = BackColor, ForeColor = ForeColor })
        {
            var list = new ListView { Dock = DockStyle.Fill, View = View.LargeIcon, LargeImageList = images,
                MultiSelect = false, HideSelection = false, AllowDrop = true, ShowItemToolTips = true,
                BackColor = dark ? Color.FromArgb(36, 44, 54) : SystemColors.Window, ForeColor = ForeColor };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(8) };
            var refresh = new Button { AutoSize = true, Text = "Refresh" };
            var open = new Button { AutoSize = true, Text = "Open map folder", Enabled = false };
            var hint = new Label { AutoSize = true, Padding = new Padding(7), Text = "Drop map ZIPs or folders here to install." };
            Action load = () =>
            {
                list.BeginUpdate();
                try
                {
                    list.Items.Clear();
                    images.Images.Clear();
                    if (!Directory.Exists(root)) return;
                    foreach (string folder in Directory.GetDirectories(root))
                    {
                        string name = Path.GetFileName(folder);
                        if (!File.Exists(Path.Combine(folder, name + ".map"))) continue;
                        var item = new ListViewItem(name.Replace('_', ' ')) { Tag = folder };
                        string artwork = Path.Combine(folder, name + "_art.tga");
                        if (!File.Exists(artwork))
                        {
                            string[] candidates = Directory.GetFiles(folder, "*_art.tga");
                            if (candidates.Length == 1) artwork = candidates[0];
                        }
                        foreach (string preview in new[] { artwork, Path.Combine(folder, name + ".tga") })
                        {
                            try
                            {
                                using (var bitmap = ReadMinimap(preview))
                                {
                                    using (var thumbnail = new Bitmap(128, 128))
                                    {
                                    using (var graphics = Graphics.FromImage(thumbnail))
                                    {
                                        graphics.Clear(list.BackColor);
                                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        double scale = 128.0 / Math.Max(bitmap.Width, bitmap.Height);
                                        int width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
                                        int height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
                                        graphics.DrawImage(bitmap, (128 - width) / 2, (128 - height) / 2, width, height);
                                    }
                                    images.Images.Add(thumbnail);
                                    }
                                }
                                item.ImageIndex = images.Images.Count - 1;
                                item.ToolTipText = Path.GetFileName(preview);
                                break;
                            }
                            catch (IOException) { item.ToolTipText = "Preview unavailable"; }
                            catch (ArgumentException) { item.ToolTipText = "Invalid preview"; }
                        }
                        list.Items.Add(item);
                    }
                    list.Sorting = SortOrder.Ascending;
                }
                finally { list.EndUpdate(); }
            };
            list.SelectedIndexChanged += (sender, args) => open.Enabled = list.SelectedItems.Count == 1;
            open.Click += (sender, args) => Process.Start(new ProcessStartInfo((string)list.SelectedItems[0].Tag) { UseShellExecute = true });
            list.DoubleClick += (sender, args) => { if (open.Enabled) open.PerformClick(); };
            refresh.Click += (sender, args) => load();
            list.DragEnter += (sender, args) =>
                args.Effect = args.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            bool importing = false;
            dialog.FormClosing += (sender, args) => { if (importing) args.Cancel = true; };
            list.DragDrop += async (sender, args) =>
            {
                if (importing) return;
                importing = true;
                list.AllowDrop = false;
                refresh.Enabled = false;
                var results = new System.Collections.Generic.List<string>();
                foreach (string path in (string[])args.Data.GetData(DataFormats.FileDrop))
                {
                    try { results.Add(await Task.Run(() => ImportMap(path, root))); }
                    catch (Exception error) { results.Add(Path.GetFileName(path) + ": " + error.Message); }
                }
                importing = false;
                list.AllowDrop = true;
                refresh.Enabled = true;
                load();
                MessageBox.Show(dialog, String.Join(Environment.NewLine, results), "Map import");
            };
            bar.Controls.AddRange(new Control[] { refresh, open, hint });
            dialog.Controls.Add(list);
            dialog.Controls.Add(bar);
            ThemeControls(dialog);
            load();
            dialog.ShowDialog(this);
        }
    }

    private static Bitmap ReadMinimap(string path)
    {
        using (var reader = new BinaryReader(File.OpenRead(path)))
        {
            int id = reader.ReadByte(), palette = reader.ReadByte(), type = reader.ReadByte();
            reader.ReadBytes(9);
            int width = reader.ReadUInt16(), height = reader.ReadUInt16();
            int bits = reader.ReadByte(), flags = reader.ReadByte();
            if (palette != 0 || (type != 2 && type != 10) || (bits != 24 && bits != 32) ||
                width == 0 || height == 0 || width > 512 || height > 512)
                throw new IOException("Unsupported TGA");
            reader.ReadBytes(id);
            var result = new Bitmap(width, height);
            try
            {
                int index = 0;
                while (index < width * height)
                {
                    int header = type == 10 ? reader.ReadByte() : 0;
                    int count = (header & 127) + 1;
                    bool repeat = (header & 128) != 0;
                    if (index + count > width * height) throw new IOException("Invalid TGA packet");
                    Color color = Color.Black;
                    for (int i = 0; i < count; i++, index++)
                    {
                        if (i == 0 || !repeat)
                        {
                            int blue = reader.ReadByte(), green = reader.ReadByte(), red = reader.ReadByte();
                            if (bits == 32) reader.ReadByte();
                            color = Color.FromArgb(red, green, blue);
                        }
                        int x = index % width, y = index / width;
                        result.SetPixel((flags & 16) != 0 ? width - 1 - x : x,
                            (flags & 32) != 0 ? y : height - 1 - y, color);
                    }
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }
    }

    private static void CopyMapFolder(string source, string target, ref long total)
    {
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Linked map folders are not supported.");
        foreach (string file in Directory.GetFiles(source))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked map files are not supported.");
            total += new FileInfo(file).Length;
            if (total > 256L * 1024 * 1024) throw new IOException("Map folder exceeds 256 MB.");
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }
        foreach (string folder in Directory.GetDirectories(source))
        {
            if (Path.GetFileName(folder).StartsWith(".kw-import-", StringComparison.OrdinalIgnoreCase)) continue;
            string child = Path.Combine(target, Path.GetFileName(folder));
            Directory.CreateDirectory(child);
            CopyMapFolder(folder, child, ref total);
        }
    }

    private static string ImportMap(string input, string root)
    {
        Directory.CreateDirectory(root);
        string temp = Path.Combine(root, ".kw-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            if (Directory.Exists(input))
            {
                long total = 0;
                CopyMapFolder(input, temp, ref total);
            }
            else if (String.Equals(Path.GetExtension(input), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                using (var archive = ZipFile.OpenRead(input))
                {
                    long total = 0;
                    foreach (var entry in archive.Entries)
                    {
                        if (String.IsNullOrEmpty(entry.Name)) continue;
                        string target = Path.GetFullPath(Path.Combine(temp, entry.FullName));
                        if (!target.StartsWith(temp + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            throw new IOException("Unsafe archive path.");
                        total += entry.Length;
                        if (total > 256L * 1024 * 1024) throw new IOException("Map ZIP exceeds 256 MB.");
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        entry.ExtractToFile(target);
                    }
                }
            }
            else throw new IOException("Drop a ZIP or a map folder.");
            string[] maps = Directory.GetFiles(temp, "*.map", SearchOption.AllDirectories);
            if (maps.Length == 0) throw new IOException("No playable .map file found.");
            var installed = new System.Collections.Generic.List<string>();
            foreach (string map in maps)
            {
                string name = Path.GetFileNameWithoutExtension(map);
                string dest = Path.Combine(root, name);
                if (Directory.Exists(dest)) { installed.Add(name + " already exists (skipped)."); continue; }
                string staged = Path.Combine(temp, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(staged);
                foreach (string file in Directory.GetFiles(Path.GetDirectoryName(map)))
                {
                    string basename = Path.GetFileName(file);
                    if (basename.Equals(name + ".map", StringComparison.OrdinalIgnoreCase) ||
                        basename.Equals(name + ".tga", StringComparison.OrdinalIgnoreCase) ||
                        basename.Equals(name + "_art.tga", StringComparison.OrdinalIgnoreCase) ||
                        basename.Equals("map.xml", StringComparison.OrdinalIgnoreCase) ||
                        basename.Equals("overrides.xml", StringComparison.OrdinalIgnoreCase) ||
                        basename.Equals("map.str", StringComparison.OrdinalIgnoreCase))
                        File.Copy(file, Path.Combine(staged, basename));
                }
                Directory.Move(staged, dest);
                installed.Add(name + " installed.");
            }
            return String.Join(Environment.NewLine, installed);
        }
        finally { Directory.Delete(temp, true); }
    }

    private void ThemeControls(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Button || control is ComboBox)
            {
                control.BackColor = dark ? Color.FromArgb(43, 53, 69) : SystemColors.Control;
                control.ForeColor = dark ? Color.WhiteSmoke : SystemColors.ControlText;
                var button = control as Button;
                if (button != null) { button.UseVisualStyleBackColor = !dark; button.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard; }
            }
            ThemeControls(control);
        }
    }

    private void ApplyTheme()
    {
        BackColor = dark ? Color.FromArgb(24, 27, 32) : SystemColors.Control;
        ForeColor = dark ? Color.FromArgb(239, 241, 245) : SystemColors.ControlText;
        extras.BackColor = dark ? Color.FromArgb(43, 53, 69) : SystemColors.Window;
        extras.ForeColor = ForeColor;
        foreach (Button button in new[] { applyExtra, add })
        {
            button.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
            button.FlatAppearance.BorderColor = Color.FromArgb(98, 115, 139);
            button.Padding = dark ? new Padding(8, 3, 8, 3) : Padding.Empty;
            HighlightButton(button, false);
        }
        ThemeControls(this);
        HighlightActivePatch();
    }

    private const string RepackProfile = "R25j Fixes R3 Core";
    private const string RepackCore = "R25j Fixes R2";

    private void LoadExtraProfiles()
    {
        string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "R25j-repair", "ExtraPatchProfiles");
        extras.Items.Clear();
        extras.Items.Add("R25i");
        extras.Items.Add(RepackProfile);
        if (Directory.Exists(folder))
            foreach (string file in Directory.GetFiles(folder, "*.profile"))
            {
                string profile = Path.GetFileNameWithoutExtension(file);
                if (profile != "R25j Fixes R2" && profile != "Stock R25j" && !extras.Items.Contains(profile))
                    extras.Items.Add(profile);
            }
        if (extras.Items.Count > 0) extras.SelectedIndex = 0;
        applyExtra.Enabled = !busy && extras.SelectedIndex >= 0;
    }

    private async Task Run(string target, string importFolder = null)
    {
        busy = true;
        add.Enabled = extras.Enabled = applyExtra.Enabled = false;
        progress.Style = ProgressBarStyle.Marquee;
        status.Text = target == null ? "Checking installed patch..." : "Preparing " + target + "...";

        try
        {
            int result = await Task.Run(() => RunSwitcher(target, importFolder));
            if (result == 0)
            {

                if (importFolder != null) LoadExtraProfiles();
                else if (target == null) status.Text = "Choose a patch.";
                else SetActivePatch(target);
            }
        }
        catch (Exception error)
        {
            status.Text = error.Message;
        }
        finally
        {
            progress.Style = ProgressBarStyle.Blocks;
            busy = false;
            add.Enabled = extras.Enabled = true;
            applyExtra.Enabled = extras.SelectedIndex >= 0;
        }
    }

    private int RunSwitcher(string target, string importFolder)
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        string script = Path.Combine(folder, "R25j-repair", "Switch-Patch.ps1");
        if (!File.Exists(script)) throw new FileNotFoundException("Keep the switcher in the Kane's Wrath game folder.");
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + script + "\" " +
                        (importFolder != null ? "-ImportFolder \"" + importFolder + "\"" :
                         target == null ? "-Status" : "-TargetPatch \"" + target + "\""),
            WorkingDirectory = folder,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using (var process = new Process { StartInfo = start })
        {
            process.OutputDataReceived += (sender, args) => Report(args.Data);
            process.ErrorDataReceived += (sender, args) => Report(args.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode;
        }
    }

    private string activePatch;

    private void SetActivePatch(string patch)
    {
        activePatch = patch == RepackCore ? RepackProfile : patch;
        current.Text = "Selected patch: " + activePatch;
        HighlightActivePatch();
    }

    private void HighlightActivePatch()
    {
        current.ForeColor = activePatch == null ? ForeColor : dark ? Color.LightGreen : Color.DarkGreen;
        bool extraActive = activePatch != null && activePatch == extras.SelectedItem as string;
        applyExtra.Text = "Switch";
        HighlightButton(applyExtra, extraActive);
    }

    private void HighlightButton(Button button, bool active)
    {
        button.UseVisualStyleBackColor = !dark && !active;
        button.BackColor = active ? Color.FromArgb(29, 103, 63) :
            dark ? Color.FromArgb(43, 53, 69) : SystemColors.Control;
        button.ForeColor = active ? Color.White : ForeColor;
        if (dark)
            button.FlatAppearance.MouseOverBackColor = active ? Color.FromArgb(39, 123, 78) : Color.FromArgb(61, 76, 98);
    }

    private void Report(string line)
    {
        if (String.IsNullOrWhiteSpace(line) || line.StartsWith("Previous files saved in ", StringComparison.Ordinal)) return;
        BeginInvoke((Action)(() =>
        {
            if (line.StartsWith("Selected patch: ", StringComparison.Ordinal)) SetActivePatch(line.Substring("Selected patch: ".Length));
            else status.Text = line.Replace(RepackCore, RepackProfile);
        }));
    }
}





