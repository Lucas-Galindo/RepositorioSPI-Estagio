namespace SPI.Domain.Exceptions
{
    // Violacao de regra de negocio que impede a operacao (CPF/e-mail/nome
    // duplicado, conflito de horario de agenda, senha atual incorreta, etc).
    public class ConflitoException : Exception
    {
        public ConflitoException(string mensagem) : base(mensagem)
        {
        }
    }
}
