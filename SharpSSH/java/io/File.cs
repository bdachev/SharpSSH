using System;
using System.IO;

namespace Tamir.SharpSsh.java.io
{
    public class File
    {
        private string file;
        internal FileInfo info;

        public File(string file)
        {
            this.file = file;
            info = new FileInfo(file);
        }

        public string getCanonicalPath()
        {
            return Path.GetFullPath(file);
        }

        public bool isDirectory()
        {
            return Directory.Exists(file);
        }

        public long Length()
        {
            return info.Length;
        }

        public bool isAbsolute()
        {
            return Path.IsPathRooted(file);
        }

        public String[] list()
        {
            string[] dirs = Directory.GetDirectories(file);
            string[] files = Directory.GetFiles(file);
            String[] _list = new String[dirs.Length + files.Length];
            Array.Copy(dirs, 0, _list, 0, dirs.Length);
            Array.Copy(files, 0, _list, dirs.Length, files.Length);
            return _list;
        }
    }
}