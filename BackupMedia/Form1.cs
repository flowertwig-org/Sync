using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackupMedia
{
    public partial class Form1 : Form
    {
        string directory1 = null;
        string directory2 = null;

        Exception exception = null;

        SortedList<string, string> infoList1 = new SortedList<string, string>();
        SortedList<string, string> infoList2 = new SortedList<string, string>();
        SortedList<string, string> result = new SortedList<string, string>();

        List<string> fileToCopy = new List<string>();

        int index = 0;

        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;

            backgroundWorker2.DoWork += BackgroundWorker2_DoWork;
            backgroundWorker2.ProgressChanged += BackgroundWorker2_ProgressChanged;
            backgroundWorker2.RunWorkerCompleted += BackgroundWorker2_RunWorkerCompleted;
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == -1)
            {
                var index = (int)e.UserState;
                progressBar1.Maximum = index;
            }
            else
            {
                progressBar1.Value = e.ProgressPercentage;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                directory1 = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                directory2 = folderBrowserDialog1.SelectedPath;

                if (!string.IsNullOrEmpty(directory1) && !string.IsNullOrEmpty(directory2))
                {
                    button3.Enabled = true;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            backgroundWorker1.RunWorkerAsync();
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            if (result.Count > 0)
            {
                if (result.Count == 1)
                {
                    MessageBox.Show(string.Format("There is {0} file missing in target folder.", result.Count));
                }
                else
                {
                    MessageBox.Show(string.Format("There are {0} files missing in target folder.", result.Count));
                }

                foreach (var item in result)
                {
                    checkedListBox1.Items.Add(item.Value, true);
                }

                button4.Enabled = true;
            }
            else if (exception != null)
            {
                MessageBox.Show("Error:\r\n" + exception.Message);
            }
            else
            {
                MessageBox.Show("Source folder has no new files.");
            }
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var di1 = new System.IO.DirectoryInfo(directory1);
            var di2 = new System.IO.DirectoryInfo(directory2);
            if (di1.Exists && di2.Exists)
            {
                try
                {
                    // TODO: do stuff
                    var files1 = di1.GetFiles();
                    var files2 = di2.GetFiles();

                    int nOfFiles = files1.Length + files2.Length + 1;
                    backgroundWorker1.ReportProgress(-1, nOfFiles);

                    HashFiles(files1, infoList1);
                    HashFiles(files2, infoList2);


                    // Check if we have files in folder 1 that doesn't exist in folder 2.
                    foreach (var item in infoList1)
                    {
                        if (!infoList2.ContainsKey(item.Key))
                        {
                            result.Add(item.Key, item.Value);
                        }
                        //else
                        //{
                        //    infoList2.Remove(item.Key);
                        //}
                    }
                    backgroundWorker1.ReportProgress(++index);

                    //// Check if we have files in folder 2 that doesn't exist in folder 1.
                    //foreach (var item in infoList2)
                    //{
                    //    if (!infoList1.ContainsKey(item.Key))
                    //    {
                    //        result.Add(item.Key, item.Value);
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
        }

        private void HashFiles(System.IO.FileInfo[] files, SortedList<string, string> infoList)
        {
            string directory = null;
            var firstFile = files.FirstOrDefault();
            if (firstFile != null)
            {
                directory = firstFile.Directory.FullName;
            }

            SortedList<string, string> previouslyHashedFiles = GetHashedFiles(directory);


            foreach (var file in files)
            {
                try
                {
                    if (file.Name != "hashes.txt")
                    {
                        string hashString = null;
                        if (previouslyHashedFiles.ContainsKey(file.Name))
                        {
                            hashString = previouslyHashedFiles[file.Name];
                        }
                        var fileHashedAlready = !string.IsNullOrEmpty(hashString);
                        if (!fileHashedAlready)
                        {
                            var sha512 = new System.Security.Cryptography.SHA512Managed();
                            var hash = sha512.ComputeHash(file.OpenRead());
                            hashString = Convert.ToBase64String(hash);
                        }

                        infoList.Add(hashString, file.FullName);

                        UpdateHashFile(previouslyHashedFiles, infoList, file);
                    }

                    backgroundWorker1.ReportProgress(++index);
                }
                catch (Exception ex)
                {
                    // TODO: Log error
                    exception = ex;
                }
            }
        }

        private SortedList<string, string> GetHashedFiles(string directory)
        {
            SortedList<string, string> list = new SortedList<string, string>();

            if (directory == null)
            {
                return list;
            }

            var filename = directory + Path.DirectorySeparatorChar + "hashes.txt";
            if (!File.Exists(filename))
            {
                return list;
            }

            var lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                var pair = line.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length != 2)
                {
                    continue;
                }

                var fileHash = pair[0];
                var fileName = pair[1];

                list.Add(fileName, fileHash);
            }

            return list;
        }

        private void UpdateHashFile(SortedList<string, string> previouslyHashedFiles, SortedList<string, string> infoList, FileInfo file)
        {
            var sb = new StringBuilder();
            var tmpList = new SortedList<string, string>(infoList);

            var directory = file.Directory.FullName + Path.DirectorySeparatorChar;

            foreach (var item in previouslyHashedFiles)
            {
                var fileName = item.Key;
                var fileHash = item.Value;
                if (!tmpList.ContainsKey(fileHash))
                {
                    tmpList.Add(fileHash, directory + fileName);
                }
            }

            foreach (var item in tmpList)
            {
                var fileInfo = new FileInfo(item.Value);
                // <hash>#<filename>
                sb.AppendLine(item.Key + "#" + fileInfo.Name);
            }

            var hashFileName = directory + "hashes.txt";
            try
            {
                File.WriteAllText(hashFileName, sb.ToString());
            }
            catch (Exception)
            {
                // If we don't have permission or simular to file we will just ignore it...
            }
        }
        private void UpdateHashFile(string fileHash, string fileName, string directory, SortedList<string, string> previouslyHashedFiles)
        {
            var sb = new StringBuilder();
            var tmpList = new SortedList<string, string>();

            foreach (var item in previouslyHashedFiles)
            {
                var name = item.Key;
                var hash = item.Value;
                if (!tmpList.ContainsKey(hash))
                {
                    tmpList.Add(hash, directory + name);
                }
            }

            tmpList.Add(fileHash, fileName);

            foreach (var item in tmpList)
            {
                var fileInfo = new FileInfo(item.Value);
                // <hash>#<filename>
                sb.AppendLine(item.Key + "#" + fileInfo.Name);
            }

            var hashFileName = directory + "hashes.txt";
            try
            {
                File.WriteAllText(hashFileName, sb.ToString());
            }
            catch (Exception ex)
            {
                exception = ex;
                // If we don't have permission or simular to file we will just ignore it...
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button4.Enabled = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            button4.Enabled = false;
            progressBar1.Value = 0;

            fileToCopy.Clear();

            fileToCopy = checkedListBox1.CheckedItems.Cast<string>().ToList();

            exception = null;

            backgroundWorker2.RunWorkerAsync();
        }

        private void BackgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            checkedListBox1.Items.Clear();

            if (exception != null)
            {
                MessageBox.Show("Error:\r\n" + exception.Message);
            }
            else
            {
                foreach (var item in result)
                {
                    checkedListBox1.Items.Add(item.Value, false);
                }
                MessageBox.Show("Done!");
            }
        }

        private void BackgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == -1)
            {
                var index = (int)e.UserState;
                progressBar1.Maximum = index;
            }
            else
            {
                progressBar1.Value = e.ProgressPercentage;
            }
        }

        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {

                var nOfFiles = fileToCopy.Count;

                backgroundWorker2.ReportProgress(-1, nOfFiles);


                var index = 0;

                result.Clear();

                foreach (string item in fileToCopy)
                {
                    index++;

                    if (string.IsNullOrEmpty(item))
                        continue;

                    var fi = new System.IO.FileInfo(item);

                    if (fi.Exists)
                    {

                        //var source = directory1 + System.IO.Path.DirectorySeparatorChar + fi.Name;
                        //result.Add(source, source);

                        var target = directory2 + System.IO.Path.DirectorySeparatorChar + fi.Name;
                        result.Add(target, target);

                        fi.CopyTo(target);

                        var previouslyHashedFiles = GetHashedFiles(directory2);

                        var matchPair = infoList1.FirstOrDefault(x => x.Value == fi.FullName);
                        if (!string.IsNullOrEmpty(matchPair.Key))
                        {
                            UpdateHashFile(matchPair.Key, fi.FullName, directory2 + Path.DirectorySeparatorChar, previouslyHashedFiles);
                        }
                        else
                        {
                            var error = new Exception(string.Format("Match pair error: {0}, {1}", matchPair.ToString(), fi.FullName));
                            exception = error;
                            throw exception;
                        }
                    }

                    backgroundWorker2.ReportProgress(index);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }
    }
}
