using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NSwagIdsrv.Client
{
    public abstract class AuthNswagClientBase
    {
        private readonly AuthNswagClientOptions opts;

        private DiscoveryDocumentResponse disco;

        private TokenResponse token;
        
        private DateTime tokenExpirationTime;
        
        public string BaseUrl { get; }

        protected abstract string GetScope();

        protected AuthNswagClientBase(AuthNswagClientOptions opts)
        {
            this.opts = opts;
            BaseUrl = opts.BusinessServiceUrl;
        }

        protected virtual JsonSerializerSettings GenerateUpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.Error = HandleDeserializationError;
            return settings;
        }

        protected virtual void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
        {
            errorArgs.ErrorContext.Handled = true;
        }
        
        protected async Task<HttpClient> CreateHttpClientAsync(CancellationToken ct)
        {
            var httpClient = new HttpClient();

            await GetTokenIfNeed(httpClient, ct);
            await RefreshTokenIfNeed(httpClient, ct);

            httpClient.SetBearerToken(token.AccessToken);
            return httpClient;
        }

        private async Task RefreshTokenIfNeed(HttpClient httpClient, CancellationToken ct)
        {
            if (DateTime.UtcNow < tokenExpirationTime)
            {
                return;
            }

            token = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = opts.ClientId,
                ClientSecret = opts.ClientSecret,
                RefreshToken = token.RefreshToken
            }, ct);
            
            OnGotToken();
        }

        private async Task GetTokenIfNeed(HttpClient httpClient, CancellationToken ct)
        {
            if (token != null)
            {
                return;
            }

            disco = await httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = opts.AuthServiceUrl,
                Policy = new DiscoveryPolicy
                {
                    ValidateEndpoints = false,
                    ValidateIssuerName = false,
                    RequireHttps = false
                }
            }, ct);
            if (disco.IsError)
            {
                if (disco.Exception != null)
                {
                    throw new AuthException(disco.Exception.Message, disco.Exception);    
                }

                throw new AuthException(disco.Error);
            }

            token = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = opts.ClientId,
                ClientSecret = opts.ClientSecret,

                UserName = opts.UserName,
                Password = opts.UserSecret,

                Scope = GetScope()
            }, ct);
            OnGotToken();
        }

        private void EnsureTokenIsGotten()
        {
            if (token.IsError)
            {
                if (token.Exception != null)
                {
                    throw new AuthException(token.Exception.Message, token.Exception);    
                }
                throw new AuthException(token.Error);
            }
        }
        
        private void OnGotToken()
        {
            EnsureTokenIsGotten();
            tokenExpirationTime = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 5); // 5 just in case
        }
    }
}