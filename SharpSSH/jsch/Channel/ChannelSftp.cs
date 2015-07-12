using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Tamir.SharpSsh.java;
using Tamir.SharpSsh.java.io;
using Tamir.SharpSsh.java.lang;
using Tamir.Streams;
using File = Tamir.SharpSsh.java.io.File;

namespace Tamir.SharpSsh.jsch
{
    /* -*-mode:java; c-basic-offset:2; indent-tabs-mode:nil -*- */
    /*
    Copyright (c) 2002,2003,2004,2005,2006 ymnk, JCraft,Inc. All rights reserved.

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

    /// <summary>
    /// Based on JSch-0.1.30
    /// </summary>
    public class ChannelSftp : ChannelSession
    {
        private static byte SSH_FXP_INIT = 1;
        // private static byte SSH_FXP_VERSION = 2;
        private static byte SSH_FXP_OPEN = 3;
        private static byte SSH_FXP_CLOSE = 4;
        private static byte SSH_FXP_READ = 5;
        private static byte SSH_FXP_WRITE = 6;
        private static byte SSH_FXP_LSTAT = 7;
        private static byte SSH_FXP_FSTAT = 8;
        private static byte SSH_FXP_SETSTAT = 9;
        private static byte SSH_FXP_FSETSTAT = 10;
        private static byte SSH_FXP_OPENDIR = 11;
        private static byte SSH_FXP_READDIR = 12;
        private static byte SSH_FXP_REMOVE = 13;
        private static byte SSH_FXP_MKDIR = 14;
        private static byte SSH_FXP_RMDIR = 15;
        private static byte SSH_FXP_REALPATH = 16;
        private static byte SSH_FXP_STAT = 17;
        private static byte SSH_FXP_RENAME = 18;
        private static byte SSH_FXP_READLINK = 19;
        private static byte SSH_FXP_SYMLINK = 20;
        private static byte SSH_FXP_STATUS = 101;
        private static byte SSH_FXP_HANDLE = 102;
        private static byte SSH_FXP_DATA = 103;
        private static byte SSH_FXP_NAME = 104;
        private static byte SSH_FXP_ATTRS = 105;
        private static byte SSH_FXP_EXTENDED = (byte) 200;
        private static byte SSH_FXP_EXTENDED_REPLY = (byte) 201;

        // pflags
        private static int SSH_FXF_READ = 0x00000001;
        private static int SSH_FXF_WRITE = 0x00000002;
        private static int SSH_FXF_APPEND = 0x00000004;
        private static int SSH_FXF_CREAT = 0x00000008;
        private static int SSH_FXF_TRUNC = 0x00000010;
        private static int SSH_FXF_EXCL = 0x00000020;

        private static int SSH_FILEXFER_ATTR_SIZE = 0x00000001;
        private static int SSH_FILEXFER_ATTR_UIDGID = 0x00000002;
        private static int SSH_FILEXFER_ATTR_PERMISSIONS = 0x00000004;
        private static int SSH_FILEXFER_ATTR_ACMODTIME = 0x00000008;
        private static uint SSH_FILEXFER_ATTR_EXTENDED = 0x80000000;

        public static int SSH_FX_OK = 0;
        public static int SSH_FX_EOF = 1;
        public static int SSH_FX_NO_SUCH_FILE = 2;
        public static int SSH_FX_PERMISSION_DENIED = 3;
        public static int SSH_FX_FAILURE = 4;
        public static int SSH_FX_BAD_MESSAGE = 5;
        public static int SSH_FX_NO_CONNECTION = 6;
        public static int SSH_FX_CONNECTION_LOST = 7;
        public static int SSH_FX_OP_UNSUPPORTED = 8;
        /*
        SSH_FX_OK
        Indicates successful completion of the operation.
        SSH_FX_EOF
        indicates end-of-file condition; for SSH_FX_READ it means that no
        more data is available in the file, and for SSH_FX_READDIR it
        indicates that no more files are contained in the directory.
        SSH_FX_NO_SUCH_FILE
        is returned when a reference is made to a file which should exist
        but doesn't.
        SSH_FX_PERMISSION_DENIED
        is returned when the authenticated user does not have sufficient
        permissions to perform the operation.
        SSH_FX_FAILURE
        is a generic catch-all error message; it should be returned if an
        error occurs for which there is no more specific error code
        defined.
        SSH_FX_BAD_MESSAGE
        may be returned if a badly formatted packet or protocol
        incompatibility is detected.
        SSH_FX_NO_CONNECTION
        is a pseudo-error which indicates that the client has no
        connection to the server (it can only be generated locally by the
        client, and MUST NOT be returned by servers).
        SSH_FX_CONNECTION_LOST
        is a pseudo-error which indicates that the connection to the
        server has been lost (it can only be generated locally by the
        client, and MUST NOT be returned by servers).
        SSH_FX_OP_UNSUPPORTED
        indicates that an attempt was made to perform an operation which
        is not supported for the server (it may be generated locally by
        the client if e.g.  the version number exchange indicates that a
        required feature is not supported by the server, or it may be
        returned by the server if the server does not implement an
        operation).
        */
        private static int MAX_MSG_LENGTH = 256 * 1024;

        public const int OVERWRITE = 0;
        public const int RESUME = 1;
        public const int APPEND = 2;

        internal int seq = 1;
        private int[] ackid = new int[1];
        private Buffer buf;
        private Packet packet;

        private String _version = "3";
        private int server_version = 3;

        /*
        10. Changes from previous protocol versions
        The SSH File Transfer Protocol has changed over time, before it's
        standardization.  The following is a description of the incompatible
        changes between different versions.
        10.1 Changes between versions 3 and 2
        o  The SSH_FXP_READLINK and SSH_FXP_SYMLINK messages were added.
        o  The SSH_FXP_EXTENDED and SSH_FXP_EXTENDED_REPLY messages were added.
        o  The SSH_FXP_STATUS message was changed to include fields `error
        message' and `language tag'.
        10.2 Changes between versions 2 and 1
        o  The SSH_FXP_RENAME message was added.
        10.3 Changes between versions 1 and 0
        o  Implementation changes, no actual protocol changes.
        */

        private String file_separator = Path.DirectorySeparatorChar.ToString();
        private char file_separatorc = Path.DirectorySeparatorChar;

        private String cwd;
        private String home;
        private String lcwd;

        internal ChannelSftp()
        {
            packet = new Packet(buf);
        }

        public override void init()
        {
        }

        public override void start()
        {
            try
            {
                PipedOutputStream pos = new PipedOutputStream();

                io.setOutputStream(pos);

                PipedInputStream pis = new MyPipedInputStream(pos, 32 * 1024);

                io.setInputStream(pis);

                Request request = new RequestSftp();
                request.request(session, this);

                buf = new Buffer(rmpsize);
                packet = new Packet(buf);
                int i = 0;
                int length;
                int type;
                byte[] str;

                // send SSH_FXP_INIT
                sendINIT();

                // receive SSH_FXP_VERSION
                Header _header = new Header();
                _header = header(buf, _header);
                length = _header.length;

                if (length > MAX_MSG_LENGTH)
                {
                    throw new SftpException(SSH_FX_FAILURE, "Received message is too long: " + length);
                }

                type = _header.type; // 2 -> SSH_FXP_VERSION
                server_version = _header.rid;
                skip(length);

                // send SSH_FXP_REALPATH
                sendREALPATH(".".GetBytes());

                // receive SSH_FXP_NAME
                _header = header(buf, _header);
                length = _header.length;
                type = _header.type; // 104 -> SSH_FXP_NAME
                buf.rewind();
                fill(buf.buffer, 0, length);
                i = buf.getInt(); // count
                str = buf.getString(); // filename
                home = cwd = new JavaString(str);
                str = buf.getString(); // logname

                lcwd = new File(".").getCanonicalPath();
            }
            catch (Exception e)
            {
                if (e is JSchException) throw (JSchException)e;
                throw new JSchException(e);
            }
        }

        public void lcd(String path)
        {
            path = localAbsolutePath(path);
            if ((new File(path)).isDirectory())
            {
                try
                {
                    path = (new File(path)).getCanonicalPath();
                }
                catch (Exception e)
                {
                }
                lcwd = path;
                return;
            }
            throw new SftpException(SSH_FX_NO_SUCH_FILE, "No such directory");
        }

        /*
        cd /tmp
        c->s REALPATH
        s->c NAME
        c->s STAT
        s->c ATTR
        */

        public void cd(String path)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                if (v.Count != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                path = (String)(v[0]);
                sendREALPATH(path.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != 101 && type != 104)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                int i;
                if (type == 101)
                {
                    i = buf.getInt();
                    throwStatusError(buf, i);
                }
                i = buf.getInt();
                byte[] str = buf.getString();

                if (str != null && str[0] != '/')
                {
                    str = (cwd + "/" + new JavaString(str)).GetBytes();
                }
                str = buf.getString(); // logname
                i = buf.getInt(); // attrs

                String newpwd = new JavaString(str);
                SftpATTRS attr = GetPathAttributes(newpwd);

                if ((attr.getFlags() & SftpATTRS.SSH_FILEXFER_ATTR_PERMISSIONS) == 0)
                {
                    throw new SftpException(SSH_FX_FAILURE, "Can't change directory: " + path);
                }

                if (!attr.isDir())
                {
                    throw new SftpException(SSH_FX_FAILURE, "Can't change directory: " + path);
                }

                cwd = newpwd;
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        /*
        put foo
        c->s OPEN
        s->c HANDLE
        c->s WRITE
        s->c STATUS
        c->s CLOSE
        s->c STATUS
        */

        public void put(String src, String dst)
        {
            put(src, dst, null, OVERWRITE);
        }

        public void put(String src, String dst, int mode)
        {
            put(src, dst, null, mode);
        }

        public void put(String src, String dst, SftpProgressMonitor monitor)
        {
            put(src, dst, monitor, OVERWRITE);
        }

        public void put(String src, String dst, SftpProgressMonitor monitor, int mode)
        {
            src = localAbsolutePath(src);
            dst = remoteAbsolutePath(dst);

            try
            {
                ArrayList v = glob_remote(dst);
                int vsize = v.Count;
                if (vsize != 1)
                {
                    if (vsize == 0)
                    {
                        if (isPattern(dst))
                            throw new SftpException(SSH_FX_FAILURE, dst);
                        else
                            dst = Util.unquote(dst);
                    }
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                else
                {
                    dst = (String)(v[0]);
                }

                bool _isRemoteDir = isRemoteDir(dst);

                v = glob_local(src);
                vsize = v.Count;

                StringBuilder dstsb = null;
                if (_isRemoteDir)
                {
                    if (!dst.EndsWith("/"))
                    {
                        dst += "/";
                    }

                    dstsb = new StringBuilder(dst);
                }
                else if (vsize > 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, "Copying multiple files, but destination is missing or a file.");
                }

                for (int j = 0; j < vsize; j++)
                {
                    String _src = (String)(v[j]);
                    String _dst = null;
                    if (_isRemoteDir)
                    {
                        int i = _src.LastIndexOf(file_separatorc);
                        if (i == -1) dstsb.Append(_src);
                        else dstsb.Append(_src.Substring(i + 1));
                        _dst = dstsb.ToString();
                        dstsb.Remove(dst.Length, _dst.Length);
                    }
                    else
                    {
                        _dst = dst;
                    }

                    long size_of_dst = 0;
                    if (mode == RESUME)
                    {
                        try
                        {
                            SftpATTRS attr = GetPathAttributes(_dst);
                            size_of_dst = attr.getSize();
                        }
                        catch (Exception eee)
                        {
                        }

                        long size_of_src = new File(_src).Length();
                        if (size_of_src < size_of_dst)
                        {
                            throw new SftpException(SSH_FX_FAILURE, "failed to resume for " + _dst);
                        }
                        if (size_of_src == size_of_dst)
                        {
                            return;
                        }
                    }

                    if (monitor != null)
                    {
                        monitor.init(SftpProgressMonitor.PUT, _src, _dst, (new File(_src)).Length());
                        if (mode == RESUME)
                        {
                            monitor.count(size_of_dst);
                        }
                    }
                    FileInputStream fis = null;
                    try
                    {
                        fis = new FileInputStream(_src);
                        _put(fis, _dst, monitor, mode);
                    }
                    finally
                    {
                        if (fis != null)
                        {
                            fis.close();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, e.Message);
            }
        }

        public void put(InputStream src, String dst)
        {
            put(src, dst, null, OVERWRITE);
        }

        public void put(InputStream src, String dst, int mode)
        {
            put(src, dst, null, mode);
        }

        public void put(InputStream src, String dst, SftpProgressMonitor monitor)
        {
            put(src, dst, monitor, OVERWRITE);
        }

        public void put(InputStream src, String dst, SftpProgressMonitor monitor, int mode)
        {
            try
            {
                dst = remoteAbsolutePath(dst);
                ArrayList v = glob_remote(dst);
                int vsize = v.Count;

                if (vsize != 1)
                {
                    if (vsize == 0)
                    {
                        if (isPattern(dst))
                            throw new SftpException(SSH_FX_FAILURE, dst);
                        else
                            dst = Util.unquote(dst);
                    }
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                else
                {
                    dst = (String)(v[0]);
                }
                if (isRemoteDir(dst))
                {
                    throw new SftpException(SSH_FX_FAILURE, dst + " is a directory");
                }
                _put(src, dst, monitor, mode);
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, e.Message);
            }
        }

        private void _put(InputStream src, String dst, SftpProgressMonitor monitor, int mode)
        {
            try
            {
                long skip = 0;
                if (mode == RESUME || mode == APPEND)
                {
                    try
                    {
                        SftpATTRS attr = GetPathAttributes(dst);
                        skip = attr.getSize();
                    }
                    catch (Exception eee)
                    {
                    }
                }
                if (mode == RESUME && skip > 0)
                {
                    long skipped = src.skip(skip);
                    if (skipped < skip)
                    {
                        throw new SftpException(SSH_FX_FAILURE, "failed to resume for " + dst);
                    }
                }
                if (mode == OVERWRITE)
                {
                    sendOPENW(dst.GetBytes());
                }
                else
                {
                    sendOPENA(dst.GetBytes());
                }

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
                {
                    throw new SftpException(SSH_FX_FAILURE, "invalid type=" + type);
                }
                if (type == SSH_FXP_STATUS)
                {
                    int i = buf.getInt();
                    throwStatusError(buf, i);
                }
                byte[] handle = buf.getString(); // filename
                byte[] data = null;

                bool dontcopy = true;

                if (!dontcopy)
                {
                    data = new byte[buf.buffer.Length
                                    - (5 + 13 + 21 + handle.Length
                                       + 32 + 20 // padding and mac
                                      )
                        ];
                }

                long offset = 0;
                if (mode == RESUME || mode == APPEND)
                {
                    offset += skip;
                }

                int startid = seq;
                int _ackid = seq;
                int ackcount = 0;
                while (true)
                {
                    int nread = 0;
                    int s = 0;
                    int datalen = 0;
                    int count = 0;

                    if (!dontcopy)
                    {
                        datalen = data.Length - s;
                    }
                    else
                    {
                        data = buf.buffer;
                        s = 5 + 13 + 21 + handle.Length;
                        datalen = buf.buffer.Length - s
                                  - 32 - 20; // padding and mac
                    }

                    do
                    {
                        nread = src.Read(data, s, datalen);
                        if (nread > 0)
                        {
                            s += nread;
                            datalen -= nread;
                            count += nread;
                        }
                    } while (datalen > 0 && nread > 0);
                    if (count <= 0) break;

                    int _i = count;
                    while (_i > 0)
                    {
                        _i -= sendWRITE(handle, offset, data, 0, _i);
                        if ((seq - 1) == startid ||
                            io.ins.available() >= 1024)
                        {
                            while (io.ins.available() > 0)
                            {
                                if (checkStatus(ackid, _header))
                                {
                                    _ackid = ackid[0];
                                    if (startid > _ackid || _ackid > seq - 1)
                                    {
                                        if (_ackid == seq)
                                        {
                                            Console.WriteLine("ack error: startid=" + startid + " seq=" + seq + " _ackid=" + _ackid);
                                        }
                                        else
                                        {
                                            throw new SftpException(SSH_FX_FAILURE, "ack error: startid=" + startid + " seq=" + seq + " _ackid=" + _ackid);
                                        }
                                    }
                                    ackcount++;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    offset += count;
                    if (monitor != null && !monitor.count(count))
                    {
                        break;
                    }
                }
                int _ackcount = seq - startid;
                while (_ackcount > ackcount)
                {
                    if (!checkStatus(null, _header))
                    {
                        break;
                    }
                    ackcount++;
                }
                if (monitor != null) monitor.end();
                _sendCLOSE(handle, _header);
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException) e;
                throw new SftpException(SSH_FX_FAILURE, e.Message);
            }
        }

        private SftpATTRS GetPathAttributes(String path)
        {
            try
            {
                sendSTAT(path.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_ATTRS)
                {
                    if (type == SSH_FXP_STATUS)
                    {
                        int i = buf.getInt();
                        throwStatusError(buf, i);
                    }
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                SftpATTRS attr = SftpATTRS.getATTR(buf);
                return attr;
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException) e;
                throw new SftpException(SSH_FX_FAILURE, e.Message);
            }
        }

        public OutputStream put(String dst)
        {
            return put(dst, (SftpProgressMonitor)null, OVERWRITE);
        }

        public OutputStream put(String dst, int mode)
        {
            return put(dst, (SftpProgressMonitor)null, mode);
        }

        public OutputStream put(String dst, SftpProgressMonitor monitor, int mode)
        {
            return put(dst, monitor, mode, 0);
        }

        public OutputStream put(String dst, SftpProgressMonitor monitor, int mode, long offset)
        {
            dst = remoteAbsolutePath(dst);
            try
            {
                ArrayList v = glob_remote(dst);
                if (v.Count != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                dst = (String)(v[0]);
                if (isRemoteDir(dst))
                {
                    throw new SftpException(SSH_FX_FAILURE, dst + " is a directory");
                }

                long skip = 0;
                if (mode == RESUME || mode == APPEND)
                {
                    try
                    {
                        SftpATTRS attr = stat(dst);
                        skip = attr.getSize();
                    }
                    catch (Exception eee)
                    {
                    }
                }

                if (mode == OVERWRITE)
                {
                    sendOPENW(dst.GetBytes());
                }
                else
                {
                    sendOPENA(dst.GetBytes());
                }

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;

                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                if (type == SSH_FXP_STATUS)
                {
                    int i = buf.getInt();
                    throwStatusError(buf, i);
                }

                byte[] handle = buf.getString(); // filename

                //long offset=0;
                if (mode == RESUME || mode == APPEND)
                {
                    offset += skip;
                }

                long[] _offset = new long[1];
                _offset[0] = offset;
                OutputStream outs = new OutputStreamPut(this, handle, _offset, monitor);

                return outs;
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException) e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void get(String remoteAbsolutePath, String localAbsolutePath)
        {
            get(remoteAbsolutePath, localAbsolutePath, null, OVERWRITE);
        }

        public void get(String remoteAbsolutePath, String localAbsolutePath, SftpProgressMonitor monitor)
        {
            get(remoteAbsolutePath, localAbsolutePath, monitor, OVERWRITE);
        }

        public void get(String remoteAbsolutePath, String localAbsolutePath, SftpProgressMonitor monitor, int mode)
        {
            remoteAbsolutePath = this.remoteAbsolutePath(remoteAbsolutePath);
            localAbsolutePath = this.localAbsolutePath(localAbsolutePath);

            try
            {
                ArrayList files = glob_remote(remoteAbsolutePath);

                if (files.Count == 0)
                {
                    throw new SftpException(SSH_FX_NO_SUCH_FILE, "No such file");
                }

                bool copyingToDirectory = new File(localAbsolutePath).isDirectory();

                // if we're not copying to a directory but we're copying multiple files, there's a problem
                if (false == copyingToDirectory && files.Count > 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, "Copying multiple files, but destination is missing or is a file.");
                }

                // if the given local path doesn't end with a '\' or other file separator, add one
                if (!localAbsolutePath.EndsWith(file_separator))
                {
                    localAbsolutePath += file_separator;
                }

                for (int j = 0; j < files.Count; j++)
                {
                    String sourceFile = (String) (files[j]);

                    SftpATTRS attr = GetPathAttributes(sourceFile);

                    // get information on the current file
                    if (attr.isDir())
                    {
                        // right now it's not able to get a directory
                        throw new SftpException(SSH_FX_FAILURE, "not supported to get directory " + sourceFile);
                    }

                    String destinationPath = null;

                    if (copyingToDirectory)
                    {
                        StringBuilder destinationSb = new StringBuilder(localAbsolutePath);

                        // find the last file separator character
                        int i = sourceFile.LastIndexOf(file_separatorc);

                        // basically we're appending just the filename
                        if (i == -1)
                            destinationSb.Append(sourceFile);
                        else
                            destinationSb.Append(sourceFile.Substring(i + 1));

                        destinationPath = destinationSb.ToString();
                    }
                    else
                    {
                        destinationPath = localAbsolutePath;
                    }

                    if (mode == RESUME)
                    {
                        long sizeOfSourceFile = attr.getSize();

                        long sizeOfDestinationFile = new File(destinationPath).Length();

                        // this means we already copied more data than is available. fail
                        if (sizeOfDestinationFile > sizeOfSourceFile)
                        {
                            throw new SftpException(SSH_FX_FAILURE, "failed to resume for " + destinationPath);
                        }

                        // if the sizes are equal, we're gravy
                        if (sizeOfDestinationFile == sizeOfSourceFile)
                        {
                            return;
                        }
                    }

                    if (monitor != null)
                    {
                        monitor.init(SftpProgressMonitor.GET, sourceFile, destinationPath, attr.getSize());

                        if (mode == RESUME)
                        {
                            monitor.count(new File(destinationPath).Length());
                        }
                    }

                    // create the output stream, and append if it's in overwrite mode
                    FileOutputStream fileOutputStream = new FileOutputStream(destinationPath, mode == OVERWRITE);

                    _get(sourceFile, fileOutputStream, monitor, mode, new File(destinationPath).Length());

                    fileOutputStream.Close();
                }
            }
            catch (SftpException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void get(String remoteAbsolutePath, OutputStream dst)
        {
            get(remoteAbsolutePath, dst, null, OVERWRITE, 0);
        }

        public void get(String remoteAbsolutePath, OutputStream dst, SftpProgressMonitor monitor)
        {
            get(remoteAbsolutePath, dst, monitor, OVERWRITE, 0);
        }

        public void get(String remoteAbsolutePath, OutputStream dst, SftpProgressMonitor monitor, int mode, long skip)
        {
            try
            {
                remoteAbsolutePath = this.remoteAbsolutePath(remoteAbsolutePath);
                ArrayList v = glob_remote(remoteAbsolutePath);

                if (v.Count != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }

                remoteAbsolutePath = (String) (v[0]);

                if (monitor != null)
                {
                    SftpATTRS attr = GetPathAttributes(remoteAbsolutePath);
                    monitor.init(SftpProgressMonitor.GET, remoteAbsolutePath, "??", attr.getSize());
                    if (mode == RESUME)
                    {
                        monitor.count(skip);
                    }
                }

                _get(remoteAbsolutePath, dst, monitor, mode, skip);
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException) e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        ///tamir: updated to jcsh-0.1.30
        private void _get(String src, OutputStream dst, SftpProgressMonitor monitor, int mode, long skip)
        {
            try
            {
                sendOPENR(src.GetBytes());


                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;

                buf.rewind();

                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
                {
                    throw new SftpException(SSH_FX_FAILURE, "Type is " + type);
                }

                if (type == SSH_FXP_STATUS)
                {
                    int i = buf.getInt();
                    throwStatusError(buf, i);
                }

                byte[] handle = buf.getString(); // filename

                long offset = 0;
                if (mode == RESUME)
                {
                    offset += skip;
                }

                int request_len = 0;

                while (true)
                {
                    request_len = buf.buffer.Length - 13;
                    if (server_version == 0)
                    {
                        request_len = 1024;
                    }
                    sendREAD(handle, offset, request_len);

                    _header = header(buf, _header);
                    length = _header.length;
                    type = _header.type;

                    int i;
                    if (type == SSH_FXP_STATUS)
                    {
                        buf.rewind();
                        fill(buf.buffer, 0, length);
                        i = buf.getInt();
                        if (i == SSH_FX_EOF)
                        {
                            goto BREAK;
                        }
                        throwStatusError(buf, i);
                    }

                    if (type != SSH_FXP_DATA)
                    {
                        goto BREAK;
                    }

                    buf.rewind();
                    fill(buf.buffer, 0, 4);
                    length -= 4;
                    i = buf.getInt(); // length of data 
                    int foo = i;
                    while (foo > 0)
                    {
                        int bar = foo;
                        if (bar > buf.buffer.Length)
                        {
                            bar = buf.buffer.Length;
                        }
                        i = io.ins.read(buf.buffer, 0, bar);
                        if (i < 0)
                        {
                            goto BREAK;
                        }
                        int data_len = i;
                        dst.Write(buf.buffer, 0, data_len);

                        offset += data_len;
                        foo -= data_len;

                        if (monitor != null)
                        {
                            if (!monitor.count(data_len))
                            {
                                while (foo > 0)
                                {
                                    i = io.ins.read(buf.buffer,
                                                    0,
                                                    (buf.buffer.Length < foo ? buf.buffer.Length : foo));
                                    if (i <= 0) break;
                                    foo -= i;
                                }
                                goto BREAK;
                            }
                        }
                    }
                }
                BREAK:
                dst.Flush();

                if (monitor != null) monitor.end();
                _sendCLOSE(handle, _header);
            }
            catch (Exception e)
            {
                //System.Console.WriteLine(e);
                if (e is SftpException) throw (SftpException) e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public InputStream get(String remoteAbsolutePath)
        {
            return get(remoteAbsolutePath, null, OVERWRITE);
        }

        public InputStream get(String remoteAbsolutePath, SftpProgressMonitor monitor)
        {
            return get(remoteAbsolutePath, monitor, OVERWRITE);
        }

        public InputStream get(String remoteAbsolutePath, int mode)
        {
            return get(remoteAbsolutePath, null, mode);
        }

        public InputStream get(String remoteAbsolutePath, SftpProgressMonitor monitor, int mode)
        {
            if (mode == RESUME)
            {
                throw new SftpException(SSH_FX_FAILURE, "faile to resume from " + remoteAbsolutePath);
            }
            remoteAbsolutePath = this.remoteAbsolutePath(remoteAbsolutePath);
            try
            {
                ArrayList v = glob_remote(remoteAbsolutePath);
                if (v.Count != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                remoteAbsolutePath = (String) (v[0]);

                SftpATTRS attr = GetPathAttributes(remoteAbsolutePath);
                if (monitor != null)
                {
                    monitor.init(SftpProgressMonitor.GET, remoteAbsolutePath, "??", attr.getSize());
                }

                sendOPENR(remoteAbsolutePath.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                if (type == SSH_FXP_STATUS)
                {
                    int i = buf.getInt();
                    throwStatusError(buf, i);
                }

                byte[] handle = buf.getString(); // filename

                InputStream ins = new InputStreamGet(this, handle, monitor);
                return ins;
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public ArrayList ls(String path)
        {
            path = remoteAbsolutePath(path);

            String dir = path;
            byte[] pattern = null;
            SftpATTRS attr = null;
            if (isPattern(dir) ||
                ((attr = stat(dir)) != null && !attr.isDir()))
            {
                int foo = path.LastIndexOf('/');
                dir = path.Substring(0, ((foo == 0) ? 1 : foo));
                pattern = path.Substring(foo + 1).GetBytes();
            }

            sendOPENDIR(dir.GetBytes());

            Header _header = new Header();
            _header = header(buf, _header);
            int length = _header.length;
            int type = _header.type;
            buf.rewind();
            fill(buf.buffer, 0, length);

            if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
            {
                throw new SftpException(SSH_FX_FAILURE, "");
            }
            if (type == SSH_FXP_STATUS)
            {
                int i = buf.getInt();
                throwStatusError(buf, i);
            }

            byte[] handle = buf.getString(); // filename

            ArrayList v = new ArrayList();
            while (true)
            {
                sendREADDIR(handle);

                _header = header(buf, _header);
                length = _header.length;
                type = _header.type;
                if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                if (type == SSH_FXP_STATUS)
                {
                    buf.rewind();
                    fill(buf.buffer, 0, length);
                    int i = buf.getInt();
                    if (i == SSH_FX_EOF)
                        break;
                    throwStatusError(buf, i);
                }

                buf.rewind();
                fill(buf.buffer, 0, 4);
                length -= 4;
                int count = buf.getInt();

                byte[] str;

                buf.reset();
                while (count > 0)
                {
                    if (length > 0)
                    {
                        buf.shift();
                        int j = (buf.buffer.Length > (buf.index + length)) ? length : (buf.buffer.Length - buf.index);
                        int i = fill(buf.buffer, buf.index, j);
                        buf.index += i;
                        length -= i;
                    }
                    byte[] filename = buf.getString();
                    str = buf.getString();
                    String longname = new JavaString(str);

                    SftpATTRS attrs = SftpATTRS.getATTR(buf);
                    if (pattern == null || Util.glob(pattern, filename))
                    {
                        v.Add(new LsEntry(new JavaString(filename), path, longname, attrs));
                    }

                    count--;
                }
            }
            _sendCLOSE(handle, _header);
            return v;
        }

        public String readlink(String path)
        {
            try
            {
                path = remoteAbsolutePath(path);
                ArrayList v = glob_remote(path);
                if (v.Count != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                path = (String)(v[0]);

                sendREADLINK(path.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                int i;
                if (type == SSH_FXP_NAME)
                {
                    int count = buf.getInt(); // count
                    byte[] filename = null;
                    byte[] longname = null;
                    for (i = 0; i < count; i++)
                    {
                        filename = buf.getString();
                        longname = buf.getString();
                        SftpATTRS.getATTR(buf);
                    }
                    return new JavaString(filename);
                }

                i = buf.getInt();
                throwStatusError(buf, i);
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
            return null;
        }


        public void symlink(String oldpath, String newpath)
        {
            if (server_version < 3)
            {
                throw new SftpException(SSH_FX_FAILURE, "The remote sshd is too old to support symlink operation.");
            }

            try
            {
                oldpath = remoteAbsolutePath(oldpath);
                newpath = remoteAbsolutePath(newpath);

                ArrayList v = glob_remote(oldpath);
                int vsize = v.Count;
                if (vsize != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                oldpath = (String)(v[0]);

                if (isPattern(newpath))
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }

                newpath = Util.unquote(newpath);

                sendSYMLINK(oldpath.GetBytes(), newpath.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }

                int i = buf.getInt();
                if (i == SSH_FX_OK) return;
                throwStatusError(buf, i);
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void rename(String oldpath, String newpath)
        {
            if (server_version < 2)
            {
                throw new SftpException(SSH_FX_FAILURE, "The remote sshd is too old to support rename operation.");
            }

            try
            {
                oldpath = remoteAbsolutePath(oldpath);
                newpath = remoteAbsolutePath(newpath);

                ArrayList v = glob_remote(oldpath);
                int vsize = v.Count;
                if (vsize != 1)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                oldpath = (String)(v[0]);

                v = glob_remote(newpath);
                vsize = v.Count;
                if (vsize >= 2)
                {
                    throw new SftpException(SSH_FX_FAILURE, v.ToString());
                }
                if (vsize == 1)
                {
                    newpath = (String)(v[0]);
                }
                else
                {
                    // vsize==0
                    if (isPattern(newpath))
                        throw new SftpException(SSH_FX_FAILURE, newpath);
                    newpath = Util.unquote(newpath);
                }

                sendRENAME(oldpath.GetBytes(), newpath.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }

                int i = buf.getInt();
                if (i == SSH_FX_OK) return;
                throwStatusError(buf, i);
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void rm(String path)
        {
            try
            {
                path = remoteAbsolutePath(path);
                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                Header _header = new Header();

                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);
                    sendREMOVE(path.GetBytes());

                    _header = header(buf, _header);
                    int length = _header.length;
                    int type = _header.type;
                    buf.rewind();
                    fill(buf.buffer, 0, length);

                    if (type != SSH_FXP_STATUS)
                    {
                        throw new SftpException(SSH_FX_FAILURE, "");
                    }
                    int i = buf.getInt();
                    if (i != SSH_FX_OK)
                    {
                        throwStatusError(buf, i);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        private bool isRemoteDir(String path)
        {
            try
            {
                sendSTAT(path.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_ATTRS)
                {
                    return false;
                }
                SftpATTRS attr = SftpATTRS.getATTR(buf);
                return attr.isDir();
            }
            catch (Exception e)
            {
            }
            return false;
        }

        public void chgrp(int gid, String path)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);

                    SftpATTRS attr = GetPathAttributes(path);

                    attr.setFLAGS(0);
                    attr.setUIDGID(attr.uid, gid);
                    _setStat(path, attr);
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void chown(int uid, String path)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);

                    SftpATTRS attr = GetPathAttributes(path);

                    attr.setFLAGS(0);
                    attr.setUIDGID(uid, attr.gid);
                    _setStat(path, attr);
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void chmod(int permissions, String path)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);

                    SftpATTRS attr = GetPathAttributes(path);

                    attr.setFLAGS(0);
                    attr.setPERMISSIONS(permissions);
                    _setStat(path, attr);
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void setMtime(String path, int mtime)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);

                    SftpATTRS attr = GetPathAttributes(path);

                    attr.setFLAGS(0);
                    attr.setACMODTIME(attr.getATime(), mtime);
                    _setStat(path, attr);
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void rmdir(String path)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                Header _header = new Header();

                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);
                    sendRMDIR(path.GetBytes());

                    _header = header(buf, _header);
                    int length = _header.length;
                    int type = _header.type;
                    buf.rewind();
                    fill(buf.buffer, 0, length);

                    if (type != SSH_FXP_STATUS)
                    {
                        throw new SftpException(SSH_FX_FAILURE, "");
                    }

                    int i = buf.getInt();
                    if (i != SSH_FX_OK)
                    {
                        throwStatusError(buf, i);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public void mkdir(String path)
        {
            path = remoteAbsolutePath(path);

            sendMKDIR(path.GetBytes(), null);

            Header _header = new Header();
            _header = header(buf, _header);
            int length = _header.length;
            int type = _header.type;
            buf.rewind();
            fill(buf.buffer, 0, length);

            if (type != SSH_FXP_STATUS)
            {
                throw new SftpException(SSH_FX_FAILURE, "");
            }

            int i = buf.getInt();
            if (i == SSH_FX_OK) return;
            throwStatusError(buf, i);
        }

        public SftpATTRS stat(String path)
        {
            path = remoteAbsolutePath(path);

            ArrayList v = glob_remote(path);
            if (v.Count != 1)
            {
                throw new SftpException(SSH_FX_FAILURE, v.ToString());
            }
            path = (String)(v[0]);
            return GetPathAttributes(path);
        }

        public SftpATTRS lstat(String path)
        {
            path = remoteAbsolutePath(path);

            ArrayList v = glob_remote(path);
            if (v.Count != 1)
            {
                throw new SftpException(SSH_FX_FAILURE, v.ToString());
            }
            path = (String)(v[0]);

            return _lstat(path);
        }

        private SftpATTRS _lstat(String path)
        {
            try
            {
                sendLSTAT(path.GetBytes());

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_ATTRS)
                {
                    if (type == SSH_FXP_STATUS)
                    {
                        int i = buf.getInt();
                        throwStatusError(buf, i);
                    }
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                SftpATTRS attr = SftpATTRS.getATTR(buf);
                return attr;
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }


        public void setStat(String path, SftpATTRS attr)
        {
            try
            {
                path = remoteAbsolutePath(path);

                ArrayList v = glob_remote(path);
                int vsize = v.Count;
                for (int j = 0; j < vsize; j++)
                {
                    path = (String)(v[j]);
                    _setStat(path, attr);
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        private void _setStat(String path, SftpATTRS attr)
        {
            try
            {
                sendSETSTAT(path.GetBytes(), attr);

                Header _header = new Header();
                _header = header(buf, _header);
                int length = _header.length;
                int type = _header.type;
                buf.rewind();
                fill(buf.buffer, 0, length);

                if (type != SSH_FXP_STATUS)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                int i = buf.getInt();
                if (i != SSH_FX_OK)
                {
                    throwStatusError(buf, i);
                }
            }
            catch (Exception e)
            {
                if (e is SftpException) throw (SftpException)e;
                throw new SftpException(SSH_FX_FAILURE, "");
            }
        }

        public String pwd()
        {
            return cwd;
        }

        public String lpwd()
        {
            return lcwd;
        }

        public String version()
        {
            return _version;
        }

        public String getHome()
        {
            return home;
        }

        private void read(byte[] buf, int s, int l)
        {
            //throws IOException, SftpException{
            int i = 0;
            while (l > 0)
            {
                i = io.ins.read(buf, s, l);
                if (i <= 0)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                s += i;
                l -= i;
            }
        }

        internal bool checkStatus(int[] ackid, Header _header)
        {
            //throws IOException, SftpException{
            _header = header(buf, _header);
            int length = _header.length;
            int type = _header.type;
            if (ackid != null)
                ackid[0] = _header.rid;
            buf.rewind();
            fill(buf.buffer, 0, length);

            if (type != SSH_FXP_STATUS)
            {
                throw new SftpException(SSH_FX_FAILURE, "");
            }
            int i = buf.getInt();
            if (i != SSH_FX_OK)
            {
                throwStatusError(buf, i);
            }
            return true;
        }

        internal bool _sendCLOSE(byte[] handle, Header header)
        //throws Exception
        {
            sendCLOSE(handle);
            return checkStatus(null, header);
        }

        private void sendINIT()
        {
            packet.reset();
            putHEAD(SSH_FXP_INIT, 5);
            buf.putInt(3); // version 3
            session.write(packet, this, 5 + 4);
        }

        private void sendREALPATH(byte[] path)
        {
            sendPacketPath(SSH_FXP_REALPATH, path);
        }

        private void sendSTAT(byte[] path)
        {
            sendPacketPath(SSH_FXP_STAT, path);
        }

        private void sendLSTAT(byte[] path)
        {
            sendPacketPath(SSH_FXP_LSTAT, path);
        }

        private void sendFSTAT(byte[] handle)
        {
            sendPacketPath(SSH_FXP_FSTAT, handle);
        }

        private void sendSETSTAT(byte[] path, SftpATTRS attr)
        {
            packet.reset();
            putHEAD(SSH_FXP_SETSTAT, 9 + path.Length + attr.Length());
            buf.putInt(seq++);
            buf.putString(path); // path
            attr.dump(buf);
            session.write(packet, this, 9 + path.Length + attr.Length() + 4);
        }

        private void sendREMOVE(byte[] path)
        {
            sendPacketPath(SSH_FXP_REMOVE, path);
        }

        private void sendMKDIR(byte[] path, SftpATTRS attr)
        {
            packet.reset();
            putHEAD(SSH_FXP_MKDIR, 9 + path.Length + (attr != null ? attr.Length() : 4));
            buf.putInt(seq++);
            buf.putString(path); // path
            if (attr != null) attr.dump(buf);
            else buf.putInt(0);
            session.write(packet, this, 9 + path.Length + (attr != null ? attr.Length() : 4) + 4);
        }

        private void sendRMDIR(byte[] path)
        {
            sendPacketPath(SSH_FXP_RMDIR, path);
        }

        private void sendSYMLINK(byte[] p1, byte[] p2)
        {
            sendPacketPath(SSH_FXP_SYMLINK, p1, p2);
        }

        private void sendREADLINK(byte[] path)
        {
            sendPacketPath(SSH_FXP_READLINK, path);
        }

        private void sendOPENDIR(byte[] path)
        {
            sendPacketPath(SSH_FXP_OPENDIR, path);
        }

        private void sendREADDIR(byte[] path)
        {
            sendPacketPath(SSH_FXP_READDIR, path);
        }

        private void sendRENAME(byte[] p1, byte[] p2)
        {
            sendPacketPath(SSH_FXP_RENAME, p1, p2);
        }

        private void sendCLOSE(byte[] path)
        {
            sendPacketPath(SSH_FXP_CLOSE, path);
        }

        private void sendOPENR(byte[] path)
        {
            sendOPEN(path, SSH_FXF_READ);
        }

        private void sendOPENW(byte[] path)
        {
            sendOPEN(path, SSH_FXF_WRITE | SSH_FXF_CREAT | SSH_FXF_TRUNC);
        }

        private void sendOPENA(byte[] path)
        {
            sendOPEN(path, SSH_FXF_WRITE | /*SSH_FXF_APPEND|*/ SSH_FXF_CREAT);
        }

        private void sendOPEN(byte[] path, int mode)
        {
            packet.reset();
            putHEAD(SSH_FXP_OPEN, 17 + path.Length);
            buf.putInt(seq++);
            buf.putString(path);
            buf.putInt(mode);
            buf.putInt(0); // attrs
            session.write(packet, this, 17 + path.Length + 4);
        }

        private void sendPacketPath(byte fxp, byte[] path)
        {
            packet.reset();
            putHEAD(fxp, 9 + path.Length);
            buf.putInt(seq++);
            buf.putString(path); // path
            session.write(packet, this, 9 + path.Length + 4);
        }

        private void sendPacketPath(byte fxp, byte[] p1, byte[] p2)
        {
            packet.reset();
            putHEAD(fxp, 13 + p1.Length + p2.Length);
            buf.putInt(seq++);
            buf.putString(p1);
            buf.putString(p2);
            session.write(packet, this, 13 + p1.Length + p2.Length + 4);
        }

        internal int sendWRITE(byte[] handle, long offset,
                               byte[] data, int start, int length)
        {
            int _length = length;
            packet.reset();
            if (buf.buffer.Length < buf.index + 13 + 21 + handle.Length + length
                + 32 + 20 // padding and mac
                )
            {
                _length = buf.buffer.Length - (buf.index + 13 + 21 + handle.Length
                                               + 32 + 20 // padding and mac
                                              );
                //System.err.println("_length="+_length+" length="+length);
            }
            putHEAD(SSH_FXP_WRITE, 21 + handle.Length + _length); // 14
            buf.putInt(seq++); //  4
            buf.putString(handle); //  4+handle.length
            buf.putLong(offset); //  8
            if (buf.buffer != data)
            {
                buf.putString(data, start, _length); //  4+_length
            }
            else
            {
                buf.putInt(_length);
                buf.skip(_length);
            }
            session.write(packet, this, 21 + handle.Length + _length + 4);
            return _length;
        }

        private void sendREAD(byte[] handle, long offset, int length)
        {
            packet.reset();
            putHEAD(SSH_FXP_READ, 21 + handle.Length);
            buf.putInt(seq++);
            buf.putString(handle);
            buf.putLong(offset);
            buf.putInt(length);
            session.write(packet, this, 21 + handle.Length + 4);
        }

        private void putHEAD(byte type, int length)
        {
            buf.putByte((byte)Session.SSH_MSG_CHANNEL_DATA);
            buf.putInt(recipient);
            buf.putInt(length + 4);
            buf.putInt(length);
            buf.putByte(type);
        }

        private ArrayList glob_remote(String _path)
        {
            ArrayList v = new ArrayList();
            byte[] path = _path.GetBytes();
            if (!isPattern(path))
            {
                v.Add(Util.unquote(_path));
                return v;
            }
            int i = path.Length - 1;
            while (i >= 0)
            {
                if (path[i] == '/') break;
                i--;
            }
            if (i < 0)
            {
                v.Add(Util.unquote(_path));
                return v;
            }
            byte[] dir;
            if (i == 0)
            {
                dir = new byte[] { (byte)'/' };
            }
            else
            {
                dir = new byte[i];
                Array.Copy(path, 0, dir, 0, i);
            }
            byte[] pattern = new byte[path.Length - i - 1];
            Array.Copy(path, i + 1, pattern, 0, pattern.Length);

            sendOPENDIR(dir);

            Header _header = new Header();
            _header = header(buf, _header);
            int length = _header.length;
            int type = _header.type;
            buf.rewind();
            fill(buf.buffer, 0, length);

            if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
            {
                throw new SftpException(SSH_FX_FAILURE, "");
            }
            if (type == SSH_FXP_STATUS)
            {
                i = buf.getInt();
                throwStatusError(buf, i);
            }

            byte[] handle = buf.getString(); // filename

            while (true)
            {
                sendREADDIR(handle);
                _header = header(buf, _header);
                length = _header.length;
                type = _header.type;

                if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
                {
                    throw new SftpException(SSH_FX_FAILURE, "");
                }
                if (type == SSH_FXP_STATUS)
                {
                    buf.rewind();
                    fill(buf.buffer, 0, length);
                    break;
                }

                buf.rewind();
                fill(buf.buffer, 0, 4);
                length -= 4;
                int count = buf.getInt();

                byte[] str;

                buf.reset();
                while (count > 0)
                {
                    if (length > 0)
                    {
                        buf.shift();
                        int j = (buf.buffer.Length > (buf.index + length)) ? length : (buf.buffer.Length - buf.index);
                        i = io.ins.read(buf.buffer, buf.index, j);
                        if (i <= 0) break;
                        buf.index += i;
                        length -= i;
                    }

                    byte[] filename = buf.getString();
                    //System.err.println("filename: "+new String(filename));
                    str = buf.getString();
                    SftpATTRS attrs = SftpATTRS.getATTR(buf);

                    if (Util.glob(pattern, filename))
                    {
                        v.Add(new JavaString(dir) + "/" + new JavaString(filename));
                    }
                    count--;
                }
            }
            if (_sendCLOSE(handle, _header))
                return v;
            return null;
        }

        private ArrayList glob_local(String _path)
        {
            ArrayList v = new ArrayList();
            byte[] path = _path.GetBytes();
            int i = path.Length - 1;
            while (i >= 0)
            {
                if (path[i] == '*' || path[i] == '?') break;
                i--;
            }
            if (i < 0)
            {
                v.Add(_path);
                return v;
            }
            while (i >= 0)
            {
                if (path[i] == file_separatorc) break;
                i--;
            }
            if (i < 0)
            {
                v.Add(_path);
                return v;
            }
            byte[] dir;
            if (i == 0)
            {
                dir = new byte[] { (byte)file_separatorc };
            }
            else
            {
                dir = new byte[i];
                Array.Copy(path, 0, dir, 0, i);
            }
            byte[] pattern = new byte[path.Length - i - 1];
            Array.Copy(path, i + 1, pattern, 0, pattern.Length);

            try
            {
                String[] children = (new File(Encoding.Default.GetString(dir))).list();
                for (int j = 0; j < children.Length; j++)
                {
                    if (Util.glob(pattern, children[j].GetBytes()))
                    {
                        v.Add(Encoding.Default.GetString(dir) + file_separator + children[j]);
                    }
                }
            }
            catch (Exception e)
            {
            }
            return v;
        }

        private void throwStatusError(Buffer buf, int i)
        {
            if (server_version >= 3)
            {
                byte[] str = buf.getString();

                throw new SftpException(i, new JavaString(str));
            }
            else
            {
                throw new SftpException(i, "Failure");
            }
        }

        private static bool isLocalAbsolutePath(String path)
        {
            return (new File(path)).isAbsolute();
        }

        public override void disconnect()
        {
            clearRunningThreads();
            base.disconnect();
        }

        private ArrayList threadList = null;

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void addRunningThread(JavaThread thread)
        {
            if (threadList == null) threadList = new ArrayList();
            threadList.Add(thread);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected void clearRunningThreads()
        {
            if (threadList == null) return;
            for (int t = 0; t < threadList.Count; t++)
            {
                JavaThread thread = (JavaThread)threadList[t];
                if (thread != null)
                    if (thread.IsAlive())
                        thread.Interrupt();
            }
            threadList.Clear();
        }

        private bool isPattern(String path)
        {
            return path.IndexOf("*") != -1 || path.IndexOf("?") != -1;
        }

        private bool isPattern(byte[] path)
        {
            return isPattern(new JavaString(path));
        }

        private int fill(byte[] buf, int s, int len)
        {
            int i = 0;
            int foo = s;
            while (len > 0)
            {
                i = io.ins.read(buf, s, len);
                if (i <= 0)
                {
                    throw new IOException("inputstream is closed");
                }
                s += i;
                len -= i;
            }
            return s - foo;
        }

        //tamir: some functions from jsch-0.1.30
        private void skip(long foo)
        {
            while (foo > 0)
            {
                long bar = io.ins.skip(foo);
                if (bar <= 0)
                    break;
                foo -= bar;
            }
        }

        internal class Header
        {
            public int length;
            public int type;
            public int rid;
        }

        internal Header header(Buffer buf, Header header)
        {
            buf.rewind();
            int i = fill(buf.buffer, 0, 9);
            header.length = buf.getInt() - 5;
            header.type = buf.getByte() & 0xff;
            header.rid = buf.getInt();
            return header;
        }

        private String remoteAbsolutePath(String path)
        {
            if (path[0] == '/') return path;

            if (cwd.EndsWith("/")) return cwd + path;

            return cwd + "/" + path;
        }

        private String localAbsolutePath(String path)
        {
            if (isLocalAbsolutePath(path)) return path;

            if (lcwd.EndsWith(file_separator)) return lcwd + path;

            return lcwd + file_separator + path;
        }

        public class LsEntry
        {
            public LsEntry(String filename, String path, String longname, SftpATTRS attrs)
            {
                Filename = filename;
                Path = path;
                Longname = longname;
                Attributes = attrs;
            }

            public String Filename { get; internal set; }
            public String Path { get; internal set; }
            public String Longname { get; internal set; }
            public SftpATTRS Attributes { get; internal set; }

            public String FullPath
            {
                get
                {
                    if (Path.EndsWith("/"))
                    {
                        return Path + Filename;
                    }

                    return Path + "/" + Filename;
                }
            }
        }

        public class InputStreamGet : InputStream
        {
            private ChannelSftp sftp;
            private SftpProgressMonitor monitor;
            private long offset = 0;
            private bool closed = false;
            private int rest_length = 0;
            private byte[] _data = new byte[1];
            private byte[] rest_byte = new byte[1024];
            private byte[] handle;
            private Header header = new Header();

            public InputStreamGet(ChannelSftp sftp, byte[] handle, SftpProgressMonitor monitor)
            {
                this.sftp = sftp;
                this.handle = handle;
                this.monitor = monitor;
            }

            public override int ReadByte()
            {
                if (closed) return -1;

                int i = Read(_data, 0, 1);

                if (i == -1)
                {
                    return -1;
                }
                else
                {
                    return _data[0] & 0xff;
                }
            }

            public int Read(byte[] d)
            {
                if (closed) return -1;
                return Read(d, 0, d.Length);
            }

            public override int Read(byte[] d, int s, int len)
            {
                if (closed) return -1;
                int i;
                int foo;

                if (d == null)
                {
                    throw new NullReferenceException();
                }

                if (s < 0 || len < 0 || s + len > d.Length)
                {
                    throw new IndexOutOfRangeException();
                }

                if (len == 0)
                {
                    return 0;
                }

                if (rest_length > 0)
                {
                    foo = rest_length;
                    if (foo > len) foo = len;
                    Array.Copy(rest_byte, 0, d, s, foo);

                    if (foo != rest_length)
                    {
                        Array.Copy(rest_byte, foo,
                                   rest_byte, 0, rest_length - foo);
                    }

                    if (monitor != null)
                    {
                        if (!monitor.count(foo))
                        {
                            close();
                            return -1;
                        }
                    }

                    rest_length -= foo;

                    return foo;
                }

                if (sftp.buf.buffer.Length - 13 < len)
                {
                    len = sftp.buf.buffer.Length - 13;
                }

                if (sftp.server_version == 0 && len > 1024)
                {
                    len = 1024;
                }

                try
                {
                    sftp.sendREAD(handle, offset, len);
                }
                catch (Exception e)
                {
                    throw new IOException("error");
                }

                header = sftp.header(sftp.buf, header);
                rest_length = header.length;
                int type = header.type;
                int id = header.rid;

                if (type != SSH_FXP_STATUS && type != SSH_FXP_DATA)
                {
                    throw new IOException("error");
                }

                if (type == SSH_FXP_STATUS)
                {
                    sftp.buf.rewind();
                    sftp.fill(sftp.buf.buffer, 0, rest_length);
                    i = sftp.buf.getInt();
                    rest_length = 0;

                    if (i == SSH_FX_EOF)
                    {
                        close();
                        return -1;
                    }

                    throw new IOException("error");
                }

                sftp.buf.rewind();
                sftp.fill(sftp.buf.buffer, 0, 4);
                i = sftp.buf.getInt();
                rest_length -= 4;

                offset += rest_length;
                foo = i;

                if (foo > 0)
                {
                    int bar = rest_length;

                    if (bar > len)
                    {
                        bar = len;
                    }

                    i = sftp.io.ins.read(d, s, bar);

                    if (i < 0)
                    {
                        return -1;
                    }

                    rest_length -= i;

                    if (rest_length > 0)
                    {
                        if (rest_byte.Length < rest_length)
                        {
                            rest_byte = new byte[rest_length];
                        }

                        int _s = 0;
                        int _len = rest_length;
                        int j;

                        while (_len > 0)
                        {
                            j = sftp.io.ins.read(rest_byte, _s, _len);
                            if (j <= 0) break;
                            _s += j;
                            _len -= j;
                        }
                    }

                    if (monitor != null)
                    {
                        if (!monitor.count(i))
                        {
                            close();
                            return -1;
                        }
                    }

                    return i;
                }
                return 0; // ??
            }

            public override void Close()
            {
                if (closed) return;

                closed = true;

                if (monitor != null) monitor.end();

                try
                {
                    sftp._sendCLOSE(handle, header);
                }
                catch (Exception e)
                {
                    throw new IOException("Error closing inputstream!", e);
                }
            }
        }
    }
}