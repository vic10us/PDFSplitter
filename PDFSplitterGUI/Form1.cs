using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PDFSplitter.Core;
using PDFSplitterGUI.Properties;

namespace PDFSplitterGUI
{
    public partial class Form1 : Form
    {
        private readonly SynchronizationContext synchronizationContext;

        public Form1()
        {
            InitializeComponent();
            synchronizationContext = SynchronizationContext.Current;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void fileTextBox_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(fileTextBox.Text)) okButton.Enabled = false;
            var extension = Path.GetExtension(fileTextBox.Text) ?? "";
            okButton.Enabled = File.Exists(fileTextBox.Text) && extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase);
        }

        private async void okButton_Click(object sender, EventArgs e)
        {
            // Process the PDF file.
            var util = new Utils(s =>
            {
                synchronizationContext.Post(o =>
                {
                    var m = (ProcessMessage) o;
                    logs.AppendText($"{m.Message}\r\n");
                    progressBar1.Value = m.Progress;
                }, s);
                // logs.Text += ;
            });
            var input = fileTextBox.Text;
            var output = GetOutputFolder(input);
            if (Directory.Exists(output))
            {
                var dialogResult =
                    MessageBox.Show(
                        string.Format(Resources.ConfirmOverwrite, output),
                        Resources.ConfirmOverwriteTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult != DialogResult.Yes) return;
                Directory.Delete(output, true);
            }
            Directory.CreateDirectory(output);
            logs.Text = Resources.Starting;
            okButton.Enabled = false;
            var processResult = await util.ProcessAndSplitAsync(input, output);
            okButton.Enabled = true;
            if (processResult) MessageBox.Show(Resources.Completed_Successfully, Resources.Success, MessageBoxButtons.OK);
            else MessageBox.Show(Resources.FailedToComplete, Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //await util.ProcessAndSplitAsync(input, output);
        }

        public static string GetOutputFolder(string inputFileName)
        {
            var inputFile = new FileInfo(inputFileName);
            var filenameWithPath = inputFile.FullName;
            var filePath = Path.GetDirectoryName(filenameWithPath);
            var outputPath = Path.Combine(filePath ?? "", Path.GetFileNameWithoutExtension(filenameWithPath));
            return outputPath;
        }

        private void verboseLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // fileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            fileTextBox.Text = openFileDialog.FileName;
        }
    }
}
