using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.IO;
using LibGit2Sharp;
using System.Globalization;

namespace GitVersionTree
{
    public partial class MainForm : Form
    {
        private Dictionary<string, string> DecorateDictionary = new Dictionary<string, string>();
        private List<List<string>> Nodes = new List<List<string>>();

        private string DotFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + Application.ProductName + ".dot";
        private string PdfFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + Application.ProductName + ".pdf";
        private string LogFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + Application.ProductName + ".log";
        private string RepositoryName;

        private Dictionary<string, List<string>> dChildParents = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> dParentChilds = new Dictionary<string, List<string>>();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName + " - v" + Application.ProductVersion.Substring(0, 3);

            RefreshPath();
        }

        private void GitPathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog BrowseOpenFileDialog = new OpenFileDialog();
            BrowseOpenFileDialog.Title = "Select git.exe";
            if (!String.IsNullOrEmpty(Reg.Read("GitPath")))
            {
                BrowseOpenFileDialog.InitialDirectory = Reg.Read("GitPath");
            }
            BrowseOpenFileDialog.FileName = "git.exe";
            BrowseOpenFileDialog.Filter = "Git Application (git.exe)|git.exe";
            if (BrowseOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("GitPath", BrowseOpenFileDialog.FileName);
                RefreshPath();
            }
        }

        private void GraphvizDotPathBrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog BrowseOpenFileDialog = new OpenFileDialog();
            BrowseOpenFileDialog.Title = "Select dot.exe";
            if (!String.IsNullOrEmpty(Reg.Read("GraphvizPath")))
            {
                BrowseOpenFileDialog.InitialDirectory = Reg.Read("GraphvizPath");
            }
            BrowseOpenFileDialog.FileName = "dot.exe";
            BrowseOpenFileDialog.Filter = "Graphviz Dot Application (dot.exe)|dot.exe";
            if (BrowseOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("GraphvizPath", BrowseOpenFileDialog.FileName);
                RefreshPath();
            }
        }

        private void GitRepositoryPathBrowseButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog BrowseFolderBrowserDialog = new FolderBrowserDialog();
            BrowseFolderBrowserDialog.Description = "Select Git repository";
            BrowseFolderBrowserDialog.ShowNewFolderButton = false;
            if (!String.IsNullOrEmpty(Reg.Read("GitRepositoryPath")))
            {
                BrowseFolderBrowserDialog.SelectedPath = Reg.Read("GitRepositoryPath");
            }
            if (BrowseFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("GitRepositoryPath", BrowseFolderBrowserDialog.SelectedPath);
                RefreshPath();
            }
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Reg.Read("GitPath")) ||
                String.IsNullOrEmpty(Reg.Read("GraphvizPath")) ||
                String.IsNullOrEmpty(Reg.Read("BrowserExec")) ||
                String.IsNullOrEmpty(Reg.Read("GitRepositoryPath")))
            {
                MessageBox.Show("Please select a Git, Graphviz & Git repository.", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                StatusRichTextBox.Text = "";
                RepositoryName = new DirectoryInfo(GitRepositoryPathTextBox.Text).Name;
                DotFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + RepositoryName + ".dot";
                PdfFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + RepositoryName + ".pdf";
                LogFilename = Directory.GetParent(Application.ExecutablePath) + @"\" + RepositoryName + ".log";
                File.WriteAllText(LogFilename, "");
                Generate();
            }
        }

        private void HomepageLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/teungri/GitTkCvsVersionTree");
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RefreshPath()
        {
            if (!String.IsNullOrEmpty(Reg.Read("GitPath")))
            {
                GitPathTextBox.Text = Reg.Read("GitPath");
            }
            if (!String.IsNullOrEmpty(Reg.Read("GraphvizPath")))
            {
                GraphvizDotPathTextBox.Text = Reg.Read("GraphvizPath");
            }
            if (!String.IsNullOrEmpty(Reg.Read("GitRepositoryPath")))
            {
                GitRepositoryPathTextBox.Text = Reg.Read("GitRepositoryPath");
            }
            if (!String.IsNullOrEmpty(Reg.Read("BrowserExec")))
            {
                TxtBxBrowserPath.Text = Reg.Read("BrowserExec");
            }
        }

        private void Status(string Message)
        {
            StatusRichTextBox.AppendText(DateTime.Now + " - " + Message + "\r\n");
            StatusRichTextBox.SelectionStart = StatusRichTextBox.Text.Length;
            StatusRichTextBox.ScrollToCaret();
            Refresh();
        }

        private string Execute(string Command, string Argument)
        {
            string ExecuteResult = String.Empty;
            Process ExecuteProcess = new Process();
            ExecuteProcess.StartInfo.UseShellExecute = false;
            ExecuteProcess.StartInfo.CreateNoWindow = true;
            ExecuteProcess.StartInfo.RedirectStandardOutput = true;
            ExecuteProcess.StartInfo.FileName = Command;
            ExecuteProcess.StartInfo.Arguments = Argument;
            ExecuteProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ExecuteProcess.Start();
            ExecuteResult = ExecuteProcess.StandardOutput.ReadToEnd();
            ExecuteProcess.WaitForExit();
            if (ExecuteProcess.ExitCode == 0)
            {
                return ExecuteResult;
            }
            else
            {
                return String.Empty;
            }
        }

        private void BtBrowserPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog BrowseOpenFileDialog = new OpenFileDialog();
            BrowseOpenFileDialog.Title = "Select Browser";
            if (!String.IsNullOrEmpty(Reg.Read("BrowserPath")))
            {
                BrowseOpenFileDialog.InitialDirectory = Reg.Read("BrowserPath");
            }
            if (!String.IsNullOrEmpty(Reg.Read("BrowserExec")))
            {
                BrowseOpenFileDialog.FileName = Reg.Read("BrowserExec");
            }
            BrowseOpenFileDialog.Filter = "Bowser application |*.exe";
            if (BrowseOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                Reg.Write("BrowserPath", BrowseOpenFileDialog.InitialDirectory);
                Reg.Write("BrowserExec", BrowseOpenFileDialog.FileName);
                RefreshPath();
            }
        }

        private void ParseParentChilds(string parent, string childs)
        {
            List<string> lstChilds = childs.Split(' ').ToList();

            if (!dParentChilds.ContainsKey(parent))
            {
                dParentChilds.Add(parent, lstChilds);
            } 
            else dParentChilds[parent].AddRange(lstChilds);

            foreach (string child in lstChilds)
            {
                if (!dChildParents.ContainsKey(child))
                {
                    dChildParents.Add(child, new List<string>() {parent} );
                }
                else dChildParents[child].Add(parent);
            }
        }
        private void Generate()
        {
            string Result;
            string[] MergedColumns;
            string[] MergedParents;
            string gitRepoPath = Reg.Read("GitRepositoryPath") + "\\.git"; 
            string gitExec = Reg.Read("GitPath"); 
            string gitDir = $"--git-dir \"{gitRepoPath}\"";

            Repository repository = new Repository(gitRepoPath);

            var commitFilter = new CommitFilter { SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time };
            var commits = repository.Commits.QueryBy(commitFilter).ToList();

            var branches = repository.Branches;
            foreach (var branch in branches.Where(b => b.Commits.Any()))
            {
                var bCommits = branch.Commits.ToList();
            }

            string dateformat = "%Y-%m-%d %H:%M:%S";
            
            Status("Getting git commit(s) ...");
            // %h: abbr commit hash, %p: abbr parent hashes, %d: ref names (tag, branch names)
            string gitcmd = $"{gitDir} log --all --pretty=format:\"%h|%p|%d|%cd|%s\" --date=format:\"{dateformat}\"";
            Result = Execute(gitExec, gitcmd);

            Dictionary<string, DateTime> dShaTimestamp = new Dictionary<string, DateTime>();
            Dictionary<DateTime, string> dTimestampSha = new Dictionary<DateTime, string>();
            Dictionary<string, string> dShaMessage = new Dictionary<string, string>();

            dChildParents = new Dictionary<string, List<string>>();
            dParentChilds = new Dictionary<string, List<string>>();

            if (String.IsNullOrEmpty(Result))
            {
                Status("Unable to get get branch or branch empty ...");
            }
            else
            {
                string format = "yyyy-MM-dd HH:mm:ss";
                File.AppendAllText(LogFilename, "[commit(s)]\r\n");
                File.AppendAllText(LogFilename, Result + "\r\n");
                string[] DecorateLines = Result.Split('\n');
                foreach (string DecorateLine in DecorateLines)
                {
                    MergedColumns = DecorateLine.Split('|');
                    ParseParentChilds(MergedColumns[0], MergedColumns[1]);
                    DateTime dt = DateTime.ParseExact(MergedColumns.ElementAt(3), format, CultureInfo.InvariantCulture);
                    string sDt = dt.ToString("yyyy/MM/dd hh:mm");
                    string sha = MergedColumns[0];
                    string msg = MergedColumns[4];
                    dShaTimestamp.Add(sha, dt);
                    dTimestampSha.Add(dt, sha);
                    dShaMessage.Add(sha, msg);

                    if (!String.IsNullOrEmpty(MergedColumns[2]))
                    {
                        DecorateDictionary.Add(MergedColumns[0], MergedColumns[2]);
                    }
                }
                Status("Processed " + DecorateDictionary.Count + " decorate(s) ...");
            }

            Status("Getting git all branches ...");
            // string gitcmd = $"{gitDir} log --all --pretty=format:\"%h|%p|%d|%cd|%s\" --date=format:\"{dateformat}\"";
            gitcmd = $"{gitDir} show-branch --all";
            Result = Execute(gitExec, gitcmd);
            string[] lines = Result.Split('\n');
            
            gitcmd = $"{gitDir} show-branch --all --sha1-name";
            Result = Execute(gitExec, gitcmd);
            lines = Result.Split('\n');

            Status("Getting git ref branch(es) ...");
            gitcmd = $"{gitDir} for-each-ref --format=\"%(objectname:short)|%(refname:short)\" ";
            Result = Execute(gitExec, gitcmd);
            
            if (String.IsNullOrEmpty(Result))
            {
                Status("Unable to get get branch or branch empty ...");
            }
            else
            {
                File.AppendAllText(LogFilename, "[ref branch(es)]\r\n");
                File.AppendAllText(LogFilename, Result + "\r\n");
                string[] RefLines = Result.Split('\n');
                foreach (string RefLine in RefLines)
                {
                    if (String.IsNullOrEmpty(RefLine)) continue;

                    string[] RefColumns = RefLine.Split('|');
                    if (!RefColumns[1].ToLower().StartsWith("refs/tags") &&
                         (RefColumns[1].ToLower().Equals("master") || RefColumns[1].ToLower().Contains("/master")))
                    {
                        gitcmd = $"{gitDir} log --reverse --first-parent --pretty=format:\"%h\" {RefColumns[0]}";
                        Result = Execute(gitExec, gitcmd);
                        if (String.IsNullOrEmpty(Result))
                        {
                            Status("Unable to get commit(s) ...");
                            continue;
                        }

                        string[] HashLines = Result.Split('\n');
                        Nodes.Add(new List<string>());
                        foreach (string HashLine in HashLines)
                        {
                            Nodes[Nodes.Count - 1].Add(HashLine);
                        }
                    }
                }
                foreach (string RefLine in RefLines)
                {
                    if (String.IsNullOrEmpty(RefLine)) continue;

                    string[] RefColumns = RefLine.Split('|');
                    if (!RefColumns[1].ToLower().StartsWith("refs/tags") &&
                        !RefColumns[1].ToLower().Contains("master"))
                    {
                        Result = Execute(gitExec, gitcmd);
                        if (String.IsNullOrEmpty(Result))
                        {
                            Status("Unable to get commit(s) ...");
                            continue;
                        }

                        string[] HashLines = Result.Split('\n');
                        Nodes.Add(new List<string>());
                        foreach (string HashLine in HashLines)
                        {
                            Nodes[Nodes.Count - 1].Add(HashLine);
                        }
                    }
                }
            }

            Status("Getting git merged branch(es) ...");
            gitcmd = $"{gitDir} log --all --merges --pretty=format:\"%h|%p\"";
            Result = Execute(gitExec, gitcmd);
            if (String.IsNullOrEmpty(Result))
            {
                Status("Unable to get get branch or branch empty ...");
            }
            else
            {
                File.AppendAllText(LogFilename, "[merged branch(es)]\r\n");
                File.AppendAllText(LogFilename, Result + "\r\n");
                string[] MergedLines = Result.Split('\n');
                foreach (string MergedLine in MergedLines)
                {
                    MergedColumns = MergedLine.Split('|');
                    MergedParents = MergedColumns[1].Split(' ');
                    if (MergedParents.Length <= 1) continue;

                    for (int i = 1; i < MergedParents.Length; i++)
                    {
                        gitcmd = $"{gitDir} log --reverse --first-parent --pretty=format:\"%h\" {MergedParents[i]}";
                        Result = Execute(gitExec, gitcmd);
                        if (String.IsNullOrEmpty(Result))
                        {
                            Status("Unable to get commit(s) ...");
                            continue;
                        }

                        string[] HashLines = Result.Split('\n');
                        Nodes.Add(new List<string>());
                        foreach (string HashLine in HashLines)
                        {
                            Nodes[Nodes.Count - 1].Add(HashLine);
                        }
                        Nodes[Nodes.Count - 1].Add(MergedColumns[0]);
                    }
                }
            }

            Status("Processed " + Nodes.Count + " branch(es) ...");

            StringBuilder DotStringBuilder = new StringBuilder();
            Status("Generating dot file ...");
            DotStringBuilder.Append("strict digraph \"" + RepositoryName + "\" {\r\n");
            // DotStringBuilder.Append("  splines=line rankdir=\"BT\";\r\n");
            DotStringBuilder.Append(" rankdir=\"BT\";\r\n");

            // HashSet<string> labels = new HashSet<string>();
            // for (int i = 0; i < Nodes.Count; i++)
            // {   // make labels
            //      for (int j = 0; j < Nodes[i].Count; j++)
            //     {
            //         string sha = Nodes[i][j];
            //         string ts = "xx";
            //         string branch = "Branch bla";
            //         if (sha.Split('|').Count() < 2)
            //         {
            //             if (dShaTimestamp.ContainsKey(sha))
            //             {
            //                 ts = dShaTimestamp[sha].ToString();
            //             }
            //         }
            //         else
            //         {
            //             sha = sha.Split('|').ElementAt(0);
            //             if (dShaTimestamp.ContainsKey(sha))
            //             {
            //                 ts = dShaTimestamp[sha].ToString();
            //             }
            //         }
            //         if (labels.Contains(sha)) continue;
            //         labels.Add(sha);
            //         DotStringBuilder.Append($"{sha} [label = \"{sha}\\n{ts}\\n{branch}\"];\r\n");
            //     }
            // }

            for (int i = 0; i < Nodes.Count; i++)
            {
                DotStringBuilder.Append("  node[group=\"" + (i + 1) + "\"];\r\n");
                DotStringBuilder.Append("  ");
                for (int j = 0; j < Nodes[i].Count; j++)
                {
                    string sha = Nodes[i][j];
                    sha += $" | {dShaTimestamp[sha]}";
                    // if (sha.Split('|').Count() >= 2)
                    // {
                    //     sha = sha.Split('|').ElementAt(0);
                    // }
                    DotStringBuilder.Append("\"" + sha + "\"");
                    if (j < Nodes[i].Count - 1)
                    {
                        DotStringBuilder.Append(" -> ");
                    }
                    else
                    {
                        DotStringBuilder.Append(";");
                    }
                }
                DotStringBuilder.Append("\r\n");
            }

            int DecorateCount = 0;
            foreach (KeyValuePair<string, string> DecorateKeyValuePair in DecorateDictionary)
            {
                DecorateCount++;
                DotStringBuilder.Append("  subgraph Decorate" + DecorateCount + "\r\n");
                DotStringBuilder.Append("  {\r\n");
                DotStringBuilder.Append("    rank=\"same\";\r\n");
                string sha = DecorateKeyValuePair.Key;
                sha += $" | {dShaTimestamp[sha]}";
                // if (sha.Split('|').Count() >= 2)
                // {
                //     sha += sha.Split('|').ElementAt(0);
                // }
                if (DecorateKeyValuePair.Value.Trim().Substring(0, 5) == "(tag:")
                {
                    DotStringBuilder.Append($"    \"{DecorateKeyValuePair.Value.Trim()}\" [shape=\"box\", style=\"filled\", fillcolor=\"#ffffdd\"];\r\n");
                }
                else
                {
                    DotStringBuilder.Append($"    \"{DecorateKeyValuePair.Value.Trim()}\" [shape=\"box\", style=\"filled\", fillcolor=\"#ddddff\"];\r\n");
                }
                DotStringBuilder.Append($"    \"{DecorateKeyValuePair.Value.Trim()}\" -> \"{sha}\" [weight=0, arrowtype=\"none\", dirtype=\"none\", arrowhead=\"none\", style=\"dotted\"];\r\n");
                DotStringBuilder.Append("  }\r\n");
            }

            DotStringBuilder.Append("}\r\n");
            File.WriteAllText(@DotFilename, DotStringBuilder.ToString());

            Status("Generating version tree ...");
            Process DotProcess = new Process();
            DotProcess.StartInfo.UseShellExecute = false;
            DotProcess.StartInfo.CreateNoWindow = true;
            DotProcess.StartInfo.RedirectStandardOutput = true;
            DotProcess.StartInfo.FileName = GraphvizDotPathTextBox.Text;
            DotProcess.StartInfo.Arguments = $"\"{DotFilename}\" -Tpdf -Gsize=10,10 -o\"{PdfFilename}\"";
            DotProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            DotProcess.Start();
            DotProcess.WaitForExit();

            string PsFilename = PdfFilename.Replace(".pdf", ".ps");
            DotProcess.StartInfo.Arguments = $"\"{DotFilename}\" -Tps -o\"{PsFilename}\"";
            DotProcess.Start();
            DotProcess.WaitForExit();

            string SvgFilename = PdfFilename.Replace(".pdf", ".svg");
            DotProcess.StartInfo.Arguments = $"\"{DotFilename}\" -Tsvg -o\"{SvgFilename}\"";
            DotProcess.Start();
            DotProcess.WaitForExit();

            if (DotProcess.ExitCode == 0)
            {
                if (File.Exists(@SvgFilename))
                {
// #if (!DEBUG)
                    Process ViewSvgProcess = new Process();
                    ViewSvgProcess.StartInfo.FileName = TxtBxBrowserPath.Text;
                    ViewSvgProcess.StartInfo.Arguments = $"{SvgFilename}";
                    ViewSvgProcess.Start();
                    //ViewPdfProcess.WaitForExit();
                    //Close();
// #endif
                }
            }
            else
            {
                Status("Version tree generation failed ...");
            }
            DecorateDictionary.Clear();
            Status("Done! ...");
        }


        
    } // class MainForm
}
