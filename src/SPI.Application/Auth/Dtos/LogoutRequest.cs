namespace SPI.Application.Auth.Dtos
{
    public record LogoutRequest
    {
        /// <summary>
        /// Refresh token da sessao/dispositivo a ser revogado.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
