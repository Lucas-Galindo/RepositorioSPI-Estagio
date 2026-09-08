namespace SPI.Domain.Exceptions
{
    // Mensagem sempre generica de proposito: nao deve revelar se o e-mail
    // existe, se a senha esta errada, ou se o usuario esta inativo.
    public class CredenciaisInvalidasException : Exception
    {
        public CredenciaisInvalidasException() : base("E-mail, senha ou perfil invalidos.")
        {
        }
    }
}
