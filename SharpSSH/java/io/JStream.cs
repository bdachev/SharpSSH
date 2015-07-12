using System;
using System.IO;
using Tamir.Streams;

namespace Tamir.SharpSsh.java.io
{
    public class JStream : Stream
    {
        internal Stream wrappedStream;

        public JStream(Stream wrappedStream)
        {
            this.wrappedStream = wrappedStream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return wrappedStream.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            return wrappedStream.ReadByte();
        }

        public int read(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count);
        }

        public int read(byte[] buffer)
        {
            return Read(buffer, 0, buffer.Length);
        }

        public int read()
        {
            return ReadByte();
        }

        public override void Close()
        {
            wrappedStream.Close();
        }

        public override void WriteByte(byte value)
        {
            wrappedStream.WriteByte(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            wrappedStream.Write(buffer, offset, count);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public override bool CanRead
        {
            get { return wrappedStream.CanRead; }
        }

        public override bool CanWrite
        {
            get { return wrappedStream.CanWrite; }
        }

        public override bool CanSeek
        {
            get { return wrappedStream.CanSeek; }
        }

        public override void Flush()
        {
            wrappedStream.Flush();
        }

        public override long Length
        {
            get { return wrappedStream.Length; }
        }

        public override long Position
        {
            get { return wrappedStream.Position; }
            set { wrappedStream.Position = value; }
        }

        public override void SetLength(long value)
        {
            wrappedStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return wrappedStream.Seek(offset, origin);
        }

        public long skip(long len)
        {
            //Seek doesn't work
            //return Seek(offset, IO.SeekOrigin.Current);
            int i = 0;
            int count = 0;
            byte[] buf = new byte[len];
            while (len > 0)
            {
                i = Read(buf, count, (int)len); //tamir: possible lost of pressision
                if (i <= 0)
                {
                    throw new Exception("inputstream is closed");
                    //return (s-foo)==0 ? i : s-foo;
                }
                count += i;
                len -= i;
            }
            return count;
        }

        public int available()
        {
            if (wrappedStream is PipedInputStream)
            {
                return ((PipedInputStream)wrappedStream).available();
            }
            throw new Exception("JStream.available() -- Method not implemented");
        }

        public void flush()
        {
            wrappedStream.Flush();
        }
    }
}