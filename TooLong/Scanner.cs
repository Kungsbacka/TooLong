using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using static TooLong.NativeMethods;

namespace TooLong
{
    internal static class Scanner
    {
        internal class ScanItem : INotifyPropertyChanged
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

        internal class ScanResult
        {
            public ScanItem[] Items { get; set; }
            public int TotalPathsScanned { get; set; }
            public int OverLimit { get; set; }
            public int OverMaxLen { get; set; }
            public ScanStatus Status { get; set; }
        }

        internal enum ScanStatus { Ok, PathNotFound, AccessDenied, IllegalPath, UnknownError };

        internal static void Scan(string scanPath, int limit, CancellationToken cancellationToken, IProgress<ScanResult> progress)
        {
            var results = new List<ScanItem>();
            int scanTotal = 0, overLimit = 0, overMax = 0;
            ScanResult scanResult;
            WIN32_FIND_DATA findFileData = new WIN32_FIND_DATA();
            var stack = new Stack<string>();
            stack.Push(scanPath);
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

                    ScanStatus status;
                    switch (Marshal.GetLastWin32Error())
                    {
                        case ERROR_FILE_NOT_FOUND:
                        case ERROR_PATH_NOT_FOUND:
                        case ERROR_BAD_NETPATH:
                            status = ScanStatus.PathNotFound;
                            break;
                        case ERROR_ACCESS_DENIED:
                            status = ScanStatus.AccessDenied;
                            break;
                        case ERROR_INVALID_NAME:
                        case ERROR_INVALID_PARAMETER:
                            status = ScanStatus.IllegalPath;
                            break;
                        default:
                            status = ScanStatus.UnknownError;
                            break;
                    }
                    if (progress != null)
                    {
                        progress.Report(new ScanResult() { Status = status });
                    }
                    if (status != ScanStatus.AccessDenied)
                    {
                        return;
                    }
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
                        if (fullPath.Length > limit)
                        {
                            results.Add(new ScanItem()
                            {
                                Path = fullPath,
                                Length = fullPath.Length,
                                IsDirectory = isDirectory
                            });
                            overLimit++;
                        }
                        if (fullPath.Length > MAX_PATH)
                        {
                            overMax++;
                        }
                        if (scanTotal % 300 == 0 && progress != null)
                        {
                            scanResult = new ScanResult()
                            {
                                Items = results.ToArray(),
                                TotalPathsScanned = scanTotal,
                                OverLimit = overLimit,
                                OverMaxLen = overMax,
                                Status = ScanStatus.Ok
                            };
                            results.Clear();
                            progress.Report(scanResult);
                        }
                    }
                    while (FindNextFile(hFind, findFileData) && !cancellationToken.IsCancellationRequested);
                    FindClose(hFind);
                }
            }
            if (progress != null)
            {
                scanResult = new ScanResult()
                {
                    Items = results.ToArray(),
                    TotalPathsScanned = scanTotal,
                    OverLimit = overLimit,
                    OverMaxLen = overMax,
                    Status = ScanStatus.Ok
                };
                progress.Report(scanResult);
            }
        }
    }
}
