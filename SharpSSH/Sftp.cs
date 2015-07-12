using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Tamir.SharpSsh.java.io;
using Tamir.SharpSsh.jsch;
using Tamir.Streams;

/* 
 * Sftp.cs
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
    public class Sftp : SshTransferProtocolBase
    {
        private MyProgressMonitor m_monitor;
        private bool cancelled = false;

        public Sftp(string sftpHost, string user, string password)
            : base(sftpHost, user, password)
        {
            Init();
        }

        public Sftp(string sftpHost, string user)
            : base(sftpHost, user)
        {
            Init();
        }

        private void Init()
        {
            m_monitor = new MyProgressMonitor(this);
        }

        protected override string ChannelType
        {
            get { return "sftp"; }
        }

        private ChannelSftp SftpChannel
        {
            get { return (ChannelSftp) m_channel; }
        }

        public override void Cancel()
        {
            cancelled = true;
        }

        //Get

        public void Get(string fromFilePath)
        {
            Get(fromFilePath, ".");
        }

        public void Get(string[] fromFilePaths)
        {
            for (int i = 0; i < fromFilePaths.Length; i++)
            {
                Get(fromFilePaths[i]);
            }
        }

        public void Get(string[] fromFilePaths, string toDirPath)
        {
            for (int i = 0; i < fromFilePaths.Length; i++)
            {
                Get(fromFilePaths[i], toDirPath);
            }
        }

        public override void Get(string fromFilePath, string toFilePath)
        {
            cancelled = false;
            SftpChannel.get(fromFilePath, toFilePath, m_monitor, ChannelSftp.OVERWRITE);
        }

        /// <summary>
        /// Get content from remote file and save to stream
        /// </summary>
        /// <param name="fromFilePath"></param>
        /// <param name="toStream"></param>
        /// <remarks>Added by Holger Boskugel; opensource@vbwebprofi.de; Berlin, Germany</remarks>
        public void Get(string fromFilePath, OutputStream toStream)
        {
            cancelled = false;
            SftpChannel.get(fromFilePath, toStream, m_monitor, ChannelSftp.OVERWRITE, 0);
        }

        /// <summary>
        /// Get content from remote file and save to stream
        /// </summary>
        /// <param name="fromFilePath"></param>
        /// <param name="toStream"></param>
        /// <remarks>Added by Holger Boskugel; opensource@vbwebprofi.de; Berlin, Germany</remarks>
        public void Get(string fromFilePath, Stream toStream)
        {
            this.Get(fromFilePath, new OutputStreamWrapper(toStream));
        }

        /// <summary>
        /// Get binary data from remote file
        /// </summary>
        /// <param name="fromFilePath"></param>
        /// <param name="toData"></param>
        /// <remarks>Added by Holger Boskugel; opensource@vbwebprofi.de; Berlin, Germany</remarks>
        public void Get(string fromFilePath, out byte[] toData)
        {
            MemoryStream memoryStream = new MemoryStream();

            this.Get(fromFilePath, new OutputStreamWrapper(memoryStream));

            memoryStream.Flush();
            memoryStream.Close();

            toData = memoryStream.ToArray();
        }

        //Put

        public void Put(string fromFilePath)
        {
            Put(fromFilePath, ".");
        }

        public void Put(string[] fromFilePaths)
        {
            for (int i = 0; i < fromFilePaths.Length; i++)
            {
                Put(fromFilePaths[i]);
            }
        }

        public void Put(string[] fromFilePaths, string toDirPath)
        {
            for (int i = 0; i < fromFilePaths.Length; i++)
            {
                Put(fromFilePaths[i], toDirPath);
            }
        }

        public override void Put(string fromFilePath, string toFilePath)
        {
            cancelled = false;
            SftpChannel.put(fromFilePath, toFilePath, m_monitor, ChannelSftp.OVERWRITE);
        }

        /// <summary>
        /// Put content from local stream to remote file
        /// </summary>
        /// <param name="fromStream"></param>
        /// <param name="toFilePath"></param>
        /// <remarks>
        /// Progress monitor not used in method (Stream length not known)
        /// Added by Holger Boskugel; opensource@vbwebprofi.de; Berlin, Germany
        /// </remarks>
        public void Put(InputStream fromStream, string toFilePath)
        {
            cancelled = false;
            // MyProgressMonitor can't be initialized with length of InputStream
            //SftpChannel.put(fromStream, toFilePath, m_monitor, ChannelSftp.OVERWRITE);
            SftpChannel.put(fromStream, toFilePath, null, ChannelSftp.OVERWRITE);
        }

        /// <summary>
        /// Put content from local stream to remote file
        /// </summary>
        /// <param name="fromStream"></param>
        /// <param name="toFilePath"></param>
        /// <remarks>
        /// Progress monitor used, if Stream supports Length property
        /// Added by Holger Boskugel; opensource@vbwebprofi.de; Berlin, Germany
        /// </remarks>
        public void Put(Stream fromStream, string toFilePath)
        {
            long lngMax;

            try
            {
                lngMax = fromStream.Length;
            }
            catch
            {
                lngMax = -1;
            }

            if (lngMax == -1)
            {
                SftpChannel.put(new InputStreamWrapper(fromStream), toFilePath, null, ChannelSftp.OVERWRITE);
            }
            else
            {
                m_monitor.init(SftpProgressMonitor.PUT, "*Stream*", toFilePath, lngMax);

                SftpChannel.put(new InputStreamWrapper(fromStream), toFilePath, m_monitor, ChannelSftp.OVERWRITE);
            }
        }

        /// <summary>
        /// Put binary data to remote file
        /// </summary>
        /// <param name="fromData"></param>
        /// <param name="toFilePath"></param>
        /// <remarks>Added by Holger Boskugel; opensource@vbwebprofi.de; Berlin, Germany</remarks>
        public void Put(byte[] fromData, string toFilePath)
        {
            m_monitor.init(SftpProgressMonitor.PUT, "*BinaryData*", toFilePath, fromData.LongLength);

            SftpChannel.put(new InputStreamWrapper(new MemoryStream(fromData)), toFilePath, m_monitor, ChannelSftp.OVERWRITE);
        }

        //MkDir

        public override void Mkdir(string directory)
        {
            SftpChannel.mkdir(directory);
        }

        //Ls

        public IEnumerable<ChannelSftp.LsEntry> GetFileList(string path)
        {
            foreach (ChannelSftp.LsEntry entry in SftpChannel.ls(path))
            {
                // #8 - don't list directories when running a get file list
                if (!entry.Attributes.isDir())
                {
                    yield return entry;
                }
            }
        }

        public IEnumerable<ChannelSftp.LsEntry> GetDirectoryList(string path)
        {
            foreach (ChannelSftp.LsEntry entry in SftpChannel.ls(path))
            {
                if (entry.Attributes.isDir())
                {
                    yield return entry;
                }
            }
        }

        //Rm

        public void DeleteFile(string path)
        {
            SftpChannel.rm(path);
        }

        public void RenameFile(String oldPath, String newPath)
        {
            SftpChannel.rename(oldPath, newPath);
        }

        #region ProgressMonitor Implementation

        private class MyProgressMonitor : SftpProgressMonitor
        {
            private long transferred = 0;
            private long total = 0;
            private int elapsed = -1;
            private Sftp m_sftp;
            private string src;
            private string dest;

            private Timer timer;

            public MyProgressMonitor(Sftp sftp)
            {
                m_sftp = sftp;
            }

            public override void init(int op, String src, String dest, long max)
            {
                this.src = src;
                this.dest = dest;
                this.elapsed = 0;
                this.total = max;
                timer = new Timer(1000);
                timer.Start();
                timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);

                string note;
                if (op.Equals(GET))
                {
                    note = "Downloading " + Path.GetFileName(src) + "...";
                }
                else
                {
                    note = "Uploading " + Path.GetFileName(src) + "...";
                }
                m_sftp.SendStartMessage(src, dest, (int) total, note);
            }

            public override bool count(long c)
            {
                this.transferred += c;
                string note = ("Transfering... [Elapsed time: " + elapsed + "]");
                m_sftp.SendProgressMessage(src, dest, (int) transferred, (int) total, note);
                return !m_sftp.cancelled;
            }

            public override void end()
            {
                timer.Stop();
                timer.Dispose();
                string note = ("Done in " + elapsed + " seconds!");
                m_sftp.SendEndMessage(src, dest, (int) transferred, (int) total, note);
                transferred = 0;
                total = 0;
                elapsed = -1;
                src = null;
                dest = null;
            }

            private void timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                this.elapsed++;
            }
        }

        #endregion ProgressMonitor Implementation
    }
}