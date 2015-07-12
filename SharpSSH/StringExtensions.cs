using System;
using System.Text;

namespace Tamir.SharpSsh
{
    public static class StringExtensions
    {
        public static byte[] GetBytes(this String str)
        {
            return Encoding.Default.GetBytes(str);
        }
    }
}