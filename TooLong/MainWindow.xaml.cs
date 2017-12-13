using System;
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
using static TooLong.Scanner;

namespace TooLong
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isScanning = false;
        private ObservableCollection<ScanItem> _scanResult;

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
            _scanResult = new ObservableCollection<ScanItem>();
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
            UpdateGuiBeforeScan();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            var callback = new Progress<ScanResult>(ProgressHandler);
            _isScanning = true;
            await Task.Run(() => {
                Scan(path, limit, token, callback);
            }, token);
            _isScanning = false;
            UpdateGuiAfterScan(_cancellationTokenSource.IsCancellationRequested);
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private void ProgressHandler(ScanResult scanResult)
        {
            // This is called by Scan() asynchronously and can potentially be called again
            // before the first call finishes.
            var ts = TranslationSource.Instance;
            StatusBarStatsTextBlock.Text = string.Format(ts["StatusBarStatsText"],
                scanResult.TotalPathsScanned,
                scanResult.OverLimit,
                scanResult.OverMaxLen
            );
            if (scanResult.Items != null)
            {
                foreach (var item in scanResult.Items)
                {
                    _scanResult.Add(item);
                }
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Handling of long paths has changed in .NET 4.6.2 and .NET Standard.
            // Consider using System.IO.Path to manipulate paths if targeting a
            // newer framework version.
            // (https://blogs.msdn.microsoft.com/jeremykuhne/2016/06/21/more-on-new-net-path-handling/)
            var row = (DataGridRow)sender;
            var scanResult = (ScanItem)row.DataContext;
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

        private void ResultDataGrid_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            // Give status column room to grow by temporarily shrinking the path column
            var dataGrid = (DataGrid)sender;
            dataGrid.Columns[0].Width = 0;
            dataGrid.UpdateLayout();
            dataGrid.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
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

        private void UpdateGuiBeforeScan()
        {
            var ts = TranslationSource.Instance;
            ScanButton.Content = ts["ScanButtonScanningText"];
            ScanProgressBar.IsIndeterminate = true;
            StatusBarTextBlock.Text = ts["Scanning"] + "...";
            StatusBarStatsTextBlock.Text = string.Format(ts["StatusBarStatsText"], 0, 0, 0);
            _scanResult.Clear();
        }

        private void UpdateGuiAfterScan(bool scanCancelled)
        {
            var ts = TranslationSource.Instance;
            ScanButton.Content = ts["ScanButtonText"];
            ScanProgressBar.IsIndeterminate = false;
            if (scanCancelled)
            {
                StatusBarTextBlock.Text = ts["ScanCancelled"];
            }
            else
            {
                StatusBarTextBlock.Text = ts["Finished"] + "!";
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
