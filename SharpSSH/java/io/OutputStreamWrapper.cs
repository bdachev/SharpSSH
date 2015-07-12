using System.IO;
using Tamir.SharpSsh.java.io;

// Part of patch 3088879 of sourceforge.net

namespace Tamir.Streams
{
    /// <summary>
    /// Summary description for OutputStreamWrapper.
    /// </summary>
    /// <remarks>Added by Holger Boskugel, vbwebprofi@gmx.de; Berlin, Germany</remarks>
    public class OutputStreamWrapper : OutputStream
    {
        private Stream s;

        public OutputStreamWrapper(Stream s)
        {
            this.s = s;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.s.Write(buffer, offset, count);
        }
    }
}