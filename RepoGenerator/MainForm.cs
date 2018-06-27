using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RepoGenerator
{
    public partial class MainForm : Form
    {
        private readonly string destinationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Repo.cs"
        );

        private readonly string debuggerBreak = "\t\tDebugger.Break();" + Environment.NewLine + "\t\t";
        private readonly string debugWriteLine = "Debug.WriteLine(ex);" + Environment.NewLine;

        private readonly string method = @"
        public static {0} {1}{2}({3} {4})
        {{
            try 
            {{
                using (var model = new {5}())
                {{
                    {6}
                }}
            }}
            catch (Exception ex) 
            {{ 
                {7}{8}{9}
            }}
        }}

        ";

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (folderBrowser.ShowDialog() != DialogResult.OK)
                return;

            txtLocation.Text = folderBrowser.SelectedPath;
            var files = Directory.GetFiles(txtLocation.Text, "*.cs", SearchOption.AllDirectories).ToList();

            txtClassList.Clear();
            foreach (var file in files)
            {
                if (file.IndexOf("Model", StringComparison.OrdinalIgnoreCase) >= 0)
                    txtModelName.Text = Path.GetFileNameWithoutExtension(file);

                txtClassList.AppendText(Path.GetFileName(file) + Environment.NewLine);
            }
        }

        private string GetModelName()
            => string.IsNullOrEmpty(txtModelName.Text) ? "DataModel" : txtModelName.Text;

        private string GetDebuggerBreak() => cbDebuggerBreak.Checked
            ? debuggerBreak
            : "";

        private string GetDebugWriteLine() => cbDebugException.Checked
            ? debugWriteLine
            : "";

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (!txtClassList.Text.Contains(".cs"))
                return;

            var selectedFiles = txtClassList.Text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string create = "", retreive = "", retreiveInBulk = "", update = "", delete = "";

            foreach (var file in selectedFiles)
            {
                var className = Path.GetFileNameWithoutExtension(file);
                var paramName = char.ToLower(className[0]) + className.Substring(1);

                create += string.Format(method,
                    "bool",
                    "Add",
                    className,
                    className,
                    paramName,
                    GetModelName(),
                    $@"
                model.{className}s.Add({paramName});

                return model.SaveChanges() > 0;
                    ",
                    GetDebugWriteLine(),
                    GetDebuggerBreak(),
                    "return false;"
                );

                retreive += string.Format(method,
                    className,
                    "Get",
                    className,
                    "int",
                    "id",
                    GetModelName(),
                    $"return model.{className}s.Find(id);",
                    GetDebugWriteLine(),
                    GetDebuggerBreak(),
                    "return null;"
                );

                retreiveInBulk += string.Format(method,
                    $"List<{className}>",
                    "Get",
                    $"{className}s",
                    "",
                    "",
                    GetModelName(),
                    $"return model.{className}s.ToList();",
                    GetDebugWriteLine(),
                    GetDebuggerBreak(),
                    "return null;"
                );

                update += string.Format(method,
                    "bool",
                    "Update",
                    className,
                    className,
                    paramName,
                    GetModelName(),
                    $@"
                model.{className}s.Update({paramName});

                return model.SaveChanges() > 0;
                    ",
                    GetDebugWriteLine(),
                    GetDebuggerBreak(),
                    "return false;"
                );

                delete += string.Format(method,
                    "bool",
                    "Remove",
                    className,
                    "int",
                    "id",
                    GetModelName(),
                    $@"
                model.{className}s.Remove(model.{className}s.Find(id));
                        
                return model.SaveChanges() > 0;
                    ",
                    GetDebugWriteLine(),
                    GetDebuggerBreak(),
                    "return false;"
                );

            }

            File.WriteAllText(destinationPath, string.Join(Environment.NewLine, create, retreiveInBulk, retreive, update, delete));
            System.Diagnostics.Process.Start("notepad.exe", destinationPath);
        }
    }
}
