using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tamir.SharpSsh;
using Console = System.Diagnostics.Trace;

namespace UnitTestSSH
{
    [TestClass]
    public class UnitTest1
    {
        //   System.Diagnostics.Trace.

        [TestMethod]
        public void TestMethod1()
        {
            string user = Environment.GetEnvironmentVariable("PASS") ?? Environment.UserName;
            string pass = Environment.GetEnvironmentVariable("USER");

            SshConnectionInfo input = new SshConnectionInfo
            {
                Host = "192.168.137.162",
                User = user,
                Pass = pass
            };
            // Util.GetInput();

            SshShell shell = new SshShell(input.Host, input.User);

            if (input.Pass != null) shell.Password = input.Pass;
            if (input.IdentityFile != null) shell.AddIdentityFile(input.IdentityFile);

            //This statement must be prior to connecting
            shell.RedirectToConsole();

            Console.Write("Connecting...");
            shell.Connect();
            Console.WriteLine("OK");

            // SSH-2.0-OpenSSH_6.6.1p1 Ubuntu-2ubuntu2
            Console.WriteLine("server=" + shell.ServerVersion);
            System.Diagnostics.Trace.WriteLine("test");

            while (shell.ShellOpened)
            {
                System.Threading.Thread.Sleep(500);
            }
            Console.Write("Disconnecting...");
            shell.Close();
            Console.WriteLine("OK");


            // SshShellTest.RunExample();
        }
    }
}


/*
< system.diagnostics >
    < trace autoflush = "false" indentsize = "4" >
         < listeners >
           < add name = "configConsoleListener"
          type = "System.Diagnostics.ConsoleTraceListener" />
      </ listeners >
    </ trace >
  </ system.diagnostics >


*/
