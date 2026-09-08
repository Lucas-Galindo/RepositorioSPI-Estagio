using SPI.Application.Auth.Dtos;

namespace SPI.Application.Auth.Services
{
    public interface IRecuperacaoSenhaService
    {
        // Nunca lanca excecao por e-mail nao encontrado: a resposta ao
        // cliente e sempre generica, para nao revelar quais e-mails existem.
        Task EsqueciSenhaAsync(EsqueciSenhaRequest request, CancellationToken cancellationToken = default);

        Task RedefinirSenhaAsync(RedefinirSenhaRequest request, CancellationToken cancellationToken = default);
    }
}
