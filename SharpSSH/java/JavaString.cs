using System;
using System.Text;

namespace Tamir.SharpSsh.java
{
    /// <summary>
    /// This is an ugly wrapper around System.String that I'm slowly trying to get rid of, at 
    /// least to keep it from leaking into outside code...
    /// </summary>
    public class JavaString
    {
        private string s;

        public JavaString(string s)
        {
            this.s = s;
        }

        public JavaString(byte[] arr)
            : this(Encoding.Default.GetString(arr))
        {
        }

        public static implicit operator JavaString(String s1)
        {
            if (s1 == null) return null;
            return new JavaString(s1);
        }

        public static implicit operator String(JavaString s1)
        {
            if (s1 == null) return null;
            return s1.ToString();
        }

        public static String operator +(JavaString s1, JavaString s2)
        {
            return s1.ToString() + s2.ToString();
        }

        public byte[] GetBytes()
        {
            return this.ToString().GetBytes();
        }

        public int IndexOf(String sub)
        {
            return s.IndexOf(sub);
        }

        public int IndexOf(char sub)
        {
            return s.IndexOf(sub);
        }

        public int IndexOf(char sub, int i)
        {
            return s.IndexOf(sub, i);
        }

        public char CharAt(int i)
        {
            return s[i];
        }

        public String Substring(int start, int end)
        {
            int len = end - start;
            return s.Substring(start, len);
        }

        public String Substring(int len)
        {
            return s.Substring(len);
        }

        public int Length()
        {
            return s.Length;
        }

        public bool EndsWith(String str)
        {
            return s.EndsWith(str);
        }

        public int LastIndexOf(String str)
        {
            return s.LastIndexOf(str);
        }

        public int LastIndexOf(char c)
        {
            return s.LastIndexOf(c);
        }

        public override string ToString()
        {
            return s;
        }
    }
}