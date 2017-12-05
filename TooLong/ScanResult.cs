using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TooLong
{
    public class ScanResult : INotifyPropertyChanged
    {
        private string _path;
        private int _length;

        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                if (value != _path)
                {
                    _path = value;
                    NotifyPropertyChanged("Path");
                }
            }
        }
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
                if (value != _length)
                {
                    _length = value;
                    NotifyPropertyChanged("Length");
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
