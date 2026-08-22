using System.Collections.Generic;
using System;
namespace EmailService
{
    public class EmailConfig
    {

        private string _smtpServer;
        private int _smtpPort;
       
        public string SmtpServer { get => _smtpServer; private set {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("SMTP Host cannot be empty");
                }
                _smtpServer = value;
            } }
        public int SmtpPort {
            get => _smtpPort; private set {

                if(value < 1 || value > 65535)
                {
                    // ArgumentOutOfRangeException(string) treats the string as the parameter
                    // name, not the message - that produced an unreadable "Parameter name:
                    // Invalid value for SMTP Port" error. Use the (paramName, actualValue, message)
                    // overload instead so the exception is actually useful when logged.
                    throw new ArgumentOutOfRangeException(nameof(SmtpPort), value, "SMTP Port must be between 1 and 65535");
                }

                _smtpPort = value;
            }
        }

        public bool EnableSSL { get; private set; } = false;
        public System.Net.NetworkCredential Credentials { get; private set; }

        public EmailConfig(string server, int port ,System.Net.NetworkCredential credentials, bool enableSSL = false)
        {
            SmtpServer = server;
            SmtpPort = port;
            Credentials = credentials;
            EnableSSL = enableSSL;
        }

        // Note: the previous EmailConfig(string, int) overload was removed - it was fully
        // covered by this constructor's `enableSSL = false` default, so it was dead weight.
        public EmailConfig(string server, int port, bool enableSSL = false)
        {
            SmtpServer = server;
            SmtpPort = port;
            EnableSSL = enableSSL;
        }
    }

   
}