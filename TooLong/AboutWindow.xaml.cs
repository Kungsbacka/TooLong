using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace TooLong
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            var ts = TranslationSource.Instance;
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            VersionTextBlock.Text = string.Format(CultureInfo.InvariantCulture, ts["VersionString"], v.Major, v.Minor, v.Build, v.Revision);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
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
