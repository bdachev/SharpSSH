using Ex = System.Exception;

namespace Tamir.SharpSsh.java
{
    /// <summary>
    /// Summary description for Exception.
    /// </summary>
    public class JavaException : Ex
    {
        public JavaException(Ex innerException = null) : base(string.Empty, innerException)
        {
        }

        public JavaException(string msg, Ex innerException = null) : base(msg, innerException)
        {
        }

        public virtual string toString()
        {
            return string.Join(ToString(),
                base.InnerException != null ? "\n" + base.InnerException.Message : string.Empty);
        }
    }
}
