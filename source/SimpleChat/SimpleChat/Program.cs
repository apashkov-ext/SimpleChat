using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleChat
{
    class Program
    {
        private static readonly SplittedConsole SplittedConsole = new SplittedConsole();

        private static readonly object ClientLockObject = new object();
        private static RemoteClient _client;

        private static string _name;
        private static int _port;

        private static Action<string> _onInputAction;

        private static string _remoteIp;
        private static int _remotePort;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.Write("Enter your name: ");
            _name = Console.ReadLine();

            Console.Write("Enter your port: ");
            _port = int.Parse(Console.ReadLine());
            Console.Clear();

            Reset();
            WaitForConnection();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var info = Console.ReadKey(false);
                    try
                    {
                        HandleKey(info);
                    }
                    catch (ExitException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        SplittedConsole.AddLineToOutputArea($"[ERROR]: {e.Message}");
                        SplittedConsole.ClearInputArea();
                        Reset();
                    }
                }

                Thread.Sleep(10);
            }
        }

        private static void HandleKey(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    _onInputAction?.Invoke(SplittedConsole.GetInput());
                    SplittedConsole.ClearInputArea();
                    break;
                case ConsoleKey.Backspace:
                    SplittedConsole.TruncateInputArea();
                    break;
                case ConsoleKey.Escape:
                    Reset();
                    SplittedConsole.ClearInputArea();
                    break;
                default:
                    SplittedConsole.AppendToInputArea(keyInfo.KeyChar.ToString());
                    break;
            }
        }

        private static void HandleInput(string input)
        {
            switch (input)
            {
                case "exit":
                    throw new ExitException();
                case "connect":
                    Connect();
                    break;
                default:
                    lock (ClientLockObject)
                    {
                        if (_client == null)
                        {
                            throw new ClientIsNotConnectedException();
                        }

                        _client.SendMessage(input);
                    }

                    SplittedConsole.AddLineToOutputArea(input);
                    break;
            }
        }

        private static void Connect()
        {
            SplittedConsole.SetConsolePrompt("Enter IP: ");
            _onInputAction = GetIp;
        }

        private static void GetIp(string input)
        {
            var r = new Regex("(([2]([0-4][0-9]|[5][0-5])|[0-1]?[0-9]?[0-9])[.]){3}(([2]([0-4][0-9]|[5][0-5])|[0-1]?[0-9]?[0-9]))");
            if (!r.IsMatch(input))
            {
                SplittedConsole.AddLineToOutputArea("Incorrect IP address");
                return;
            }

            _remoteIp = input;

            SplittedConsole.SetConsolePrompt("Enter Port: ");
            _onInputAction = GetPort;
        }

        private static void GetPort(string input)
        {
            if (!int.TryParse(input, out int parsed))
            {
                SplittedConsole.AddLineToOutputArea("Incorrect Port");
                return;
            }

            _remotePort = parsed;

            SplittedConsole.AddLineToOutputArea($"Connecting to {_remoteIp}:{_remotePort}...");
            SplittedConsole.SetConsolePrompt("");
            SplittedConsole.ClearInputArea();

            var tcpCli = new TcpClient();
            tcpCli.Connect(_remoteIp, _remotePort);
            SetupRemoteClient(tcpCli);

            SplittedConsole.SetStatusBar($"Connected to {_remoteIp}:{_remotePort}");
            SplittedConsole.AddLineToOutputArea("[INFO]: Connected successfully. Lets chat!");
            Reset();
        }

        private static void Reset()
        {
            SplittedConsole.SetConsolePrompt($"{_name} > ");
            _onInputAction = HandleInput;
        }

        private static void WaitForConnection()
        {
            SplittedConsole.SetStatusBar("Waiting for connection");
            Task.Run(() =>
            {
                var listener = new TcpListener(IPAddress.Any, _port);
                listener.Start();
                var client = listener.AcceptTcpClient();

                try
                {
                    SetupRemoteClient(client);
                    SplittedConsole.AddLineToOutputArea("[INFO]: Remote client has been connected successfully. Lets chat!");
                    var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    SplittedConsole.SetStatusBar($"Connected to {endpoint.Address}:{endpoint.Port}");
                }
                catch (ClientAlreadyConnectedException)
                {
                    listener.Stop();
                }
            });
        }

        private static void SetupRemoteClient(TcpClient tcpClient)
        {
            lock (ClientLockObject)
            {
                if (_client != null)
                {
                    throw new ClientAlreadyConnectedException();
                }

                _client = new RemoteClient(tcpClient);
                _client.OnIncomingMessage += message =>
                {
                    SplittedConsole.AddLineToOutputArea(message);
                };
                _client.OnError += e =>
                {
                    SplittedConsole.AddLineToOutputArea($"[REMOTE CLIENT ERROR]: {e.Message}");
                };
                _client.StartChat();
            }
        }

    }
}
