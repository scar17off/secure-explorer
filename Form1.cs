using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Secure_Explorer
{
    public partial class Form1 : Form
    {
        private ContextMenuStrip contextMenuStrip;

        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "C:\\";
            textBox3.Text = "1";
            UpdateFileList();
            textBox2.KeyPress += TextBox2_KeyPress;
            InitializeContextMenu();
        }

        private void InitializeContextMenu()
        {
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add("Encrypt", null, EncryptMenuItem_Click);
            contextMenuStrip.Items.Add("Decrypt", null, DecryptMenuItem_Click);
            dataGridView1.ContextMenuStrip = contextMenuStrip;
            
            dataGridView1.CellMouseClick += DataGridView1_CellMouseClick;
        }

        private void UpdateFileList()
        {
            dataGridView1.Rows.Clear();
            string currentPath = textBox2.Text;
            try
            {
                // Add ".." entry for parent directory, except for root directories
                if (!IsRootDirectory(currentPath))
                {
                    dataGridView1.Rows.Add("..", "<DIR>", "Folder");
                }

                string[] directories = Directory.GetDirectories(currentPath);
                string[] files = Directory.GetFiles(currentPath);
                foreach (string directory in directories)
                {
                    dataGridView1.Rows.Add(Path.GetFileName(directory), "<DIR>", "Folder");
                }
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string fileSize = FormatFileSize(fileInfo.Length);
                    string fileType = Path.GetExtension(file).TrimStart('.').ToUpper() + " File";
                    dataGridView1.Rows.Add(Path.GetFileName(file), fileSize, fileType);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private bool IsRootDirectory(string path)
        {
            return Path.GetDirectoryName(path) == null;
        }
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            UpdateFileList();
        }
        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                string fileName = dataGridView1.Rows[e.RowIndex].Cells["NameColumn"].Value.ToString();
                string fileType = dataGridView1.Rows[e.RowIndex].Cells["TypeColumn"].Value.ToString();

                if (fileName == ".." && fileType == "Folder")
                {
                    // Navigate to parent directory
                    string parentPath = Path.GetDirectoryName(textBox2.Text);
                    if (parentPath != null)
                    {
                        textBox2.Text = parentPath;
                        UpdateFileList();
                    }
                }
                else
                {
                    string fullPath = Path.Combine(textBox2.Text, fileName);

                    if (fileType == "Folder")
                    {
                        textBox2.Text = fullPath;
                        UpdateFileList();
                    }
                    else if (File.Exists(fullPath))
                    {
                        MessageBox.Show($"Opening file: {fullPath}", "File Open", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        private void TextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                UpdateFileList();
            }
        }

        private void EncryptMenuItem_Click(object sender, EventArgs e)
        {
            ProcessSelectedItems(true);
        }

        private void DecryptMenuItem_Click(object sender, EventArgs e)
        {
            ProcessSelectedItems(false);
        }

        private async void ProcessSelectedItems(bool encrypt)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                string password = textBox1.Text;
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Please enter a password in the password field.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int threadCount = int.Parse(textBox3.Text);
                int totalItems = 0;
                int processedItems = 0;

                List<string> itemsToProcess = new List<string>();

                // Count total items and prepare the list of items to process
                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                {
                    string fileName = row.Cells["NameColumn"].Value.ToString();
                    string fileType = row.Cells["TypeColumn"].Value.ToString();
                    string fullPath = Path.Combine(textBox2.Text, fileName);

                    if (fileType == "Folder")
                    {
                        totalItems += CountItemsInFolder(fullPath);
                        itemsToProcess.AddRange(Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories));
                        itemsToProcess.AddRange(Directory.GetDirectories(fullPath, "*", SearchOption.AllDirectories));
                    }
                    else
                    {
                        totalItems++;
                        itemsToProcess.Add(fullPath);
                    }
                }

                progressBar1.Minimum = 0;
                progressBar1.Maximum = totalItems;
                progressBar1.Value = 0;

                // Process items in parallel
                await Task.Run(() =>
                {
                    Parallel.ForEach(itemsToProcess, new ParallelOptions { MaxDegreeOfParallelism = threadCount },
                        (item) =>
                        {
                            if (Directory.Exists(item))
                            {
                                ProcessFolder(item, encrypt, password, ref processedItems);
                            }
                            else
                            {
                                ProcessFile(item, encrypt, password);
                                Interlocked.Increment(ref processedItems);
                            }
                            UpdateProgressBar(processedItems);
                        });
                });

                UpdateFileList();

                progressBar1.Value = 0;
            }
        }

        private int CountItemsInFolder(string folderPath)
        {
            int count = 1; // Count the folder itself
            try
            {
                count += Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Length;
                count += Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories).Length;
            }
            catch (Exception)
            {
                
            }
            return count;
        }

        private void ProcessFolder(string folderPath, bool encrypt, string password, ref int processedItems)
        {
            try
            {
                string folderName = Path.GetFileName(folderPath);
                string parentFolder = Path.GetDirectoryName(folderPath);
                string newFolderName = encrypt ? folderName + ".secure" : folderName.EndsWith(".secure") ? folderName.Substring(0, folderName.Length - 7) : folderName;
                string newFolderPath = Path.Combine(parentFolder, newFolderName);

                if (folderPath != newFolderPath)
                {
                    Directory.Move(folderPath, newFolderPath);
                }
                Interlocked.Increment(ref processedItems);
                UpdateProgressBar(processedItems);
            }
            catch (Exception)
            {
                
            }
        }

        private void ProcessFile(string filePath, bool encrypt, string password)
        {
            try
            {
                string newFilePath;
                if (encrypt)
                {
                    // Check if the file is already encrypted
                    if (!Path.GetExtension(filePath).Equals(".secure", StringComparison.OrdinalIgnoreCase))
                    {
                        newFilePath = filePath + ".secure";
                        EncryptFile(filePath, newFilePath, password);
                        File.Delete(filePath);
                    }
                }
                else
                {
                    if (Path.GetExtension(filePath).Equals(".secure", StringComparison.OrdinalIgnoreCase))
                    {
                        newFilePath = Path.ChangeExtension(filePath, null);
                        DecryptFile(filePath, newFilePath, password);
                        File.Delete(filePath);
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }

        private void EncryptFile(string inputFile, string outputFile, string password)
        {
            try
            {
                byte[] salt = GenerateRandomSalt();
                using (var aes = new AesManaged())
                {
                    var key = new Rfc2898DeriveBytes(password, salt, 50000);
                    aes.Key = key.GetBytes(aes.KeySize / 8);
                    aes.IV = key.GetBytes(aes.BlockSize / 8);

                    using (var inputStream = new FileStream(inputFile, FileMode.Open))
                    using (var outputStream = new FileStream(outputFile, FileMode.Create))
                    {
                        outputStream.Write(salt, 0, salt.Length);
                        using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            inputStream.CopyTo(cryptoStream);
                        }
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }

        private void DecryptFile(string inputFile, string outputFile, string password)
        {
            try
            {
                byte[] salt = new byte[32];
                using (var inputStream = new FileStream(inputFile, FileMode.Open))
                {
                    inputStream.Read(salt, 0, salt.Length);
                    using (var aes = new AesManaged())
                    {
                        var key = new Rfc2898DeriveBytes(password, salt, 50000);
                        aes.Key = key.GetBytes(aes.KeySize / 8);
                        aes.IV = key.GetBytes(aes.BlockSize / 8);

                        using (var outputStream = new FileStream(outputFile, FileMode.Create))
                        using (var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(outputStream);
                        }
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }

        private byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private void DataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                if (!dataGridView1.Rows[e.RowIndex].Selected)
                {
                    dataGridView1.ClearSelection();
                    dataGridView1.Rows[e.RowIndex].Selected = true;
                }
            }
        }

        private void UpdateProgressBar(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(UpdateProgressBar), value);
            }
            else
            {
                progressBar1.Value = Math.Min(value, progressBar1.Maximum);
            }
        }
    }
}