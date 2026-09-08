namespace SPI.Application.Auth.Dtos
{
    public record RefreshRequest
    {
        /// <summary>
        /// Refresh token valido, emitido por um login ou refresh anterior.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
