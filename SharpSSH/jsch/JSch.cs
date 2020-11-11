using System;
using System.Collections;
using System.Text;

namespace Tamir.SharpSsh.jsch
{
    /* -*-mode:java; c-basic-offset:2; -*- */
    /*
    Copyright (c) 2002,2003,2004 ymnk, JCraft,Inc. All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:

    1. Redistributions of source code must retain the above copyright notice,
        this list of conditions and the following disclaimer.

    2. Redistributions in binary form must reproduce the above copyright 
        notice, this list of conditions and the following disclaimer in 
        the documentation and/or other materials provided with the distribution.

    3. The names of the authors may not be used to endorse or promote products
        derived from this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
    INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
    FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
    INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
    INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
    LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
    OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
    EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
    */

    public class JSch
    {
        private static Hashtable config;

        public static void Init()
        {
            config = new Hashtable();

            config.Add("kex", "diffie-hellman-group1-sha1,diffie-hellman-group-exchange-sha1,diffie-hellman-group-exchange-sha256");
            config.Add("server_host_key", "ssh-rsa,ssh-dss");

            config.Add("cipher.s2c", "aes128-ctr,aes128-cbc,3des-cbc,aes192-ctr,aes192-cbc,aes256-ctr,aes256-cbc");
            config.Add("cipher.c2s", "aes128-ctr,aes128-cbc,3des-cbc,aes192-ctr,aes192-cbc,aes256-ctr,aes256-cbc");
            config.Add("mac.s2c", "hmac-md5,hmac-sha1,hmac-sha2-256,hmac-sha1-96,hmac-md5-96");
            config.Add("mac.c2s", "hmac-md5,hmac-sha1,hmac-sha2-256,hmac-sha1-96,hmac-md5-96");
            config.Add("compression.s2c", "none");
            config.Add("compression.c2s", "none");
            config.Add("lang.s2c", "");
            config.Add("lang.c2s", "");

            config.Add("diffie-hellman-group-exchange-sha1", "Tamir.SharpSsh.jsch.DHGEX");
            config.Add("diffie-hellman-group1-sha1", "Tamir.SharpSsh.jsch.DHG1");
            config.Add("diffie-hellman-group-exchange-sha256", "Tamir.SharpSsh.jsch.DHGEX256");

            config.Add("dh", "Tamir.SharpSsh.jsch.jce.DH");
            config.Add("3des-cbc", "Tamir.SharpSsh.jsch.jce.TripleDESCBC");
            config.Add("hmac-sha1", "Tamir.SharpSsh.jsch.jce.HMACSHA1");
            config.Add("hmac-sha1-96", "Tamir.SharpSsh.jsch.jce.HMACSHA196");
            config.Add("hmac-md5", "Tamir.SharpSsh.jsch.jce.HMACMD5");
            config.Add("hmac-md5-96", "Tamir.SharpSsh.jsch.jce.HMACMD596");
            config.Add("hmac-sha2-256", "Tamir.SharpSsh.jsch.jce.HMACSHA256");
            config.Add("hmac-sha2-512", "Tamir.SharpSsh.jsch.jce.HMACSHA512");
            config.Add("sha-1", "Tamir.SharpSsh.jsch.jce.SHA1");
            config.Add("sha-256", "Tamir.SharpSsh.jsch.jce.SHA256");
            config.Add("sha-384", "Tamir.SharpSsh.jsch.jce.SHA384");
            config.Add("sha-512", "Tamir.SharpSsh.jsch.jce.SHA512");
            config.Add("md5", "Tamir.SharpSsh.jsch.jce.MD5");
            config.Add("signature.dss", "Tamir.SharpSsh.jsch.jce.SignatureDSA");
            config.Add("signature.rsa", "Tamir.SharpSsh.jsch.jce.SignatureRSA");
            config.Add("keypairgen.dsa", "Tamir.SharpSsh.jsch.jce.KeyPairGenDSA");
            config.Add("keypairgen.rsa", "Tamir.SharpSsh.jsch.jce.KeyPairGenRSA");
            config.Add("random", "Tamir.SharpSsh.jsch.jce.Random");

            config.Add("aes128-cbc", "Tamir.SharpSsh.jsch.jce.AES128CBC");
            config.Add("aes128-ctr", "Tamir.SharpSsh.jsch.jce.AES128CTR");
            config.Add("aes192-cbc", "Tamir.SharpSsh.jsch.jce.AES192CBC");
            config.Add("aes192-ctr", "Tamir.SharpSsh.jsch.jce.AES192CTR");
            config.Add("aes256-cbc", "Tamir.SharpSsh.jsch.jce.AES256CBC");
            config.Add("aes256-ctr", "Tamir.SharpSsh.jsch.jce.AES256CTR");

            config.Add("StrictHostKeyChecking", "ask");
        }

        internal ArrayList pool = new ArrayList();
        internal ArrayList identities = new ArrayList();

        private HostKeyRepository known_hosts = null;

        public JSch()
        {
            if (config == null)
                Init();
        }

        public Session getSession(String username, String host)
        {
            return getSession(username, host, 22);
        }

        public Session getSession(String username, String host, int port)
        {
            Session s = new Session(this);
            s.setUserName(username);
            s.setHost(host);
            s.setPort(port);
            pool.Add(s);
            return s;
        }

        internal void removeSession(Session session)
        {
            lock (pool)
            {
                pool.Remove(session);
            }
        }

        public HostKeyRepository getHostKeyRepository()
        {
            if (known_hosts == null) known_hosts = new KnownHosts(this);
            return known_hosts;
        }

        public void setHostKeyRepository(HostKeyRepository value)
        {
            known_hosts = value;
        }

        public void addIdentity(String foo)
        {
            addIdentity(foo, (String) null);
        }

        public void addIdentity(String foo, String bar)
        {
            Identity identity = new IdentityFile(foo, this);
            if (bar != null) identity.setPassphrase(bar);
            identities.Add(identity);
        }

        internal String getConfig(String foo)
        {
            return (String) (config[foo]);
        }

        private ArrayList proxies;

        private void setProxy(String hosts, IProxy proxy)
        {
            String[] patterns = Util.split(hosts, ",");
            if (proxies == null)
            {
                proxies = new ArrayList();
            }
            lock (proxies)
            {
                for (int i = 0; i < patterns.Length; i++)
                {
                    if (proxy == null)
                    {
                        proxies[0] = null;
                        proxies[0] = Encoding.Default.GetBytes(patterns[i]);
                    }
                    else
                    {
                        proxies.Add(Encoding.Default.GetBytes(patterns[i]));
                        proxies.Add(proxy);
                    }
                }
            }
        }

        internal IProxy getProxy(String host)
        {
            if (proxies == null) return null;
            byte[] _host = Encoding.Default.GetBytes(host);
            lock (proxies)
            {
                for (int i = 0; i < proxies.Count; i += 2)
                {
                    if (Util.glob(((byte[]) proxies[i]), _host))
                    {
                        return (IProxy) (proxies[i + 1]);
                    }
                }
            }
            return null;
        }

        internal void ClearProxies()
        {
            proxies = null;
        }

        public static void setConfig(Hashtable foo)
        {
            lock (config)
            {
                IEnumerator e = foo.Keys.GetEnumerator();
                while (e.MoveNext())
                {
                    String key = (String) (e.Current);
                    config.Add(key, (String) (foo[key]));
                }
            }
        }
    }
}
