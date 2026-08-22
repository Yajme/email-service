using System;
using System.Threading.Tasks;
using System.Diagnostics;
using MailKit.Net.Smtp;

namespace EmailService
{
    public interface IEmailFunction
    {
        Task SendMailAsync(IEmailMessage message);
    }

    public class EmailFunction : IEmailFunction, IDisposable
    {

        private readonly SmtpClientPool _pool;
        public EmailFunction(EmailConfig config)
        {
            // Initialize the pool with a limit of 10 concurrent connections
            _pool = new SmtpClientPool(config, 10);
        }

        
        // Sending the mail
        public async Task SendMailAsync(IEmailMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            SmtpClient client = null;
            try
            {
                /// 1. Borrow a connected/authenticated client from the pool
                client = await _pool.GetClientAsync();
               message.
                var mimeMessage = message.SetMessage();
                await client.SendAsync(mimeMessage);

                Trace.WriteLine($"Email sent successfully via pool to: {mimeMessage.To}");

                
            }
            // Previously this caught ArgumentException just to re-wrap it in a new
            // ArgumentException with the same message - `throw;` does the same job
            // while preserving the original stack trace.
            catch (ArgumentException)
            {
                throw;
            }
            catch (MailKit.Security.AuthenticationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error sending email: {ex.Message}");

                // We wrap the exception to provide a cleaner error message to the caller
                throw new Exception("An error occurred while sending the email. Please check the logs for details.", ex);
            }
            finally
            {
                //  ALWAYS return the client to the pool, even on failure.
                // Note: if GetClientAsync() itself throws before assigning `client`,
                // this calls ReturnClient(null) - confirm SmtpClientPool null-checks internally.
                _pool.ReturnClient(client);
            }
        }

        // Ensure resources are cleaned up if the service is disposed
        public void Dispose() => _pool.Dispose();
    }
}