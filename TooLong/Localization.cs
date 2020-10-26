using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;

namespace TooLong
{
    public class TranslationSource : INotifyPropertyChanged
    {
        private readonly ResourceManager resManager = Properties.Resources.ResourceManager;
        private CultureInfo currentCulture = null;

        public static TranslationSource Instance { get; } = new TranslationSource();

        public string this[string key] => resManager.GetString(key, currentCulture);

        public CultureInfo CurrentCulture
        {
            get { return currentCulture; }
            set
            {
                if (currentCulture != value)
                {
                    currentCulture = value;
                    var @event = PropertyChanged;
                    if (@event != null)
                    {
                        @event.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class LocExtension : Binding
    {
        public LocExtension(string name) : base("[" + name + "]")
        {
            Mode = BindingMode.OneWay;
            Source = TranslationSource.Instance;
        }
    }
}