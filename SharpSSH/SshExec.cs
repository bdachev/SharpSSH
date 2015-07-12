using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tamir.SharpSsh.jsch;

/* 
 * SshExe.cs
 * 
 * Copyright (c) 2006 Tamir Gal, http://www.tamirgal.com, All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *  	1. Redistributions of source code must retain the above copyright notice,
 *		this list of conditions and the following disclaimer.
 *
 *	    2. Redistributions in binary form must reproduce the above copyright 
 *		notice, this list of conditions and the following disclaimer in 
 *		the documentation and/or other materials provided with the distribution.
 *
 *	    3. The names of the authors may not be used to endorse or promote products
 *		derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR
 *  *OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
 * OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 **/

namespace Tamir.SharpSsh
{
    /// <summary>
    /// Summary description for SshExe.
    /// </summary>
    public class SshExec : SshBase
    {
        public SshExec(string host, string user, string password)
            : base(host, user, password)
        {
        }

        public SshExec(string host, string user)
            : base(host, user)
        {
        }

        protected override string ChannelType
        {
            get { return "exec"; }
        }

        /// <summary>
        ///This function is empty, so no channel is connected
        ///on session connect 
        /// </summary>
        protected override void ConnectChannel()
        {
        }

        protected ChannelExec GetChannelExec(string command)
        {
            ChannelExec exeChannel = (ChannelExec) m_session.openChannel("exec");
            exeChannel.setCommand(command);
            return exeChannel;
        }

        /// <summary>
        /// Executes the given command and provides output as the 
        /// command executes. 
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="pty">Whether to allocate a pseudo-terminal or not.</param>
        /// <param name="sep">Collect results </param>
        /// <returns></returns>
        public IEnumerator<string> RunCommandEx(string command, bool pty = false, byte? sep = null) // = ' ')
        {
            m_channel = GetChannelExec(command);
            ((ChannelExec) m_channel).setPty(pty);

            if (!sep.HasValue)
                sep = Convert.ToByte(" "); // new String(' ', 1)  as byte;

            return StreamASCII(sep.Value, m_channel);
        }

        public string RunCommand(string command)
        {
            m_channel = GetChannelExec(command);
            Stream s = m_channel.getInputStream();
            m_channel.connect();
            byte[] buff = new byte[1024];
            StringBuilder res = new StringBuilder();
            int c = 0;
            while (true)
            {
                c = s.Read(buff, 0, buff.Length);
                if (c == -1) break;
                res.Append(Encoding.Unicode.GetString(buff, 0, c));
                //Console.WriteLine(res);
            }
            m_channel.disconnect();
            return res.ToString();
        }

        public int RunCommand(string command, ref string StdOut, ref string StdErr)
        {
            StdOut = string.Empty;
            StdErr = string.Empty;
            m_channel = GetChannelExec(command);
            Stream stdout = m_channel.getInputStream();
            Stream stderr = ((ChannelExec) m_channel).getErrStream();

            m_channel.connect();
            byte[] buff = new byte[1024];
            StringBuilder sbStdOut = new StringBuilder();
            StringBuilder sbStdErr = new StringBuilder();
            int o = 0;
            int e = 0;
            while (true)
            {
                if (o != -1) o = stdout.Read(buff, 0, buff.Length);
                if (o != -1) sbStdOut.Append(Encoding.Unicode.GetString(buff, 0, o));
                if (e != -1) e = stderr.Read(buff, 0, buff.Length);
                if (e != -1) sbStdErr.Append(Encoding.Unicode.GetString(buff, 0, e));
                if ((o == -1) && (e == -1)) break;
            }
            m_channel.disconnect();
            StdOut = sbStdOut.ToString();
            StdErr = sbStdErr.ToString();

            return m_channel.getExitStatus();
        }

        public ChannelExec ChannelExec
        {
            get { return (ChannelExec) this.m_channel; }
        }
    }
}