using System.IO;
using System.Net;
using System.Net.Sockets;
using NetSocket = System.Net.Sockets.Socket;

namespace Tamir.SharpSsh.java.net
{
    public class Socket
    {
        // private System.Net.Sockets.Socket sock;
        private NetSocket sock;

        public Socket(string host, int port)
        {
            IPAddress ipAddr;
            IPEndPoint ep;
            if (IPAddress.TryParse(host, out ipAddr))
            {
                ep = new IPEndPoint(ipAddr, port);
            }
            else
            {
                ep = new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
            }

            // System.Net.Sockets.Socket
            this.sock = new NetSocket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            this.sock.Connect(ep);
        }

        protected void SetSocketOption(SocketOptionLevel level, SocketOptionName name, int val)
        {
            try
            {
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, val);
            }
            catch
            {
            }
        }

        // System.Net.Sockets.Socket
        public Socket(NetSocket sock)
        {
            this.sock = sock;
        }

        public Stream getInputStream()
        {
            return new NetworkStream(sock);
        }

        public Stream getOutputStream()
        {
            return new NetworkStream(sock);
        }

        public bool isConnected()
        {
            return sock.Connected;
        }

        public void setTcpNoDelay(bool b)
        {
            if (b)
            {
                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
            }
            else
            {
                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 0);
            }
        }

        public void setSoTimeout(int t)
        {
            SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, t);
            SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, t);
        }

        public void Close()
        {
            sock.Close();
        }

        public InetAddress getInetAddress()
        {
            return new InetAddress(((IPEndPoint) sock.RemoteEndPoint).Address);
        }

        public int getPort()
        {
            return ((IPEndPoint) sock.RemoteEndPoint).Port;
        }
    }
}