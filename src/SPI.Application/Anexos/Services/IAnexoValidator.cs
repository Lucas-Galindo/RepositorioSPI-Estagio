using Microsoft.AspNetCore.Http;

namespace SPI.Application.Anexos.Services
{
    /// <summary>
    /// Validacao de arquivo de anexo (comprovante financeiro), centralizada e
    /// reaproveitada por Contas a Pagar e Contas a Receber (Principio II da
    /// constituicao: validacao de negocio unica, nao duplicada).
    /// </summary>
    public interface IAnexoValidator
    {
        /// <summary>Devolve a lista de mensagens de erro (vazia = arquivo valido).</summary>
        List<string> Validar(IFormFile arquivo);
    }
}
