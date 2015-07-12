using System.Net.Sockets;
using Tamir.SharpSsh.java.net;

namespace Tamir.SharpSsh.jsch
{
    /// <summary>
    /// Summary description for ServerSocketFactory.
    /// </summary>
    public interface ServerSocketFactory
    {
        TcpListener createServerSocket(int port, int backlog, InetAddress bindAddr);
    }
}