namespace SPI.Infrastructure.Security
{
    // Ligado a secao "Jwt" da configuracao. Issuer/Audience/expiracoes vem do
    // appsettings.json; Key e secreta e so existe via User Secrets/variavel de ambiente.
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
