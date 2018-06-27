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

        private readonly string method = @"
        public static {0} {1}{2}({3} {4})
        {{
            using (var model = new DataModel())
            {{
                {5}
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
                txtClassList.AppendText(Path.GetFileName(file) + Environment.NewLine);
        }

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
                    "void",
                    "Add",
                    className,
                    className,
                    paramName,
                    $@"
                model.{className}s.Add({paramName});

                model.SaveChanges();
                    "
                );

                retreive += string.Format(method,
                    className,
                    "Get",
                    className,
                    "int",
                    "id",
                    $"return model.{className}s.Find(id);"
                );

                retreiveInBulk += string.Format(method,
                    $"List<{className}>",
                    "Get",
                    $"{className}s",
                    "",
                    "",
                    $"return model.{className}s.ToList();"
                );

                update += string.Format(method,
                    "bool",
                    "Update",
                    className,
                    className,
                    paramName,
                    $@"
                model.{className}s.Update({paramName});

                return model.SaveChanges() > 0;
                    "
                );

                delete += string.Format(method,
                    "bool",
                    "Remove",
                    className,
                    "int",
                    "id",
                    $@"
                model.{className}s.Remove(model.{className}s.Find(id));
                        
                return model.SaveChanges() > 0;
                    "
                );

                File.WriteAllText(destinationPath, string.Join(Environment.NewLine, create, retreiveInBulk, retreive, update, delete));
            }
        }
    }
}
