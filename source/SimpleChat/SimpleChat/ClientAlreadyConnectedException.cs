using System;

namespace SimpleChat
{
    internal class ClientAlreadyConnectedException : ApplicationException
    {
        public ClientAlreadyConnectedException() : base("Remote client has been already connected") { }
    }
}