using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tamir.SharpSsh;
using Tamir.SharpSsh.jsch;
using Tamir.SharpSsh.Wrappers;
using Console = System.Diagnostics.Trace;

namespace UnitTestSSH
{
    [TestClass]
    public class TestSSH
    {
        static SshConnectionInfo UserInput
        {
            get
            {
                string pass = Environment.GetEnvironmentVariable("PASS") ?? Environment.UserName;
                string user = Environment.GetEnvironmentVariable("USER");

                SshConnectionInfo input = new SshConnectionInfo
                {
                    Host = "192.168.137.162",
                    User = user,
                    Pass = pass
                };
                return input;
            }
        }

        [TestMethod]
        public void Test_Vagrant_Connect()
        {
            SshConnectionInfo input = UserInput;

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

            SshExec shellExec = null;
            if (shell.ShellOpened)
            {
                // shell.Close();

                shellExec = SshExec.Clone(shell);
                // new SshExec(shell.Host, shell.Username, shell.Password);
                shellExec.Connect();
            }

            if (shellExec != null && shellExec.Connected)
            {
                var session = shellExec.Session;
                var channel = shellExec.Channel;
                Console.WriteLine(session);
                Console.WriteLine(channel);

                var stream = shellExec.RunCommandEx("ls -l", true);
                // = shell.StreamASCII();
                while (stream.MoveNext())
                    Console.WriteLine(stream.Current);

                System.Threading.Thread.Sleep(500);
            }

            Console.Write("Disconnecting...");
            if (shellExec != null)
                shellExec.Close();
            Console.WriteLine("OK");

        }

        [TestMethod]
        public void Test_Vagrant_ls()
        {
            SshConnectionInfo input = UserInput;

            SshExec exec = new SshExec(input.Host, input.User);
            if (input.Pass != null)
                exec.Password = input.Pass;
            // if(input.IdentityFile != null) exec.AddIdentityFile( input.IdentityFile );

            Console.Write("Connecting...");
            exec.Connect();
            Console.WriteLine("OK");

            // while 
            {
                Console.Write("Enter a command to execute ['Enter' to cancel]: ");
                string command = "ls";

                Console.WriteLine(command);

                var outputEnum = exec.RunCommandEx(command, false); // .RunCommand(command);
                while (outputEnum.MoveNext())
                    Console.WriteLine(outputEnum.Current);

            }

            SFTPUtil util = SFTPUtil.Clone(exec);

            var list = util.ListFiles("/");
            foreach (ChannelSftp.LsEntry line in list)
                Console.WriteLine(line.Filename);

            Console.Write("Disconnecting...");

            util.Dispose();
            exec.Close();
            Console.WriteLine("OK");
        }
    }
}