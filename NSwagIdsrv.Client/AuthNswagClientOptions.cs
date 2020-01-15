namespace NSwagIdsrv.Client
{
    public abstract class AuthNswagClientOptions
    {
        public string BusinessServiceUrl { get; set; }
        
        public string AuthServiceUrl { get; set; }
        
        public string ClientId { get; set; }
        
        public string ClientSecret { get; set; }
        
        public string UserName { get; set; }
        
        public string UserSecret { get; set; }
        
        public abstract string Scope { get; }
    }
}