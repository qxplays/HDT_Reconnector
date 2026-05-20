using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace HDT_Reconnector.GameLog
{
    internal class LogReader
    {
        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();
        private readonly string _filePath;
        private long _offset;
        private bool _stop;
        private Thread _threadRead;
        private Thread _threadParse;

        public event Action<string> OnNewLine;

        public LogReader(string filePath)
        {
            _filePath = filePath;
        }

        public void Start()
        {
            _stop = false;
            _offset = 0;
            _threadRead = new Thread(ReadLogFile) { IsBackground = true };
            _threadRead.Start();
            _threadParse = new Thread(ParseLogFile) { IsBackground = true };
            _threadParse.Start();
        }

        public void Stop()
        {
            _stop = true;
            StopThread(_threadRead);
            StopThread(_threadParse);
        }

        private static void StopThread(Thread thread)
        {
            if (thread == null)
                return;

            while (thread.ThreadState == ThreadState.Unstarted)
                Thread.Sleep(50);

            thread.Join(2000);
        }

        private void ParseLogFile()
        {
            while (!_stop)
            {
                var count = _lines.Count;
                for (var i = 0; i < count; i++)
                {
                    if (_lines.TryDequeue(out var line))
                        OnNewLine?.Invoke(line);
                }

                Thread.Sleep(LogWatcher.UpdateDelayMs);
            }
        }

        private void ReadLogFile()
        {
            while (!_stop)
            {
                var fileInfo = new FileInfo(_filePath);
                if (!fileInfo.Exists)
                {
                    Thread.Sleep(LogWatcher.UpdateDelayMs);
                    continue;
                }

                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(_offset, SeekOrigin.Begin);
                    if (fs.Length == _offset)
                    {
                        Thread.Sleep(LogWatcher.UpdateDelayMs);
                        continue;
                    }

                    using (var sr = new StreamReader(fs))
                    {
                        string line;
                        while (!sr.EndOfStream && (line = sr.ReadLine()) != null)
                        {
                            _lines.Enqueue(line);
                            _offset += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                        }
                    }
                }

                Thread.Sleep(LogWatcher.UpdateDelayMs);
            }
        }
    }
}
