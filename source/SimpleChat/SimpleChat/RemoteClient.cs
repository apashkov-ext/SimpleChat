using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleChat
{
    internal class RemoteClient : IDisposable
    {
        private readonly object _clientLockObject = new object();
        private readonly TcpClient _tcpClient;

        public delegate void IncomingMessageEventHandler(string message);
        public event IncomingMessageEventHandler OnIncomingMessage;

        public delegate void ErrorEventHandler(Exception e);
        public event ErrorEventHandler OnError;

        private readonly object _messagesToSendLockObject = new object();
        private readonly Queue<string> _messagesToSend = new Queue<string>();

        public RemoteClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        public void StartChat()
        {
            Task.Run(() =>
            {
                using var stream = _tcpClient.GetStream();

                while (true)
                {
                    if (stream.DataAvailable)
                    {
                        var msg = Read(stream);
                        OnIncomingMessage?.Invoke(msg);
                    }

                    lock (_messagesToSendLockObject)
                    {
                        if (_messagesToSend.Count > 0)
                        {
                            Write(stream, _messagesToSend.Dequeue());
                        }
                    }
                }
            });
        }

        public void SendMessage(string message)
        {
            lock (_messagesToSendLockObject)
            {
                _messagesToSend.Enqueue(message);
            }
        }

        private string Read(Stream stream)
        {
            var bytes = new byte[_tcpClient.Available];
            stream.Read(bytes, 0, _tcpClient.Available);
            var message = Encoding.Unicode.GetString(bytes);
            return message;
        }

        private static void Write(Stream stream, string msg)
        {
            var bytes = Encoding.Unicode.GetBytes(msg);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            _tcpClient?.Dispose();
        }
    }
}