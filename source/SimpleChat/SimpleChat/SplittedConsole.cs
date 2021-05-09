using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleChat
{
    internal class SplittedConsole
    {
        private readonly int _outputAreaHeight;
        
        private readonly object _outputAreaLockObject = new object();
        private readonly List<string> _outputArea = new List<string>();

        private readonly object _promptLockObject = new object();
        private string _prompt;

        private readonly object _inputAreaLockObject = new object();
        private readonly StringBuilder _inputArea = new StringBuilder("");

        private readonly object _statusLockObject = new object();
        private string _status;

        public SplittedConsole()
        {
            _outputAreaHeight = Console.WindowHeight - 3 - 1;
        }

        public void AddLineToOutputArea(string line)
        {
            var lines = SplitString(line);
            foreach (var l in lines)
            {
                Add(l);
            }

            Draw();
        }

        private static IEnumerable<string> SplitString(string line)
        {
            var maxWidth = Console.BufferWidth - 1;
            if (line.Length <= maxWidth)
            {
                return new []{ line };
            }

            var list = new List<string>
            {
                line.Substring(0, maxWidth)
            };

            list.AddRange(SplitString($"    {line.Substring(maxWidth)}"));

            return list;
        }

        private void Add(string line)
        {
            lock (_outputAreaLockObject)
            {
                _outputArea.Add(line);

                if (_outputArea.Count == _outputAreaHeight)
                {
                    _outputArea.RemoveAt(0);
                }
            }
        }

        public void ClearInputArea()
        {
            lock (_inputAreaLockObject)
            {
                _inputArea.Clear();
            }

            Draw();
        }

        public void SetConsolePrompt(string prefix)
        {
            lock (_promptLockObject)
            {
                _prompt = prefix ?? "";
            }

            Draw();
        }

        public string GetInput()
        {
            lock (_inputAreaLockObject)
            {
                return _inputArea.ToString();
            }
        }

        public void SetStatusBar(string status)
        {
            lock (_statusLockObject)
            {
                _status = status;
            }

            Draw();
        }

        private void Draw()
        {
            lock (_outputAreaLockObject)
            {
                var sb = new StringBuilder("");

                for (int i = 0; i < _outputAreaHeight - _outputArea.Count; i++)
                {
                    sb.AppendLine();
                }

                foreach (var t in _outputArea)
                {
                    sb.Append($"{t}");
                    AppendSymbols(sb, ' ', Console.BufferWidth - t.Length - 1);
                    sb.AppendLine();
                }

                AppendStatus(sb);

                var inp = SafeGetInputArea();
                var left = $"{_prompt}{inp}";
                sb.Append(left);

                AppendSymbols(sb, ' ', Console.BufferWidth - left.Length - 1);

                ConsoleWriter.Write(sb.ToString());
                Console.SetCursorPosition(left.Length, Console.WindowHeight - 1);
            }
        }

        public void AppendToInputArea(string txt)
        {
            lock (_inputAreaLockObject)
            {
                _inputArea.Append(txt);
            }

            Draw();
        }

        public void TruncateInputArea()
        {
            lock (_inputAreaLockObject)
            {
                if (_inputArea.Length > 0)
                {
                    _inputArea.Remove(_inputArea.Length - 1, 1);
                }
            }

            Draw();
        }

        private void AppendStatus(StringBuilder sb)
        {
            AppendSymbols(sb, '=', Console.BufferWidth - 1);
            sb.AppendLine();

            lock (_statusLockObject)
            {
                var line = $"===[{_status}]";
                sb.Append(line);
                AppendSymbols(sb, '=', Console.BufferWidth - line.Length - 1);
            }

            sb.AppendLine();

            AppendSymbols(sb, '=', Console.BufferWidth - 1);
            sb.AppendLine();
        }

        private string SafeGetInputArea()
        {
            lock (_inputAreaLockObject)
            {
                return _inputArea.ToString();
            }
        }

        private static void AppendSymbols(StringBuilder sb, char symbol, int count)
        {
            for (int i = 0; i < count; i++)
            {
                sb.Append(symbol);
            }
        }
    }
}