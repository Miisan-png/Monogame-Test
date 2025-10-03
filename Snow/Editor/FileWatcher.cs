using System;
using System.IO;

namespace Snow.Engine
{
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private Action<string> _onFileChanged;
        private string _watchedFile;

        public FileWatcher(string filePath, Action<string> onFileChanged)
        {
            _watchedFile = Path.GetFullPath(filePath);
            _onFileChanged = onFileChanged;

            string directory = Path.GetDirectoryName(_watchedFile);
            string fileName = Path.GetFileName(_watchedFile);

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnChanged;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce multiple events
            System.Threading.Thread.Sleep(100);
            _onFileChanged?.Invoke(e.FullPath);
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Changed -= OnChanged;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }

    public class MultiFileWatcher : IDisposable
    {
        private System.Collections.Generic.List<FileWatcher> _watchers;
        
        public MultiFileWatcher()
        {
            _watchers = new System.Collections.Generic.List<FileWatcher>();
        }

        public void WatchFile(string filePath, Action<string> onFileChanged)
        {
            if (File.Exists(filePath))
            {
                _watchers.Add(new FileWatcher(filePath, onFileChanged));
            }
        }

        public void ClearWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        public void Dispose()
        {
            ClearWatchers();
        }
    }
}
