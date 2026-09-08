namespace SPI.Application.Auth.Dtos
{
    public record RedefinirSenhaRequest
    {
        /// <summary>
        /// Token recebido por e-mail (valido por 15 minutos, uso unico).
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Nova senha (minimo 8 caracteres, alfanumerica).
        /// </summary>
        public string NovaSenha { get; set; } = string.Empty;
    }
}
