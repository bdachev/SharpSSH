/*
Copyright (c) 2012 Tao Klerks

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tamir.SharpSsh
{
    public class DelegatingKeyboardInteractiveUserInfo : jsch.UserInfo, jsch.UIKeyboardInteractive
    {
        // This entire class exists to make the library easier to use from environments that don't easily support subclassing, 
        // such as Powershell.

        public delegate string PromptString(string message);
        private static string _defaultPromptString(string message) { throw new NotImplementedException(); }

        public delegate bool PromptBool(string message);
        private static bool _defaultPromptBool(string message) { throw new NotImplementedException(); }

        public delegate void Show(string message);
        private static void _defaultShow(string message) { throw new NotImplementedException(); }

        public delegate string[] KeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo);
        private static string[] _defaultKeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo) { throw new NotImplementedException(); }

        private string _lastPassphrase = null;
        private string _lastPassword = null;

        private PromptString _promptPassphrase = _defaultPromptString;
        private PromptString _promptPassword = _defaultPromptString;
        private PromptBool _promptYesNo = _defaultPromptBool;
        private Show _showMessage = _defaultShow;
        private KeyboardInteractive _promptKeyboardInteractive = _defaultKeyboardInteractive;

        public string getPassphrase() { return _lastPassphrase; }
        public string getPassword() { return _lastPassword; }
        public bool promptPassphrase(string message) { _lastPassphrase = _promptPassphrase(message); return _lastPassphrase != null; }
        public bool promptPassword(string message) { _lastPassword = _promptPassword(message); return _lastPassword != null; }
        public bool promptYesNo(string message) { return _promptYesNo(message); }
        public void showMessage(string message) { _showMessage(message); }
        public string[] promptKeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo) { return _promptKeyboardInteractive(destination, name, instruction, prompt, echo); }

        public DelegatingKeyboardInteractiveUserInfo(
            PromptString promptPassphrase,
            PromptString promptPassword,
            PromptBool promptYesNo,
            Show showMessage,
            KeyboardInteractive promptKeyboardInteractive
            )
        {
            if (promptPassphrase != null) _promptPassphrase = promptPassphrase;
            if (promptPassword != null) _promptPassword = promptPassword;
            if (promptYesNo != null) _promptYesNo = promptYesNo;
            if (showMessage != null) _showMessage = showMessage;
            if (promptKeyboardInteractive != null) _promptKeyboardInteractive = promptKeyboardInteractive;
        }

    }
}
