using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Tamir.SharpSsh.jsch;
using Tamir.Streams;

/* 
 * SshShell.cs
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
    /// Summary description for SshShell.
    /// </summary>
    public class SshShell : SshBase
    {
        private Stream m_sshIO = null;
        private Regex m_expectPattern;
        private bool m_removeTerminalChars = false;
        private bool m_redirectToConsole = false;
        private Dictionary<int, string> m_localMappedPorts = new Dictionary<int, string>();
        private static string escapeCharsPattern = "\\[[0-9;?]*[^0-9;]";

        public SshShell(string host, string user, string password)
            : base(host, user, password)
        {
            Init();
        }

        public SshShell(string host, string user)
            : base(host, user)
        {
            Init();
        }

        protected void Init()
        {
            ExpectPattern = "";
            m_removeTerminalChars = false;
        }

        protected override void OnChannelReceived()
        {
            base.OnChannelReceived();
            if (m_redirectToConsole)
            {
                SetStream(Console.OpenStandardInput(), Console.OpenStandardOutput());
            }
            else
            {
                m_sshIO = GetStream();
            }
        }


        protected override string ChannelType
        {
            get { return "shell"; }
        }

        public Stream IO
        {
            get
            {
//				if(m_sshIO == null)
//				{
//					m_sshIO = GetStream();
//				}
                return m_sshIO;
            }
        }

        public IEnumerator<string> StreamASCII()
        {
            Channel chan = base.Channel;
            if (chan == null || chan.isConnected())
                yield break;

            var numerator = StreamASCII(chan);
            while (numerator.MoveNext())
                yield return numerator.Current;
        }

        public void WriteLine(string data)
        {
            Write(data + "\r");
        }

        public void Write(string data)
        {
            Write(Encoding.Default.GetBytes(data));
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
        public virtual void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }


        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        public virtual void Write(byte[] buffer, int offset, int count)
        {
            IO.Write(buffer, offset, count);
            IO.Flush();
        }

        /// <summary>
        /// Creates a new I/O stream of communication with this SSH shell connection
        /// </summary>
        public Stream GetStream()
        {
            return new CombinedStream(m_channel.getInputStream(), m_channel.getOutputStream());
            ;
        }

        public void SetStream(Stream inputStream, Stream outputStream)
        {
            m_channel.setInputStream(inputStream);
            m_channel.setOutputStream(outputStream);
        }

        public void SetStream(Stream s)
        {
            SetStream(s, s);
        }

        public void RedirectToConsole()
        {
            m_redirectToConsole = true;
        }

        public virtual bool ShellOpened
        {
            get
            {
                if (m_channel != null)
                {
                    return !m_channel.isClosed();
                }
                return false;
            }
        }

        public virtual bool ShellConnected
        {
            get
            {
                if (m_channel != null)
                {
                    return m_channel.isConnected();
                }
                return false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating wether this Steam sould remove the terminal emulation's escape sequence characters from the response String.
        /// </summary>
        public bool RemoveTerminalEmulationCharacters
        {
            get { return m_removeTerminalChars; }
            set { m_removeTerminalChars = value; }
        }

        /// <summary>
        /// A regular expression pattern string to match when reading the resonse using the ReadResponse() method. The default prompt value is "\n" which makes the ReadRespons() method return one line of response.
        /// </summary>
        public string ExpectPattern
        {
            get { return m_expectPattern.ToString(); }
            set { m_expectPattern = new Regex(value, RegexOptions.Singleline); }
        }

        /// <summary>
        /// Reads a response string from the SSH channel. This method will block until the pattern in the 'Prompt' property is matched in the response.
        /// </summary>
        /// <returns>A response string from the SSH server.</returns>
        public string Expect()
        {
            return Expect(m_expectPattern);
        }

        /// <summary>
        /// Reads a response string from the SSH channel. This method will block until the pattern in the 'Prompt' property is matched in the response.
        /// </summary>
        /// <returns>A response string from the SSH server.</returns>
        public string Expect(string pattern)
        {
            return Expect(new Regex(pattern, RegexOptions.Singleline));
        }

        /// <summary>
        /// Reads a response string from the SSH channel. This method will block until the pattern in the 'Prompt' property is matched in the response.
        /// </summary>
        /// <returns>A response string from the SSH server.</returns>
        public string Expect(Regex pattern)
        {
            int readCount;
            StringBuilder resp = new StringBuilder();
            byte[] buff = new byte[1024];
            Match match;

            do
            {
                readCount = IO.Read(buff, 0, buff.Length);
                if (readCount == -1) break;
                string tmp = Encoding.Default.GetString(buff, 0, readCount);
                resp.Append(tmp, 0, readCount);
                string s = resp.ToString();
                match = pattern.Match(s);
            } while (!match.Success);

            string result = resp.ToString();
            if (RemoveTerminalEmulationCharacters)
                result = HandleTerminalChars(result);
            return result;
        }

        /// <summary>
        /// Reads a response string from the SSH channel. 
        /// This method will continue producing results 
        /// until the pattern in the 'Prompt' property is matched in the response.
        /// </summary>
        /// <returns>A response string from the SSH server.</returns>
        public IEnumerator<string> ExpectEx()
        {
            return ExpectEx(m_expectPattern);
        }

        /// <summary>
        /// Reads a response string from the SSH channel. 
        /// This method will continue producing results until the regex (specified as a string) 
        /// in the argument is matched in the response.
        /// </summary>
        /// <returns>A response string from the SSH server.</returns>
        public IEnumerator<string> ExpectEx(string pattern)
        {
            return ExpectEx(new Regex(pattern, RegexOptions.Singleline));
        }

        /// <summary>
        /// Reads a response string from the SSH channel. 
        /// This method will continue producing results until the pattern in the argument 
        /// is matched in the response.
        /// </summary>
        /// <returns>A response string from the SSH server.</returns>
        public IEnumerator<string> ExpectEx(Regex pattern)
        {
            // int readCount;
            string result = String.Empty;
            var fn = StreamResults(IO);
            byte[] buff;
            // Match match;

            while((buff = fn()) != null)
            {
                string tmp = Encoding.ASCII.GetString(buff, 0, buff.Length);
                yield return RemoveTerminalEmulationCharacters ? 
                    HandleTerminalChars(tmp) : tmp;

                result = String.Concat(result, tmp);

                if(pattern.IsMatch(result))
                    break;
            } 
        }

        /// <summary>
        /// Closes the SSH subsystem
        /// </summary>
        public override void Close()
        {
            foreach (int localPort in m_localMappedPorts.Keys)
            {
                m_session.delPortForwardingL(localPort);
            }
            base.Close();
        }

        /// <summary>
        /// Sets up a Local Port Forward on the current session, using  specific local port number.
        /// </summary>
        public void ForwardLocalPortToRemote(int localPort, string remoteHostName, int remotePort)
        {
            if (m_localMappedPorts.ContainsKey(remotePort))
                throw new InvalidOperationException("Remote port number {0} already mapped!");

            if (!this.Connected)
                throw new InvalidOperationException("Shell must be connected before port forwarding can be configured!");

            m_session.setPortForwardingL(localPort, remoteHostName, remotePort);
            m_localMappedPorts.Add(localPort, remoteHostName);
        }

        /// <summary>
        /// Sets up a Local Port Forward on the current session, auto-selecting an appropriate local port number to listen on.
        /// </summary>
        /// <returns>The auto-assigned local port number.</returns>
        public int ForwardLocalPortToRemote(string remoteHostName, int remotePort)
        {
            System.Net.Sockets.TcpListener autoListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            autoListener.Start(0);
            int autoAssignedPort = ((System.Net.IPEndPoint)autoListener.LocalEndpoint).Port;
            autoListener.Stop();

            ForwardLocalPortToRemote(autoAssignedPort, remoteHostName, remotePort);

            return autoAssignedPort;
        }

        /// <summary>
        /// Removes escape sequence characters from the input string.
        /// </summary>
        public static string HandleTerminalChars(string str)
        {
            str = str.Replace("(B)0", "");
            str = Regex.Replace(str, escapeCharsPattern, "");
            str = str.Replace(((char) 15).ToString(), "");
            str = Regex.Replace(str, ((char) 27) + "=*", "");
            //str = Regex.Replace(str, "\\s*\r\n", "\r\n");
            return str;
        }
    }
}