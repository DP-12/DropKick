using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

internal static class SteamLaunch {
    static string Quote(string value) {
        var b = new StringBuilder("\""); int slashes = 0;
        foreach (char c in value) {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') { b.Append('\\', slashes * 2 + 1); b.Append(c); }
            else { b.Append('\\', slashes); b.Append(c); }
            slashes = 0;
        }
        b.Append('\\', slashes * 2); return b.Append('"').ToString();
    }
    [STAThread] static int Main(string[] args) {
        try {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string exe = Path.Combine(root, "ProjectZomboid64.exe");
            if (args.Length == 0 || !String.Equals(Path.GetFullPath(args[0]), exe, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Use Steam Normal Launch. Alternate Launch is not supported by this entry.");
            if (!File.Exists(Path.Combine(root,"DropKickLoader.jar"))) throw new Exception("DropKickLoader.jar missing. Reinstall or restore Steam launch options.");
            foreach (string key in new [] {"JAVA_TOOL_OPTIONS", "_JAVA_OPTIONS", "JDK_JAVA_OPTIONS"})
                if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))) throw new Exception("External Java options detected: " + key + ". Review them before using this launcher.");
            var command = new StringBuilder("-javaagent:DropKickLoader.jar --");
            for (int i=1;i<args.Length;i++) {
                if (args[i] == "--" || args[i].StartsWith("-javaagent:") || args[i].StartsWith("-agentpath:") || args[i].StartsWith("-agentlib:"))
                    throw new Exception("Conflicting JVM launch options. Restore/review Steam launch options.");
                command.Append(' ').Append(Quote(args[i]));
            }
            var info = new ProcessStartInfo(exe, command.ToString());
            info.WorkingDirectory = root; info.UseShellExecute = false;
            info.EnvironmentVariables["PATH"] = Path.Combine(root,"jre64\\bin") + ";" + Path.Combine(root,"jre64\\bin\\server") + ";" + Environment.GetEnvironmentVariable("PATH");
            using (var child = Process.Start(info)) { child.WaitForExit(); return child.ExitCode; }
        } catch (Exception error) {
            MessageBox.Show(error.Message, "DropKick launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
