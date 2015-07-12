using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using Tamir.SharpSsh.jsch;

/* 
 * SshBase.cs
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
    /// A wrapper class for JSch's SSH channel
    /// </summary>
    public abstract class SshBase : IDisposable
    {
        public static Version Version { get { return Assembly.GetAssembly(typeof(SshBase)).GetName().Version; } }
        public Session Session {[DebuggerStepThrough] get { return m_session; } }
        public Channel Channel {[DebuggerStepThrough] get { return m_channel; } }

        protected string m_host;
        protected string m_user;
        protected string m_pass;
        protected JSch m_jsch;
        protected Session m_session;
        protected Channel m_channel;
        protected HostKeyCheckType m_checkType = HostKeyCheckType.NoCheck;
        protected UserInfo m_userInfo = null;
        protected string m_hostKeyFileName = null;

        /// <summary>
        /// Default TCP port of SSH protocol
        /// </summary>
        private const int SSH_TCP_PORT = 22;

        /// <summary>
        /// Constructs a new SSH instance
        /// </summary>
        /// <param name="sftpHost">The remote SSH host</param>
        /// <param name="user">The login username</param>
        /// <param name="password">The login password</param>
        public SshBase(string sftpHost, string user, string password)
        {
            this.m_host = sftpHost;
            this.m_user = user;
            this.Password = password;
            m_jsch = new JSch();
        }

        /// <summary>
        /// Constructs a new SSH instance
        /// </summary>
        /// <param name="sftpHost">The remote SSH host</param>
        /// <param name="user">The login username</param>
        public SshBase(string sftpHost, string user)
            : this(sftpHost, user, null)
        {
        }

        /// <summary>
        /// Adds identity file for publickey user authentication
        /// </summary>
        /// <param name="privateKeyFile">The path to the private key file</param>
        public virtual void AddIdentityFile(string privateKeyFile)
        {
            m_jsch.addIdentity(privateKeyFile);
        }

        /// <summary>
        /// Adds identity file for publickey user authentication
        /// </summary>
        /// <param name="privateKeyFile">The path to the private key file</param>
        /// <param name="passphrase">A passphrase for decrypting the private key file</param>
        public virtual void AddIdentityFile(string privateKeyFile, string passphrase)
        {
            m_jsch.addIdentity(privateKeyFile, passphrase);
        }

        /// <summary>
        /// Adds a "known fingerprint" for the target host, and enables strict host checking
        /// </summary>
        /// <param name="hostFingerprint">The known fingerprint of the target host key</param>
        public virtual void SetKnownHostFingerprint(string hostFingerprint)
        {
            m_jsch.setHostKeyRepository(new SshFingerprintOnlyHostsList(m_host, hostFingerprint));
            //the "SshFingerprintOnlyHostsList" does not support saving - "ask" makes no sense.
            m_checkType = HostKeyCheckType.ForceMatch;
        }

        /// <summary>
        /// Sets a "UserInfo" object to handle user prompts.
        /// </summary>
        /// <param name="userInfo">The UserInfo object to be used for obtaining credentials from the user</param>
        public virtual void SetUserInfo(UserInfo userInfo)
        {
            m_userInfo = userInfo;
        }

        /// <summary>
        /// Sets the host key checking rule; defaults to "NoCheck", and only makes sense when combined 
        /// with a "UserInfo" object to capture mismatch-handling decisions from the user.
        /// </summary>
        /// <param name="checkingRule">The host-checking behaviour requested</param>
        public virtual void SetHostKeyCheckingRule(HostKeyCheckType checkingRule)
        {
            m_checkType = checkingRule;
        }

        /// <summary>
        /// Sets the host key filename, to be used to store keys across sessions if a defined fingerprint 
        /// is not provided.
        /// </summary>
        /// <param name="hostKeyFileName">The filename in which to retrieve and persist host keys</param>
        public virtual void SetHostKeyFileName(string hostKeyFileName)
        {
            m_hostKeyFileName = hostKeyFileName;
        }

        protected abstract string ChannelType { get; }

        /// <summary>
        /// Connect to remote SSH server
        /// </summary>
        public virtual void Connect()
        {
            this.Connect(SSH_TCP_PORT);
        }

        /// <summary>
        /// Connect to remote SSH server
        /// </summary>
        /// <param name="tcpPort">The destination TCP port for this connection</param>
        public virtual void Connect(int tcpPort)
        {
            this.ConnectSession(tcpPort);
            this.ConnectChannel();
        }

        protected virtual void ConnectSession(int tcpPort)
        {
            m_session = m_jsch.getSession(m_user, m_host, tcpPort);

            if (Password != null)
            {
                if (m_userInfo == null)
                    m_userInfo = new DisconnectedKeyboardInteractiveUserInfo(Password);
                else
                    throw new InvalidDataException("Cannot combine a predefined 'UserInfo' object with a predefined 'Password' value."); 
            }

            m_session.setUserInfo(m_userInfo);

            //determine how strict to be on host key issues.
            Hashtable config = new Hashtable();
            switch (m_checkType)
            {
                case HostKeyCheckType.AskUser:
                    config.Add("StrictHostKeyChecking", "ask");
                    break;
                case HostKeyCheckType.ForceMatch:
                    config.Add("StrictHostKeyChecking", "yes");
                    break;
                case HostKeyCheckType.NoCheck:
                    config.Add("StrictHostKeyChecking", "no");
                    break;
                default:
                    throw new InvalidDataException("Unknown value provided for 'm_checkType' property");
            }
            m_session.setConfig(config);

            if (m_hostKeyFileName != null)
                m_jsch.getHostKeyRepository().setKnownHosts(m_hostKeyFileName);

            m_session.connect();
        }

        protected virtual void ConnectChannel()
        {
            m_channel = m_session.openChannel(ChannelType);
            this.OnChannelReceived();
            m_channel.connect();
            this.OnConnected();
        }

        protected virtual void OnConnected()
        {
        }

        protected virtual void OnChannelReceived()
        {
        }

        /// <summary>
        /// Returns a delegate that, when called, will return
        /// an array of bytes from the stream. Will return
        /// null when no more results are available. Will not return
        /// a zero-length array.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected Func<byte[]> StreamResults(Stream s)
        {
            byte[] buff = new byte[1024];
            object l = new object();

            return delegate() {
                lock(l)
                {
                    if(buff == null)
                        return null;

                    int count = s.Read(buff, 0, 1024);
                    if(count <= 0)
                    {
                        buff = null;
                        return null;
                    }

                    byte[] result = new byte[count];
                    Array.Copy(buff, result, count);
                    return result;
                }
            };
        }

        /// <summary>
        /// Stream results from the channel as ASCII strings. Only the 
        /// "standard output" stream will be checked, not the "error" stream.
        /// Results will be streamed in approximately 1K chunks. 
        /// </summary>
        /// <param name="chan">SSH Channel to use. Should NOT be connected yet. Will be disconnected
        /// when streaming finishes.</param>
        /// <returns></returns>
        protected IEnumerator<string> StreamASCII(Channel chan)
        {
            if(chan == null)
                throw new ArgumentNullException("chan");

            if(chan.connected)
                throw new InvalidOperationException("The channel must not be connected when StreamASCII is called.");

            Stream s = chan.getInputStream();
            chan.connect();

            try
            {
                var fn = StreamResults(s);
                byte[] buff;

                while((buff = fn()) != null)
                    yield return Encoding.ASCII.GetString(buff);
            }
            finally
            {
                chan.disconnect();
            }
        }

        /// <summary>
        /// Get intermediate results as ASCII strings. Only the 
        /// "standard output" stream will be checked, not the "error" stream.
        /// Results will be collected until the separator byte is seen,
        /// at which time a string will be yielded. The separator
        /// byte will be included at the end of each string (except possibly
        /// the last).
        /// </summary>
        /// <param name="sep">A value to separate strings by (e.g., 0x10 for newline). </param>
        /// <param name="chan">SSH Channel to use. Should NOT be connected yet. Will be disconnected
        /// when streaming finishes.</param>
        /// <returns></returns>
        protected IEnumerator<string> StreamASCII(byte sep, Channel chan)
        {
            if(chan == null)
                throw new ArgumentNullException("chan");

            if(chan.connected)
                throw new InvalidOperationException("The channel must not be connected when StreamASCII is called.");

            Stream s = chan.getInputStream();
            chan.connect();

            try
            {
                var res = new StringBuilder();
                var fn = StreamResults(s);
                byte[] buff;

                while((buff = fn()) != null)
                {
                    int upTo;
                    if((upTo = Array.IndexOf(buff, sep) + 1) != 0)
                    {
                        if(res.Length > 0)
                        {
                            res.Append(Encoding.ASCII.GetString(buff, 0, upTo));
                            yield return res.ToString();
                            res.Length = 0;
                        }
                        else
                            yield return Encoding.ASCII.GetString(buff, 0, upTo);

                        if(upTo < buff.Length)
                        {
                            int currUpTo = 0;
                            while((currUpTo = Array.IndexOf(buff, sep, upTo) + 1) != 0)
                            {
                                yield return Encoding.ASCII.GetString(buff, upTo, currUpTo - upTo);
                                upTo = currUpTo;
                            }

                            if(upTo < buff.Length)
                                res.Append(Encoding.ASCII.GetString(buff, upTo, buff.Length - upTo));
                        }
                    }
                    else
                        res.Append(Encoding.ASCII.GetString(buff, 0, buff.Length));
                }

                if(res.Length > 0)
                    yield return res.ToString();
            }
            finally
            {
                chan.disconnect();
            }
        }

        /// <summary>
        /// Closes the SSH subsystem
        /// </summary>
        public virtual void Close()
        {
            if (m_channel != null)
            {
                m_channel.disconnect();
                m_channel = null;
            }
            if (m_session != null)
            {
                m_session.disconnect();
                m_session = null;
            }
        }

        /// <summary>
        /// Return true if the SSH subsystem is connected
        /// </summary>
        public virtual bool Connected
        {
            get
            {
                if (m_session != null)
                    return m_session.isConnected();
                return false;
            }
        }

        /// <summary>
        /// Gets the Cipher algorithm name used in this SSH connection.
        /// </summary>
        public string Cipher
        {
            get
            {
                CheckConnected();
                return m_session.getCipher();
            }
        }

        /// <summary>
        /// Gets the MAC algorithm name used in this SSH connection.
        /// </summary>
        public string Mac
        {
            get
            {
                CheckConnected();
                return m_session.getMac();
            }
        }

        /// <summary>
        /// Gets the server SSH version string.
        /// </summary>
        public string ServerVersion
        {
            get
            {
                CheckConnected();
                return m_session.getServerVersion();
            }
        }

        /// <summary>
        /// Gets the client SSH version string.
        /// </summary>
        public string ClientVersion
        {
            get
            {
                CheckConnected();
                return m_session.getClientVersion();
            }
        }

        public string Host
        {
            get
            {
                CheckConnected();
                return m_session.getHost();
            }
        }

        public HostKey HostKey
        {
            get
            {
                CheckConnected();
                return m_session.getHostKey();
            }
        }

        public int Port
        {
            get
            {
                CheckConnected();
                return m_session.getPort();
            }
        }

        /// <summary>
        /// The password string of the SSH subsystem
        /// </summary>
        public string Password
        {
            get { return m_pass; }
            set { m_pass = value; }
        }

        public string Username
        {
            get { return m_user; }
        }

        /// <summary>
        /// The proxy that is used for connections. If null, no proxy is used.
        /// </summary>
        public IProxy Proxy
        {
            get { return this.m_session.GetProxy(); }
            set { this.m_session.setProxy(value); }
        }

        private void CheckConnected()
        {
            if (!Connected)
            {
                throw new Exception("SSH session is not connected.");
            }
        }

        /// <summary>
        /// For password and KI auth modes
        /// </summary>
        protected class DisconnectedKeyboardInteractiveUserInfo : UserInfo, UIKeyboardInteractive
        {
            private string _password;

            public DisconnectedKeyboardInteractiveUserInfo(string password)
            {
                _password = password;
            }

            #region UIKeyboardInteractive Members

            public string[] promptKeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo)
            {
                return new string[] { _password };
            }

            #endregion

            #region UserInfo Members

            public bool promptYesNo(string message)
            {
                return true;
            }

            public bool promptPassword(string message)
            {
                return true;
            }

            public string getPassword()
            {
                return _password;
            }

            public bool promptPassphrase(string message)
            {
                return true;
            }

            public string getPassphrase()
            {
                return null;
            }

            public void showMessage(string message)
            {
            }

            #endregion
        }

        public void Dispose()
        {
            this.Close();
        }
    }

    public enum HostKeyCheckType 
    {
        AskUser,
        ForceMatch,
        NoCheck
    }
}