using System.Collections.Generic;

namespace TooLong
{
    public class ScanStatus
    {
        public ScanResult[] ScanResult { get; set; }
        public int TotalPathsScanned { get; set; }
        public int OverLimit { get; set; }
        public int OverMaxLen { get; set; }
    }
}
