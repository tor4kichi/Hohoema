using System;
using System.Collections.Generic;
using System.Text;

namespace NiconicoToolkit.Account
{
    public struct MailAndPasswordAuthToken
    {
        public MailAndPasswordAuthToken(string mail, string password)
        {
            Mail = mail;
            Password = password;
        }

        public string Mail { get; }
        public string Password { get; }
    }
}
