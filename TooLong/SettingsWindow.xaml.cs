using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TooLong.Properties;

namespace TooLong
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public bool ShouldClearMru { get; private set; } = false;

        public SettingsWindow()
        {
            InitializeComponent();
            var usedDrives = new List<char>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                var driveName = drive.Name.TrimEnd('\\');
                if (driveName.Length == 2 && driveName[1] == ':' && !driveName.Equals(Settings.Default.SubstDriveLetter, StringComparison.OrdinalIgnoreCase))
                {
                    usedDrives.Add(drive.Name[0]);
                }
            }
            var allDrives = "DEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            allDrives
                .Except(usedDrives)
                .ToList()
                .ForEach(d => DriveLetterComboBox.Items.Add(d.ToString() + ":"));
            if (!string.IsNullOrEmpty(Settings.Default.SubstDriveLetter))
            {
                DriveLetterComboBox.SelectedItem = Settings.Default.SubstDriveLetter;
            }
        }

        private void ClearMruButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldClearMru = true;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DriveLetterComboBox.SelectedItem != null)
            {
                Settings.Default.SubstDriveLetter = (string)DriveLetterComboBox.SelectedItem;
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
