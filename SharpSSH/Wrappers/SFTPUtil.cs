using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tamir.SharpSsh.jsch;

namespace Tamir.SharpSsh.Wrappers
{
    /// <summary>
    /// This is an ugly class that I'm using to wrap some basic functions
    /// </summary>
    public class SFTPUtil : IDisposable
    {
        private readonly String host;
        private readonly String pass;

        private readonly Sftp sftp;
        private readonly String user;

        public SFTPUtil(String host, String user, String pass)
        {
            this.host = host;
            this.user = user;
            this.pass = pass;

            sftp = new Sftp(host, user, pass);

            sftp.Connect();
        }

        public IProxy Proxy
        {
            get { return sftp.Proxy; }
            set { sftp.Proxy = value; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (sftp != null)
            {
                if (sftp.Connected)
                {
                    sftp.Close();
                }
            }
        }

        #endregion

        public IList<ChannelSftp.LsEntry> ListFiles(String remotePath)
        {
            return sftp.GetFileList(remotePath).ToList();
        }

        public void GetFile(String remotePath, String localPath)
        {
            sftp.Get(remotePath, localPath);
        }

        public void GetFile(String remotePath, Stream outputStream)
        {
            sftp.Get(remotePath, outputStream);
        }

        public void GetLotsOfFiles(String remotePath, String localPath)
        {
            foreach (ChannelSftp.LsEntry f in ListFiles(remotePath))
            {
                GetFile(f.FullPath, localPath);
            }
        }

        public void PutFile(String localPath, String remotePath)
        {
            sftp.Put(localPath, remotePath);
        }

        public void DeleteFile(String remotePath)
        {
            sftp.DeleteFile(remotePath);
        }

        public void RenameFile(String oldPath, String newPath)
        {
            sftp.RenameFile(oldPath, newPath);
        }

        public void ExecuteCommand(String command)
        {
            using (var ssh = new SshExec(host, user, pass))
            {
                ssh.Connect();

                ssh.RunCommand(command);
            }
        }
    }
}