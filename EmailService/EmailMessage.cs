using System;
using System.Collections.Generic;
using System.IO;
using MimeKit;
using System.Text.RegularExpressions;
using System.Diagnostics;




namespace EmailService
{
    public interface IEmailMessage
    {
        MimeMessage SetMessage();
    }

    public class EmailMessage : IEmailMessage
    {
        private static readonly Regex ClassicEmailRegex = new Regex(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );
        // Sender
        public string FromSenderAddress { get; set; } = string.Empty;
        public string FromSenderName { get; set; } = string.Empty;
        public EmailType SenderType { get; set; } = EmailType.Department;

        // Receiver
        public string ToRecipientAddress { get; set; } = string.Empty;
        public string ToRecipientName { get; set; } = string.Empty;
        public List<string> ReceiverEmails { get; set; } = new List<string>();
        public EmailType ReceiverType { get; set; } = EmailType.Department;

        // CCs, BCCs
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();

        // Message content
        public string Subject { get; set; } = string.Empty;
        public object Body { get; set; } = string.Empty;
        public TextPartType TextPartType { get; set; } = TextPartType.plain;


        public string HtmlContent { get; set; } = string.Empty;
        // Attachment
        public List<string> AttachmentFilePath { get; set; } = new List<string>();


        
        private static MailboxAddress CreateMailboxAddress(string address, string name)
        {
            if (string.IsNullOrWhiteSpace(address) || !ClassicEmailRegex.IsMatch(address))
            {
                throw new ArgumentException("Enter a valid email address");
            }

            if (name == string.Empty)
                Trace.TraceWarning($"Mailbox Name for email <{address}> is empty.");
            return new MailboxAddress(name ?? string.Empty, address);
        }
 
        private void PopulateHeaders(MimeMessage message)
        {
            Trace.TraceInformation("Populating Headers");


            // Set Sender
            Trace.TraceInformation("Adding Sender details as MailAddress");
            message.From.Add(CreateMailboxAddress(FromSenderAddress, FromSenderName));

            // Guard against an empty/whitespace recipient string before splitting,
            // since "".Split(',') returns [""] rather than an empty list.
            if (string.IsNullOrWhiteSpace(ToRecipientAddress))
                throw new ArgumentException("Recepients email should not be empty");

            // We should check first if there is multiple recipients then breaking it down to a list.
            // Trim each entry so "a@x.com, b@x.com" (space after comma) doesn't fail the regex.
            var recepients = ToRecipientAddress
                .Split(',')
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (recepients.Count < 1)
                throw new ArgumentException("Recepients email should not be empty");

            // Store a copy on the public property so callers/loggers see the full,
            // untouched recipient list. Using the same list reference here would mean
            // the .Remove() call below silently mutates ReceiverEmails too.
            ReceiverEmails = new List<string>(recepients);
            Trace.TraceInformation("Adding Recipient details as MailAddress");
            var primaryRecipient = CreateMailboxAddress(recepients[0], ToRecipientName);

            // Add the very first primary recepient
            message.To.Add(primaryRecipient);
            recepients.RemoveAt(0);

            if (recepients.Count > 0)
            {
                foreach (var email in recepients)
                {
                    message.To.Add(CreateMailboxAddress(email, string.Empty));
                }
            }

            // Set CC/BCC
            foreach (var email in Cc)
                message.Cc.Add(CreateMailboxAddress(email,string.Empty));
            foreach (var email in Bcc)
                message.Bcc.Add(CreateMailboxAddress(email,string.Empty));
            

            message.Subject = Subject;
        }


        
        public MimeMessage SetMessage()
        {
            try {
                var message = new MimeMessage();

                // 1. Headers are separate from the Body
                PopulateHeaders(message);

                // 2. Use BodyBuilder for EVERYTHING related to the content
                var builder = new BodyBuilder();

                // Add attachments to the same builder
                if (AttachmentFilePath != null && AttachmentFilePath.Count > 0)
                {
                    foreach (string filepath in AttachmentFilePath)
                    {
                        if (File.Exists(filepath))
                        {
                            builder.Attachments.Add(filepath);
                        }
                        else
                        {
                            throw new FileNotFoundException($"Attachment file not found: {filepath}");
                        }
                    }
                }

                // Set the body based on the type. (Previously TextBody was set once above
                // and then unconditionally overwritten here - that first assignment was dead code.)
                if (TextPartType == TextPartType.html)
                {
                    builder.HtmlBody = !string.IsNullOrEmpty(HtmlContent)
                        ? HtmlContent
                        : Body?.ToString() ?? string.Empty;
                }
                else
                {
                    builder.TextBody = Body?.ToString() ?? string.Empty;
                }
                // 3. Finalize the body once
                message.Body = builder.ToMessageBody();

                return message;
            } catch(Exception ex)
            {
                Trace.TraceError($"Something went wrong: {ex.Message}");
                // Preserve the original stack trace - `throw ex;` would reset it to this line.
                throw;
            }
        }
    }

   
    public enum TextPartType
    {
        html,
        plain
    }
    public enum EmailType
    {
        Department,
        Individual
    }
}