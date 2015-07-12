using System;

namespace Tamir.SharpSsh.jsch
{
    /// <summary>
    /// Summary description for JSchException.
    /// </summary>
    public class JSchException : Exception
    {
        public JSchException(Exception e = null) 
             : base(e == null ? null : e.Message, innerException: e)
        {
        }

        public JSchException(string msg, Exception inner = null) : base(msg, inner)
        {
        }
    }
}