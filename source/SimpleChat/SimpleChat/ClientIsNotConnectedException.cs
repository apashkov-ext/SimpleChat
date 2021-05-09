using System;

namespace SimpleChat
{
    internal class ClientIsNotConnectedException : ApplicationException
    {
        public ClientIsNotConnectedException() : base("Client is not connected") {}
    }
}