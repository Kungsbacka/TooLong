using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using TooLong.Properties;
using static TooLong.NativeMethods;
using System.Runtime.InteropServices;

namespace TooLong
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isScanning = false;
        public ObservableCollection<ScanResult> _scanResult;

        public MainWindow()
        {
            InitializeComponent();
            var ts = TranslationSource.Instance;
            if (!string.IsNullOrEmpty(Settings.Default.SubstDriveLetter))
            {
                Settings.Default.SubstDriveLetter = Settings.Default.SubstDriveLetter.ToUpper();
            }
            if (!DosDeviceHelpers.IsDriveLetter(Settings.Default.SubstDriveLetter))
            {
                DisplayError(ts["WarningInvalidDriveLetter"]);
                Settings.Default.SubstDriveLetter = "Z:";
            }
            if (Settings.Default.MRU == null)
            {
                Settings.Default.MRU = new System.Collections.Specialized.StringCollection();
            }
            else
            {
                foreach (var item in Settings.Default.MRU)
                {
                    PathComboBox.Items.Add(item);
                }
            }
            LimitTextBox.Text = Settings.Default.Limit.ToString();
            _scanResult = new ObservableCollection<ScanResult>();
            ResultDataGrid.ItemsSource = _scanResult;
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning)
            {
                _cancellationTokenSource.Cancel();
                return;
            }
            if (!CheckInput())
            {
                return;
            }
            int limit = int.Parse(LimitTextBox.Text);
            string path = PathComboBox.Text;
            if (!PathComboBox.Items.Cast<string>().Any(t => t.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                PathComboBox.Items.Insert(0, path);
            }
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Dispose();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            ToggleGui();
            var callback = new Progress<ScanStatus>(ProgressHandler);
            await Task.Run(() => {
                Scan(path, limit, token, callback);
            }, token);
            ToggleGui(token.IsCancellationRequested);
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private void ProgressHandler(ScanStatus status)
        {
            // This is called by Scan() asynchronously and can potentially be called again
            // before the first call finishes.
            var ts = TranslationSource.Instance;
            StatusBarStatsTextBlock.Text = string.Format(ts["StatusBarStatsText"],
                status.TotalPathsScanned,
                status.OverLimit,
                status.OverMaxLen
            );
            foreach (var item in status.ScanResult)
            {
                _scanResult.Add(item);
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Handling of long paths has changed in .NET 4.6.2 and .NET Standard.
            // Consider using System.IO.Path to manipulate paths if targeting a
            // newer framework version.
            // (https://blogs.msdn.microsoft.com/jeremykuhne/2016/06/21/more-on-new-net-path-handling/)
            var row = (DataGridRow)sender;
            var scanResult = (ScanResult)row.DataContext;
            var path = scanResult.Path.TrimEnd(Path.DirectorySeparatorChar);
            if (!scanResult.IsDirectory)
            {
                int i = path.Length - 1;
                while(i > 0 && path[i] != Path.DirectorySeparatorChar)
                {
                    i--;
                }
                if (i <= 0)
                {
                    return;
                }
                path = path.Substring(0, i);
                path.TrimEnd(Path.DirectorySeparatorChar);
            }
            if (path.Length >= MAX_PATH)
            {
                int i = MAX_PATH - 1;
                while (i > 0 && path[i] != Path.DirectorySeparatorChar)
                {
                    i--;
                }
                if (i <= 0)
                {
                    return;
                }
                string root = path.Substring(0, i);
                string sub = path.Substring(i + 1, path.Length - i - 1);
                if (DosDeviceHelpers.DosDeviceExist(Settings.Default.SubstDriveLetter))
                {
                    if (!DosDeviceHelpers.RemoveDosDevice(Settings.Default.SubstDriveLetter))
                    {
                        return;
                    }
                }
                if (!DosDeviceHelpers.AddDosDevice(root, Settings.Default.SubstDriveLetter))
                {
                    return;
                }
                path = Settings.Default.SubstDriveLetter + Path.DirectorySeparatorChar + sub;
            }
            System.Diagnostics.Process.Start(path);
        }

        private bool CheckInput()
        {
            var ts = TranslationSource.Instance;
            if (!int.TryParse(LimitTextBox.Text, out int limit))
            {
                DisplayError(ts["ErrorParsePathLimit"]);
                return false;
            }
            if (limit < 150)
            {
                DisplayError(ts["ErrorLimitTooLow"]);
                return false;
            }
            if (string.IsNullOrEmpty(PathComboBox.Text))
            {
                DisplayError(ts["ErrorEmptyPath"]);
                return false;
            }
            return true;
        }

        private void DisplayError(string message)
        {
            var ts = TranslationSource.Instance;
            MessageBox.Show(message, ts["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ToggleGui(bool cancelled = false)
        {
            var ts = TranslationSource.Instance;
            if (_isScanning)
            {
                ScanButton.Content = ts["ScanButtonText"];
                ScanProgressBar.IsIndeterminate = false;
                if (cancelled)
                {
                    StatusBarTextBlock.Text = ts["ScanCancelled"];
                }
                else
                {
                    StatusBarTextBlock.Text = ts["Finished"] + "!";
                }
                _isScanning = false;
            }
            else
            {
                ScanButton.Content = ts["ScanButtonScanningText"];
                ScanProgressBar.IsIndeterminate = true;
                StatusBarTextBlock.Text = ts["Scanning"] + "...";
                StatusBarStatsTextBlock.Text = string.Format(ts["StatusBarStatsText"], 0, 0, 0);
                _scanResult.Clear();
                _isScanning = true;
            }
        }

        private void UpdateGUI(object scanStatus)
        {
            // This is called by Scan asynchronously. This means that results could be added  
            var ts = TranslationSource.Instance;
            var status = (ScanStatus)scanStatus;
            StatusBarStatsTextBlock.Text = string.Format(ts["StatusBarStatsText"],
                status.TotalPathsScanned,
                status.OverLimit,
                status.OverMaxLen
            );
            foreach (var item in status.ScanResult)
            {
                _scanResult.Add(item);
            }
        }

        private void Scan(string startDir, int limit, CancellationToken cancellationToken, IProgress<ScanStatus> progress)
        {
            var results = new List<ScanResult>();
            int scanTotal = 0, overLimit = 0, overMax = 0;
            ScanStatus scanStatus;
            WIN32_FIND_DATA findFileData = new WIN32_FIND_DATA();
            var stack = new Stack<string>();
            stack.Push(startDir);
            while (stack.Count > 0)
            {
                string currentDir = stack.Pop();
                IntPtr hFind = FindFirstFileEx(
                    currentDir + @"\*",
                    FINDEX_INFO_LEVELS.FindExInfoBasic,
                    findFileData,
                    FINDEX_SEARCH_OPS.FindExSearchNameMatch,
                    IntPtr.Zero,
                    FIND_FIRST_EX_LARGE_FETCH
                );
                if (hFind.ToInt64() == INVALID_HANDLE_VALUE)
                {
                    int error = Marshal.GetLastWin32Error();
                    /* TODO:
                     * ERROR_FILE_NOT_FOUND
                     * ERROR_PATH_NOT_FOUND
                     * ERROR_BAD_NETPATH
                     * ERROR_INVALID_PARAMETER
                     * ERROR_INVALID_NAME
                     */
                }
                else
                {
                    do
                    {
                        if (findFileData.cFileName == "." || findFileData.cFileName == "..")
                        {
                            continue;
                        }
                        string fullPath = currentDir + "\\" + findFileData.cFileName;
                        scanTotal++;
                        bool isDirectory = (findFileData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY;
                        if (isDirectory)
                        {
                            stack.Push(fullPath);
                        }
                        if (fullPath.Length >= limit)
                        {
                            results.Add(new ScanResult()
                            {
                                Path = fullPath,
                                Length = fullPath.Length,
                                IsDirectory = isDirectory
                            });
                            overLimit++;
                            if (fullPath.Length > 260)
                            {
                                overMax++;
                            }
                        }
                        if (scanTotal % 300 == 0 && progress != null)
                        {
                            scanStatus = new ScanStatus()
                            {
                                ScanResult = results.ToArray(),
                                TotalPathsScanned = scanTotal,
                                OverLimit = overLimit,
                                OverMaxLen = overMax
                            };
                            progress.Report(scanStatus);
                        }
                    }
                    while (FindNextFile(hFind, findFileData) && !cancellationToken.IsCancellationRequested);
                    FindClose(hFind);
                }
            }
            if (progress != null)
            {
                scanStatus = new ScanStatus()
                {
                    ScanResult = results.ToArray(),
                    TotalPathsScanned = scanTotal,
                    OverLimit = overLimit,
                    OverMaxLen = overMax
                };
                progress.Report(scanStatus);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            int.TryParse(LimitTextBox.Text, out int limit);
            Settings.Default.Limit = limit;
            Settings.Default.MRU.Clear();
            Settings.Default.MRU.AddRange(PathComboBox.Items.Cast<string>().ToArray());
            Settings.Default.Save();
            DosDeviceHelpers.RemoveAllAddedDosDevices();
        }

        private void PathComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                var cb = (ComboBox)sender;
                if (cb.SelectedIndex > -1)
                {
                    cb.Items.RemoveAt(cb.SelectedIndex);
                    cb.Text = "";
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.MyComputer,
                ShowNewFolderButton = false
            };
            if (PathComboBox.Text.Length > 0)
            {
                folderBrowserDialog.SelectedPath = PathComboBox.Text;
            }
            var result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string path = folderBrowserDialog.SelectedPath;
                if (!PathComboBox.Items.Cast<string>().Any(t => t.Equals(path, StringComparison.OrdinalIgnoreCase)))
                {
                    PathComboBox.Items.Insert(0, path);
                }
                PathComboBox.Text = path;
            }
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            if (settingsWindow.ShouldClearMru)
            {
                PathComboBox.Items.Clear();
                PathComboBox.Text = "";
                Settings.Default.MRU.Clear();
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
