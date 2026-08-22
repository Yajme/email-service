using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace EmailService
{
    public class SmtpClientPool : IDisposable
    {


        private readonly EmailConfig _config;
        private readonly ConcurrentBag<SmtpClient> _clients = new ConcurrentBag<SmtpClient>();
        private readonly SemaphoreSlim _poolLock;
        private readonly int _maxPoolSize;

        public SmtpClientPool(EmailConfig config, int maxPoolSize = 10)
        {
            _config = config;
            _maxPoolSize = maxPoolSize;
            _poolLock = new SemaphoreSlim(maxPoolSize, maxPoolSize);
        }

        public async Task<SmtpClient> GetClientAsync()
        {
            // Wait for a slot to become available in the pool
            await _poolLock.WaitAsync();

            // Try to get an existing client from the bag
            if (_clients.TryTake(out var client))
            {
                // Check if the connection is still alive
                if (client.IsConnected)
                {
                    return client;
                }

                // Connection died, dispose it and create a new one
                client.Dispose();
            }

            // Create and authenticate a new client
            return await CreateAuthenticatedClientAsync();
        }

        public void ReturnClient(SmtpClient client)
        {
            if (client == null) return;

            if (client.IsConnected)
            {
                _clients.Add(client);
            }
            else
            {
                client.Dispose();
            }

            // Release the slot back to the semaphore
            _poolLock.Release();
        }

        private async Task<SmtpClient> CreateAuthenticatedClientAsync()
        {
            var client = new SmtpClient();
            var options = _config.EnableSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto;

            await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, options);

            if (_config.Credentials != null)
            {
                await client.AuthenticateAsync(_config.Credentials.UserName, _config.Credentials.Password);
            }

            return client;
        }

        public void Dispose()
        {
            while (_clients.TryTake(out var client))
            {
                client.Disconnect(true);
                client.Dispose();
            }
            _poolLock.Dispose();
        }
    }
}
