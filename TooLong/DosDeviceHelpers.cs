using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static TooLong.NativeMethods;

namespace TooLong
{
    public static class DosDeviceHelpers
    {
        private static HashSet<string> _addedDrives = new HashSet<string>();
        private static StringBuilder _stringBuilder = new StringBuilder(MAX_PATH);

        internal static bool AddDosDevice(string path, string driveLetter)
        {
            if (!IsDriveLetter(driveLetter))
            {
                throw new ArgumentException(nameof(driveLetter));
            }
            bool result = DefineDosDevice(0, driveLetter, path);
            if (result)
            {
                _addedDrives.Add(driveLetter);
            }
            return result;
        }

        internal static bool DosDeviceExist(string driveLetter)
        {
            if (!IsDriveLetter(driveLetter))
            {
                throw new ArgumentException(nameof(driveLetter));
            }
            int result = QueryDosDevice(driveLetter, _stringBuilder, _stringBuilder.Capacity);
            if (result == 0)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_FILE_NOT_FOUND)
                {
                    return false;
                }
                throw new Exception($"QueryDosDevice failed with error code {err}");
            }
            return true;
        }

        internal static bool RemoveDosDevice(string drive)
        {
            if (!IsDriveLetter(drive))
            {
                throw new ArgumentException(nameof(drive));
            }
            bool result = DefineDosDevice(DDD_REMOVE_DEFINITION, drive, null);
            if (result)
            {
                _addedDrives.Remove(drive);
            }
            return result;
        }

        internal static bool RemoveAllAddedDosDevices()
        {
            foreach (var drive in _addedDrives)
            {
                if (!RemoveDosDevice(drive))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsDriveLetter(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length != 2)
            {
                return false;
            }
            if (input[1] != ':' || input[0] < 'A' || input[0] > 'Z')
            {
                return false;
            }
            return true;        
        }
    }
}
