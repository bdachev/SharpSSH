using System.Net;

namespace Tamir.SharpSsh.java.net
{
    public class InetAddress
    {
        internal IPAddress addr;

        public InetAddress(IPAddress addr)
        {
            this.addr = addr != null ? addr 
                : IPAddress.None; // not null
                // "127.0.0.1";
        }

        public bool isAnyLocalAddress()
        {
            return IPAddress.IsLoopback(addr);
        }

        public override int GetHashCode()
        {
            return addr.ToString().GetHashCode();
        }

        public bool equals(InetAddress addr)
        {
            return addr.ToString().Equals(addr.ToString());
        }

        public bool equals(string addr)
        {
            return addr.ToString().Equals(addr.ToString());
        }

        public override string ToString()
        {
            return addr.ToString();
        }

        public override bool Equals(object obj)
        {
            return equals(obj.ToString());
        }

        public string getHostAddress()
        {
            return ToString();
        }
    }
}