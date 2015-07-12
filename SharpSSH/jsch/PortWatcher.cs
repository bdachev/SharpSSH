using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Tamir.SharpSsh.java.lang;
using Tamir.SharpSsh.java.net;
using Socket = Tamir.SharpSsh.java.net.Socket;

namespace Tamir.SharpSsh.jsch
{
    /* -*-mode:java; c-basic-offset:2; -*- */
    /*
    Copyright (c) 2002,2003,2004 ymnk, JCraft,Inc. All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:

      1. Redistributions of source code must retain the above copyright notice,
         this list of conditions and the following disclaimer.

      2. Redistributions in binary form must reproduce the above copyright 
         notice, this list of conditions and the following disclaimer in 
         the documentation and/or other materials provided with the distribution.

      3. The names of the authors may not be used to endorse or promote products
         derived from this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
    INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
    FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
    INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
    INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
    LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
    OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
    EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
    */

    internal class PortWatcher : JavaRunnable
    {
        private static ArrayList pool = new ArrayList();

        private Session session;
        private int lport;
        private int rport;
        private String host;
        private InetAddress boundaddress;
        private JavaRunnable thread;
        private TcpListener ss;

        public PortWatcher(Session session, String address, int lport, String host, int rport, ServerSocketFactory factory)
        {
            this.session = session;
            this.lport = lport;
            this.host = host;
            this.rport = rport;

            try
            {
                //Ignore microsoft's warning regarding "GetHostByName" being obsoleted.
                //
                //The proposed alternative, "GetHostEntry", is intended to be more generally correct (also handles IPV6), but
                // it actually does completely the WRONG thing when provided with a raw IPV4 address like "127.0.0.1": it should
                // use it as-is (at least that was the intention in this code), and instead it identifies the host and gets ALL 
                // the most-specific IP addresses of the host ("localhost"), including IPV6 link-local addresses etc. So you say 
                // "Bind to 127.0.0.1", and you get a listener that is actually bound to some link-local IPV6 address like 
                // "fe80::84a:eaa6:244a:bde1%12" OR (if you filter out IPV6) "192.168.1.111", either way NOT binding to localhost!
                //
                // In other words, only change this if you really know what you're doing.
                //
                boundaddress = new InetAddress(
                                        Dns.GetHostEntry(address).AddressList[0]);
                                        // Dns.GetHostByName(address).AddressList[0]);

                ss = (factory == null) ?
                        new TcpListener(boundaddress.addr, lport) :
                        factory.createServerSocket(lport, 0, boundaddress);

                //In the move from custom "ServerSocket" class to standard "TcpListener", we lost the auto-start.
                ss.Start();
            }
            catch (Exception e)
            {
                throw new JSchException("PortForwardingL: local port " + address + ":" + lport + " cannot be bound.", e);
            }
        }

        public static String[] getPortForwarding(Session session)
        {
            ArrayList foo = new ArrayList();

            lock (pool)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    PortWatcher p = (PortWatcher) (pool[i]);
                    if (p.session == session)
                    {
                        foo.Add(p.lport + ":" + p.host + ":" + p.rport);
                    }
                }
            }

            String[] bar = new String[foo.Count];

            for (int i = 0; i < foo.Count; i++)
            {
                bar[i] = (String) (foo[i]);
            }

            return bar;
        }

        public static PortWatcher getPort(Session session, String address, int lport)
        {
            InetAddress addr;

            try
            {
                addr = new InetAddress(Dns.GetHostEntry(address).AddressList[0]);
            }
            catch (Exception uhe)
            {
                throw new JSchException("PortForwardingL: invalid address " + address + " specified.", inner: uhe);
            }

            lock (pool)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    PortWatcher p = (PortWatcher) (pool[i]);

                    if (p.session == session && p.lport == lport)
                    {
                        if (p.boundaddress.isAnyLocalAddress() || p.boundaddress.equals(addr))
                        {
                            return p;
                        }
                    }
                }
                return null;
            }
        }

        public static PortWatcher addPort(Session session, String address, int lport, String host, int rport, ServerSocketFactory ssf)
        {
            if (getPort(session, address, lport) != null)
            {
                throw new JSchException("PortForwardingL: local port " + address + ":" + lport + " is already registered.");
            }

            PortWatcher pw = new PortWatcher(session, address, lport, host, rport, ssf);

            pool.Add(pw);

            return pw;
        }

        public static void delPort(Session session, String address, int lport)
        {
            PortWatcher pw = getPort(session, address, lport);

            if (pw == null)
            {
                throw new JSchException("PortForwardingL: local port " + address + ":" + lport + " is not registered.");
            }

            pw.Delete();

            pool.Remove(pw);
        }

        public static void delPort(Session session)
        {
            lock (pool)
            {
                PortWatcher[] foo = new PortWatcher[pool.Count];

                int count = 0;

                for (int i = 0; i < pool.Count; i++)
                {
                    PortWatcher p = (PortWatcher) (pool[i]);

                    if (p.session == session)
                    {
                        p.Delete();
                        foo[count++] = p;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    PortWatcher p = foo[i];

                    pool.Remove(p);
                }
            }
        }

        public void run()
        {
            Buffer buf = new Buffer(300); // ??
            Packet packet = new Packet(buf);
            thread = this;

            try
            {
                while (thread != null)
                {
                    Socket socket = new Socket(ss.AcceptSocket());

                    socket.setTcpNoDelay(true);

                    Stream In = socket.getInputStream();
                    Stream Out = socket.getOutputStream();

                    ChannelDirectTCPIP channel = new ChannelDirectTCPIP();

                    channel.init();
                    channel.setInputStream(In);
                    channel.setOutputStream(Out);

                    session.addChannel(channel);

                    ((ChannelDirectTCPIP) channel).setHost(host);
                    ((ChannelDirectTCPIP) channel).setPort(rport);
                    ((ChannelDirectTCPIP) channel).setOrgIPAddress(socket.getInetAddress().getHostAddress());
                    ((ChannelDirectTCPIP) channel).setOrgPort(socket.getPort());

                    channel.connect();

                    if (channel.exitstatus != -1)
                    {
                    }
                }
            }
            catch (Exception) { }

            Delete();
        }

        private void Delete()
        {
            try
            {
                thread = null;

                if (ss != null) ss.Stop();

                ss = null;
            }
            catch (Exception) { }
        }
    }
}