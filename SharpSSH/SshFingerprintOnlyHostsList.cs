using System;
using System.Collections.Generic;
using System.Text;

namespace Tamir.SharpSsh
{
	/*
	Copyright (c) 2011 Tao Klerks

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

	public class SshFingerprintOnlyHostsList : jsch.HostKeyRepository
	{
		private Dictionary<string, List<string>> m_hostList = null;

		public SshFingerprintOnlyHostsList(string host, string fingerprint)
			: base()
		{
			m_hostList = new Dictionary<string, List<string>>();
			List<string> hostFingerprints = new List<string>();
			hostFingerprints.Add(fingerprint);
			m_hostList.Add(host, hostFingerprints);
		}

		public SshFingerprintOnlyHostsList(Dictionary<string, List<string>> hostList)
			: base()
		{
			if (hostList == null)
				throw new ArgumentNullException("hostList", "Argument \"hostList\" cannot be null.");

			m_hostList = hostList;
		}

		public override int check(string host, byte[] key)
		{
			int resultSoFar = NOT_INCLUDED;

			List<string> storedHostFingerprints = null;
			if (m_hostList.TryGetValue(host, out storedHostFingerprints))
			{
				string requestedFingerprint = jsch.Util.getFingerPrint(new Tamir.SharpSsh.jsch.jce.MD5(), key);
				resultSoFar = CHANGED;

				foreach (string fingerprint in storedHostFingerprints)
					if (jsch.Util.getFingerPrint(new Tamir.SharpSsh.jsch.jce.MD5(), key) == fingerprint)
						return OK;
			}

			return resultSoFar;
		}

		public override void add(String host, byte[] key, jsch.UserInfo userinfo)
		{
			throw new NotSupportedException("This SSH host key repository does not support changing the SSH known host list.");
		}

		public override jsch.HostKey[] getHostKey()
		{
			throw new NotSupportedException("This SSH host key repository does not support listing known hosts (only fingerprints are stored).");
		}

		public override jsch.HostKey[] getHostKey(String host, String type)
		{
			throw new NotSupportedException("This SSH host key repository does not support listing known hosts (only fingerprints are stored).");
		}

		public override void  remove(String host, String type)
		{
			throw new NotSupportedException("This SSH host key repository does not support changing the SSH known host list.");
		}

		public override void remove(String host, String type, byte[] key)
		{
			throw new NotSupportedException("This SSH host key repository does not support changing the SSH known host list.");
		}

		public override string getKnownHostsRepositoryID()
		{
			return "FixedHostList";
		}

        public override void setKnownHosts(String hostKeyFileName)
        {
            throw new NotSupportedException("This SSH host key repository does not support loading or saving the SSH known host list.");
        }

    }
}
