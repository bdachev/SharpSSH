using System;

namespace Tamir.SharpSsh.jsch
{
    /// <summary>
    /// Summary description for JSchException.
    /// </summary>
    public class JSchException : java.Exception
    {
        public JSchException(Exception innerException = null) : base(innerException)
        {
        }

        public JSchException(string msg, Exception innerException = null) : base(msg, innerException)
        {
        }
    }
}
