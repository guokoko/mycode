namespace CTO.Price.Shared.Domain
{
    public sealed class AzureAdSetting
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Instance { get; set; }
        public string? CallbackPath { get; set; }
        public string? CookieSchemeName { get; set; }
        public string? SignedOutCallbackPath { get; set; }
        public string? RemoteSignOutPath { get; set; }
    }
}