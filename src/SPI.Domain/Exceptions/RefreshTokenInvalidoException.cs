namespace SPI.Domain.Exceptions
{
    public class RefreshTokenInvalidoException : Exception
    {
        public RefreshTokenInvalidoException() : base("Refresh token invalido, expirado ou ja utilizado.")
        {
        }
    }
}
