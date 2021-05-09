using System;
using System.IO;
using System.Text;

namespace SimpleChat
{
    internal class ConsoleWriter
    {
        private readonly BufferedStream _str;

        private ConsoleWriter(int bufferSize)
        {
            _str = new BufferedStream(Console.OpenStandardOutput(), bufferSize);
        }

        public static void Write(string s, int topPosition = 0)
        {
            Console.SetCursorPosition(0, topPosition);
            var rgb = new byte[Encoding.Unicode.GetByteCount(s)];
            Encoding.Unicode.GetBytes(s, 0, s.Length, rgb, 0);
            var w = new ConsoleWriter(rgb.Length);
            lock (w._str)
            {
                w._str.Write(rgb, 0, rgb.Length);
                w._str.Flush();
            }
        }
    }
}