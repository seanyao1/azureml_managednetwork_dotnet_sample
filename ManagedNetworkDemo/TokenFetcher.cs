using Azure.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;

namespace ManagedNetworkDemo
{
    public interface ITokenFectcher
    {       
        Task InitializeAsync(string tenantId, string clientId, string clientKey);

        Task<string> GetAccessTokenAsync();
    }

    public class TokenFetcher : ITokenFectcher
    {
        private AuthenticationResult _authResult;
        private string _clientId;
        private string _clientKey;
        
        public async Task<string> GetAccessTokenAsync()
        {
            if (_authResult == null)
            {
                throw new InvalidOperationException("TokenFetcher not initialized");
            }

            if(_authResult.ExpiresOn < DateTimeOffset.UtcNow.AddSeconds(30)) // if token is about to expire in 30 seconds, refresh it
            {
                _authResult = await this.AcquireTokenAsync(_authResult.TenantId, _clientId, _clientKey);
            }

            return _authResult.AccessToken;
        }

        public async Task InitializeAsync(string tenantId, string clientId, string clientKey)
        {
            _clientId = clientId;
            _clientKey = clientKey;
            _authResult = await this.AcquireTokenAsync(tenantId, clientId, clientKey);
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(string tenantId, string clientId, string clientKey)
        {
            Console.WriteLine("Begin AcquireTokenAsync");

            string authContextURL = "https://login.windows.net/" + tenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            var credential = new ClientCredential(clientId, clientKey);
            var result = await authenticationContext.AcquireTokenAsync("https://management.azure.com/", credential);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the bearer token");
            }

            return result;
        }
    }
}
