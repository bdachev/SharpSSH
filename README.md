## SharpSSH - A Secure Shell (SSH) library for .NET

Tamir Gal (c) 2007 and jcraft.com

This is a port of Tamir Gals .Net SSH Client with streaming opened up.

SharpSSH is a pure .NET implementation of the SSH2 client protocol suite. It provides an API for communication with SSH servers and can be integrated into any .NET application.

The library is a C# port of the JSch project from JCraft Inc. and is released under [BSD style license]
(http://www.jcraft.com/jsch/LICENSE.txt).

SharpSSH allows you to read/write data and transfer files over SSH channels using an API similar to JSch's API. In addition, it provides some additional wrapper classes which offer even simpler abstraction for SSH communication.
SharpSSH is hosted on sourceforge, please check out its project page.


### SharpSSH

http://www.tamirgal.com/blog/page/SharpSSH.aspx

SharpSSH is a pure .NET implementation of the SSH2 client protocol suite. It provides an API for communication with SSH servers and can be integrated into any .NET application.
The library is a C# port of the JSch project from JCraft Inc. and is released under BSD style license.

SharpSSH allows you to read/write data and transfer files over SSH channels using an API similar to JSch's API. In addition, it provides some additional wrapper classes which offer even simpler abstraction for SSH communication.
SharpSSH is hosted on sourceforge, please check out its project page.

### Feature List
SharpSSH is not yet a full port of JSch. The following list summarizes the features currently supported by SharpSSH:
SharpSSH is pure .NET, but it depends on Mentalis.org Crypto Library for encryption and integrity functions.
SSH2 protocol support
SSH File Transfer Protocol (SFTP)
Secure Copy (SCP)
Key exchange: diffie-hellman-group-exchange-sha1, diffie-hellman-group1-sha1
Cipher: 3des-cbc, aes128-cbc
MAC: hmac-md5
Host key type: ssh-rsa, ssh-dss
Userauth: password, publickey (RSA, DSA)
Port Forwarding
Stream Forwarding
Remote Exec
Generating DSA and RSA key pairs
Changing the passphrase for a private key

### Download

SharpSSH is now hosted on sourceforge: http://sourceforge.net/projects/sharpssh/

