using System;

namespace Tamir.SharpSsh
{
    public delegate void FileTransferEvent(string src, string dst, Int64 transferredBytes, Int64 totalBytes, string message);
}