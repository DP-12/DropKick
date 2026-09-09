using System;
using System.Windows.Forms;
internal static class GuiSmokeTests {
    static int Count(Control c, Type type) { int n = type.IsInstanceOfType(c) ? 1 : 0; foreach (Control child in c.Controls) n += Count(child,type); return n; }
    [STAThread] static int Main() {
        using (var form = new SetupGui()) {
            form.PerformLayout();
            if (Count(form,typeof(TextBox)) != 1 || Count(form,typeof(ComboBox)) != 2 || Count(form,typeof(Button)) != 4) throw new Exception("Unexpected controls");
            var paths = SetupGui.ParseLibraries("\"libraryfolders\" { \"0\" { \"path\" \"G:\\\\SteamLibrary\" } }");
            if (paths.Count != 1 || paths[0] != @"G:\SteamLibrary") throw new Exception("Library parser failed");
            Console.WriteLine("PASS: GUI constructed, preview button absent, library path parsing passed.");
            foreach (Control panel in form.Controls) foreach(Control c in panel.Controls) if(c is ComboBox) Console.WriteLine("Candidates=" + ((ComboBox)c).Items.Count + "; selected=" + c.Text);
        }
        return 0;
    }
}
