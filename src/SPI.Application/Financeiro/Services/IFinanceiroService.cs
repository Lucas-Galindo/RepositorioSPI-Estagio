using SPI.Application.Financeiro.Dtos;

namespace SPI.Application.Financeiro.Services
{
    public interface IFinanceiroService
    {
        // turmaNome/materiaNome/alunoBusca (Sprint 4.2 da evolucao do
        // Financeiro): busca por texto (nome ou RA), filtrando so o lado da
        // receita -- despesas (Contas a Pagar) nao tem ligacao com
        // aluno/turma/materia, entao continuam representando o total do negocio.
        Task<VisaoGeralFinanceiraResponse> ObterVisaoGeralAsync(
            DateOnly? periodoInicio,
            DateOnly? periodoFim,
            CancellationToken cancellationToken = default,
            string? turmaNome = null,
            string? materiaNome = null,
            string? alunoBusca = null);
    }
}
