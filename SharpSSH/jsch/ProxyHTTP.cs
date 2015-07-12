using System;
using System.IO;
using System.Text;
using Tamir.SharpSsh.java.io;
using Tamir.SharpSsh.java.net;

namespace Tamir.SharpSsh.jsch
{
    public class ProxyHTTP : IProxy
    {
        public const int DEFAULTPORT = 80;

        public Socket Socket { get; private set; }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string User { get; private set; }
        public string Password { get; private set; }

        public Stream InputStream { get { return inputStream.wrappedStream; } }
        public Stream OutputStream { get { return outputStream.wrappedStream; } }

        private JStream inputStream;
        private JStream outputStream;

        public ProxyHTTP(String host)
        {
            this.Port = DEFAULTPORT;
            this.Host = host;

            // try to parse out the port number from the parameter
            if (this.Host.IndexOf(':') != -1)
            {
                try
                {
                    this.Host = host.Substring(0, host.IndexOf(':'));
                    this.Port = Int32.Parse(host.Substring(host.IndexOf(':') + 1));
                }
                catch (Exception e)
                {
                    // should we care if there was a problem?
                }
            }
        }

        public ProxyHTTP(String host, int port)
        {
            this.Host = host;
            this.Port = port;
        }

        public void Connect(SocketFactory socketFactory, String host, int port, int timeout)
        {
            try
            {
                if (socketFactory == null)
                {
                    Socket = Util.createSocket(Host, Port, timeout);
                    inputStream = new JStream(Socket.getInputStream());
                    outputStream = new JStream(Socket.getOutputStream());
                }
                else
                {
                    Socket = socketFactory.createSocket(Host, Port);
                    inputStream = new JStream(socketFactory.getInputStream(Socket));
                    outputStream = new JStream(socketFactory.getOutputStream(Socket));
                }
                if (timeout > 0)
                {
                    Socket.setSoTimeout(timeout);
                }
                Socket.setTcpNoDelay(true);

                outputStream.Write(("CONNECT " + host + ":" + port + " HTTP/1.0\r\n").GetBytes());

                if (User != null && Password != null)
                {
                    byte[] _code = (User + ":" + Password).GetBytes();
                    _code = Util.toBase64(_code, 0, _code.Length);
                    outputStream.Write("Proxy-Authorization: Basic ".GetBytes());
                    outputStream.Write(_code);
                    outputStream.Write("\r\n".GetBytes());
                }

                outputStream.Write("\r\n".GetBytes());
                outputStream.flush();

                int foo = 0;

                StringBuilder sb = new StringBuilder();
                while (foo >= 0)
                {
                    foo = inputStream.read();
                    if (foo != 13)
                    {
                        sb.Append((char)foo);
                        continue;
                    }
                    foo = inputStream.read();
                    if (foo != 10)
                    {
                        continue;
                    }
                    break;
                }
                if (foo < 0)
                {
                    throw new IOException();
                }

                String response = sb.ToString();
                String reason = "Unknow reason";
                int code = -1;
                try
                {
                    foo = response.IndexOf(' ');
                    int bar = response.IndexOf(' ', foo + 1);
                    code = Int32.Parse(response.Substring(foo + 1, bar));
                    reason = response.Substring(bar + 1);
                }
                catch (Exception e)
                {
                }
                if (code != 200)
                {
                    throw new IOException("proxy error: " + reason);
                }

                int count = 0;
                while (true)
                {
                    count = 0;
                    while (foo >= 0)
                    {
                        foo = inputStream.read();
                        if (foo != 13)
                        {
                            count++;
                            continue;
                        }
                        foo = inputStream.read();
                        if (foo != 10)
                        {
                            continue;
                        }
                        break;
                    }
                    if (foo < 0)
                    {
                        throw new IOException();
                    }
                    if (count == 0) break;
                }
            }
            catch (Exception e)
            {
                try
                {
                    if (Socket != null) Socket.Close();
                }
                catch (Exception eee)
                {
                }
                String message = "ProxyHTTP: " + e;
                throw e;
            }
        }

        public void Close()
        {
            try
            {
                if (this.inputStream != null) this.inputStream.Close();

                if (this.outputStream != null) this.outputStream.Close();

                if (this.Socket != null) this.Socket.Close();
            }
            catch (Exception e)
            {

            }
        }
    }
}