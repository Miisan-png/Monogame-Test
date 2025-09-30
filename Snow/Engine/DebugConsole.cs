using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Snow.Engine
{
    public class DebugConsole
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private bool _isOpen;

        public DebugConsole()
        {
            _isOpen = false;
        }

        public void Open()
        {
            if (_isOpen)
                return;

            AllocConsole();
            _isOpen = true;

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            Log("Debug Console Initialized");
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            FreeConsole();
            _isOpen = false;
        }

        public void Log(string message)
        {
            if (!_isOpen)
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"[{timestamp}] ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            if (!_isOpen)
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{timestamp}] WARNING: ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void LogError(string message)
        {
            if (!_isOpen)
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"[{timestamp}] ERROR: ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void LogSuccess(string message)
        {
            if (!_isOpen)
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{timestamp}] SUCCESS: ");
            Console.ResetColor();
            Console.WriteLine(message);
        }

        public void Clear()
        {
            if (!_isOpen)
                return;

            Console.Clear();
        }

        public void Dispose()
        {
            Close();
        }
    }
}


