namespace SPI.Application.Auth.Dtos
{
    public record EsqueciSenhaRequest
    {
        /// <summary>
        /// E-mail da professora cadastrada. Recuperacao de senha e exclusiva do perfil Professor.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
