using System;
using System.ComponentModel;

namespace TooLong
{
    public class ScanResult : INotifyPropertyChanged
    {
        private string _path;
        private int _length;
        private bool _isDirectory;

        public string Path
        {
            get { return _path; }
            set
            {
                if (value != _path)
                {
                    _path = value;
                    NotifyPropertyChanged(nameof(Path));
                }
            }
        }
        public int Length
        {
            get { return _length; }
            set
            {
                if (value != _length)
                {
                    _length = value;
                    NotifyPropertyChanged(nameof(Length));
                }
            }
        }

        public bool IsDirectory
        {
            get { return _isDirectory; }
            set
            {
                if (value != _isDirectory)
                {
                    _isDirectory = value;
                    NotifyPropertyChanged(nameof(IsDirectory));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
