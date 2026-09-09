using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// Only a front end. All installation ownership/compatibility checks remain in Setup-Steam.ps1.
internal sealed class SetupGui : Form {
    readonly ComboBox game = new ComboBox(), profile = new ComboBox();
    readonly TextBox log = new TextBox();
    readonly FlowLayoutPanel buttons = new FlowLayoutPanel();
    readonly bool zh = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh";
    bool busy;
    string T(string cn, string en) { return zh ? cn : en; }
    public SetupGui() {
        Text = "DropKick Setup"; ClientSize = new Size(820, 520);
        MinimumSize = new Size(700, 480); StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        var notice = new Label { AutoSize = true, Dock = DockStyle.Fill, Text = T("仅 Windows / 单人。请先完全退出 Steam 和游戏。\n本工具只安装加载器；仍需另行安装飞踢模组。\n卸载：恢复安装前的 Steam 启动选项，将本工具文件移入备份；\n保留存档、飞踢模组文件和 ReflectionEnabler。卸载后飞踢不可用，重装可恢复。", "Windows / single-player only. Exit Steam and the game completely.\nThis installs the loader only; install the DropKick mod separately.\nUninstall restores prior Steam launch options and moves owned files to backups.\nSaves, mod files and ReflectionEnabler remain. DropKick is disabled until reinstalled.") };
        layout.Controls.Add(notice, 0, 0); layout.SetColumnSpan(notice, 2);
        layout.Controls.Add(new Label { AutoSize = true, Text = T("游戏目录（浏览并选择 ProjectZomboid64.exe）", "Game directory (browse to ProjectZomboid64.exe)") }, 0, 1);
        game.Dock = DockStyle.Fill; layout.Controls.Add(game, 0, 2);
        var gameBrowse = new Button { Text = T("浏览…", "Browse..."), Dock = DockStyle.Fill };
        gameBrowse.Click += delegate { using (var d = new OpenFileDialog { Filter = "ProjectZomboid64.exe|ProjectZomboid64.exe", CheckFileExists = true }) { if (d.ShowDialog(this) == DialogResult.OK) game.Text = Path.GetDirectoryName(d.FileName); } };
        layout.Controls.Add(gameBrowse, 1, 2);
        var hint = new Label { AutoSize = true, Text = T("Steam 账号配置：Steam目录\\userdata\\账号数字目录\\config\\localconfig.vdf", "Steam profile: Steam folder\\userdata\\account ID\\config\\localconfig.vdf") };
        layout.Controls.Add(hint, 0, 3); layout.SetColumnSpan(hint, 2);
        profile.Dock = DockStyle.Fill; layout.Controls.Add(profile, 0, 4);
        var profileBrowse = new Button { Text = T("浏览…", "Browse..."), Dock = DockStyle.Fill };
        profileBrowse.Click += delegate { using (var d = new OpenFileDialog { Filter = "Steam config|localconfig.vdf", CheckFileExists = true }) { if (d.ShowDialog(this) == DialogResult.OK) profile.Text = d.FileName; } };
        layout.Controls.Add(profileBrowse, 1, 4);
        buttons.AutoSize = true; buttons.Dock = DockStyle.Fill;
        AddButton(T("安装 / 更新", "Install / update"), "Install", false);
        AddButton(T("卸载加载器", "Uninstall loader"), "Uninstall", false);
        layout.Controls.Add(buttons, 0, 5); layout.SetColumnSpan(buttons, 2);
        log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Both; log.WordWrap = false; log.Dock = DockStyle.Fill;
        layout.Controls.Add(log, 0, 6); layout.SetColumnSpan(log, 2);
        for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(layout);
        game.DropDownWidth = profile.DropDownWidth = 760;
        DetectPaths();
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; MessageBox.Show(this, T("操作进行中，请等待完成。", "Operation in progress. Please wait.")); } };
    }
    void DetectPaths() {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in new [] { @"HKEY_CURRENT_USER\Software\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam" }) {
            foreach (string value in new [] { "SteamPath", "InstallPath" }) {
                try { string path = Registry.GetValue(key, value, null) as string; if (!String.IsNullOrEmpty(path)) roots.Add(Path.GetFullPath(path)); } catch { }
            }
        }
        foreach (Environment.SpecialFolder folder in new [] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles }) {
            string path = Environment.GetFolderPath(folder); if (!String.IsNullOrEmpty(path)) roots.Add(Path.Combine(path, "Steam"));
        }
        var games = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots) {
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase); libraries.Add(root);
            try {
                string file = Path.Combine(root, @"steamapps\libraryfolders.vdf");
                if (File.Exists(file)) foreach (string path in ParseLibraries(File.ReadAllText(file))) libraries.Add(path);
            } catch { }
            foreach (string library in libraries) {
                string candidate = Path.Combine(library, @"steamapps\common\ProjectZomboid");
                if (File.Exists(Path.Combine(candidate, "ProjectZomboid64.exe")) && File.Exists(Path.Combine(candidate, "projectzomboid.jar"))) games.Add(Path.GetFullPath(candidate));
            }
            try {
                string users = Path.Combine(root, "userdata");
                if (Directory.Exists(users)) foreach (string user in Directory.GetDirectories(users)) {
                    if (!Regex.IsMatch(Path.GetFileName(user), @"^\d+$")) continue;
                    string candidate = Path.Combine(user, @"config\localconfig.vdf");
                    if (File.Exists(candidate)) profiles.Add(Path.GetFullPath(candidate));
                }
            } catch { }
        }
        FillCandidates(game, games); FillCandidates(profile, profiles);
        log.Text = T("已自动查找 Steam 常规位置和库目录。", "Searched standard Steam locations and library folders.") + "\r\n" +
            T("游戏候选：", "Game candidates: ") + games.Count + " / " + T("账号配置候选：", "Profile candidates: ") + profiles.Count + "\r\n" +
            T("唯一候选会自动填写；多个候选请从下拉框选择。未找到时仍可浏览或手填。\r\n安装前请确认账号路径正确；不会自动提权或关闭进程。", "A unique candidate is filled automatically; choose from the dropdown when several exist. Browse or type if not found.\r\nConfirm the profile before installation. No automatic elevation or process termination.");
    }
    internal static List<string> ParseLibraries(string text) {
        var result = new List<string>();
        foreach (Match m in Regex.Matches(text, "\"path\"\\s*\"((?:\\\\.|[^\"\\\\])*)\"", RegexOptions.IgnoreCase)) {
            string value = m.Groups[1].Value.Replace("\\\\", "\\").Replace("\\\"", "\"");
            if (Path.IsPathRooted(value)) result.Add(value);
        }
        return result;
    }
    static void FillCandidates(ComboBox box, HashSet<string> values) {
        var sorted = new List<string>(values); sorted.Sort(StringComparer.OrdinalIgnoreCase);
        foreach (string value in sorted) box.Items.Add(value);
        if (box.Items.Count == 1) box.SelectedIndex = 0;
        else box.SelectedIndex = -1;
    }
    void AddButton(string title, string mode, bool preview) {
        var b = new Button { Text = title, AutoSize = true }; b.Click += delegate { Run(mode, preview); }; buttons.Controls.Add(b);
    }
    void Run(string mode, bool preview) {
        if (busy) return;
        string root = AppDomain.CurrentDomain.BaseDirectory, gamePath, configPath;
        try {
            gamePath = Path.GetFullPath(game.Text.Trim()); configPath = Path.GetFullPath(profile.Text.Trim());
            if (!File.Exists(Path.Combine(gamePath, "ProjectZomboid64.exe")) || !File.Exists(Path.Combine(gamePath, "projectzomboid.jar"))) throw new Exception(T("游戏目录不正确。", "Invalid game directory."));
            if (!File.Exists(configPath) || !String.Equals(Path.GetFileName(configPath), "localconfig.vdf", StringComparison.OrdinalIgnoreCase)) throw new Exception(T("请选择 localconfig.vdf。", "Select localconfig.vdf."));
            foreach (string name in new [] { "Setup-Steam.ps1", "SteamOptions.ps1", "Manage-Bridge.ps1", "DropKickLoader.jar", "DropKickSteamLaunch.exe" })
                if (!File.Exists(Path.Combine(root, name))) throw new Exception("Missing package file: " + name);
        } catch (Exception e) { MessageBox.Show(this, e.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        string confirmation = mode == "Uninstall" ? T(
            "卸载加载器将：\n• 恢复本工具记录的安装前 Steam 启动选项。\n• 将加载器 JAR、启动包装 EXE 和安装记录移入游戏目录内的备份文件夹。\n• 保留存档、飞踢模组文件、其他模组和 ReflectionEnabler，不取消工坊订阅。\n\n卸载后游戏仍可从 Steam 普通启动，但飞踢会停用；使用兼容版本重新安装可恢复。\n若安装后手工修改过启动选项或本工具文件，会拒绝覆盖并提示错误。\n此操作不是从存档移除飞踢技能；如另行停用模组，仍建议先备份存档。\n\n请确认 Steam 与游戏已完全退出，并核对以下路径：",
            "Uninstall will:\n• Restore the pre-install Steam launch options recorded by this tool.\n• Move its loader JAR, wrapper EXE and receipts into backup folders in the game directory.\n• Keep saves, DropKick files, other mods and ReflectionEnabler. Workshop subscriptions are unchanged.\n\nNormal Steam launch remains available, but DropKick is disabled until a compatible loader is reinstalled.\nManually changed launch options or owned files cause refusal instead of overwrite.\nThis does not remove the skill from saves. Back up saves before separately disabling the mod.\n\nExit Steam/game completely and confirm these paths:") : T("将修改下列游戏的启动配置，并创建备份。\n请确认账号路径正确，且 Steam 与游戏已经完全退出。", "This changes the game's launch setup and creates backups.\nConfirm the profile path and exit Steam/game completely.");
        if (!preview && MessageBox.Show(this, confirmation + "\n\n" + gamePath + "\n" + configPath, Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
        // Paths are passed as child-only environment data, never interpolated into executable code.
        string code = "$ErrorActionPreference='Stop'; [Console]::OutputEncoding=New-Object Text.UTF8Encoding($false); try { & $env:DK_SETUP_SCRIPT -Mode $env:DK_SETUP_MODE -GameDirectory $env:DK_SETUP_GAME -SteamLocalConfig $env:DK_SETUP_CONFIG -WhatIf:($env:DK_SETUP_PREVIEW -eq '1'); exit 0 } catch { [Console]::Error.WriteLine($_.ToString()); exit 1 }";
        var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell\\v1.0\\powershell.exe"), "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(code)));
        info.WorkingDirectory = root; info.UseShellExecute = false; info.CreateNoWindow = true;
        info.RedirectStandardOutput = true; info.RedirectStandardError = true;
        info.StandardOutputEncoding = Encoding.UTF8; info.StandardErrorEncoding = Encoding.UTF8;
        info.EnvironmentVariables["DK_SETUP_SCRIPT"] = Path.Combine(root, "Setup-Steam.ps1");
        info.EnvironmentVariables["DK_SETUP_MODE"] = mode; info.EnvironmentVariables["DK_SETUP_GAME"] = gamePath;
        info.EnvironmentVariables["DK_SETUP_CONFIG"] = configPath; info.EnvironmentVariables["DK_SETUP_PREVIEW"] = preview ? "1" : "0";
        busy = true; buttons.Enabled = false; log.Clear();
        ThreadPool.QueueUserWorkItem(delegate {
            int result = -1; var output = new StringBuilder();
            try {
                using (var p = new Process { StartInfo = info }) {
                    p.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) lock(output) output.AppendLine(e.Data); };
                    p.Start(); p.BeginErrorReadLine(); string stdout = p.StandardOutput.ReadToEnd(); p.WaitForExit();
                    lock(output) output.Insert(0, stdout); result = p.ExitCode;
                }
            } catch (Exception e) { lock(output) output.AppendLine(e.Message); }
            Invoke((MethodInvoker)delegate {
                busy = false; buttons.Enabled = true; log.Text = output.ToString();
                log.AppendText("\r\n" + (result == 0 ? (preview ? T("预览完成，未安装。", "Preview complete; not installed.") : T("操作完成。请从 Steam 普通启动验证。", "Completed. Verify using normal Steam Play.")) : T("操作失败。可能已生成部分文件或备份，请保留日志并核对，不要盲目删除文件。", "Failed. Partial files/backups may exist. Keep this log and review before deleting anything.")));
            });
        });
    }
    [STAThread] static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new SetupGui()); }
}
