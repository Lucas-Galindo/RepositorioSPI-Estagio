using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.MensalidadeLembrete
{
    // Fakes manuais (biblioteca de mock nao instalada neste projeto de testes), no
    // mesmo padrao de Mensalidade/MensalidadeFakes.cs e Lembretes/LembreteFakes.cs.

    public class FakePagamentoRepositoryParaLembreteVencimento : IPagamentoRepository
    {
        public List<Pagamento> Pagamentos { get; } = new();
        public int Salvamentos { get; private set; }

        public Task<List<Pagamento>> ListarMensalidadesParaLembreteVencimentoAsync(DateOnly dataVencimentoAlvo, CancellationToken cancellationToken = default) =>
            Task.FromResult(Pagamentos
                .Where(p => p.VinculoCobrancaId != null
                    && p.Status == "Pendente"
                    && !p.LembreteVencimentoEnviado
                    && p.DataVencimento <= dataVencimentoAlvo)
                .ToList());

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.CompletedTask;
        }

        public Task<Pagamento?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<List<Pagamento>> ListarAsync(int? alunoId, string? status, DateOnly? vencimentoInicio, DateOnly? vencimentoFim, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> ExisteMensalidadeGeradaAsync(int vinculoCobrancaId, DateOnly competencia, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task VincularAulaAsync(int pagamentoId, int aulaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task AtualizarStatusViaProcedureAsync(int pagamentoId, string novoStatus, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    public class FakeEmailSenderParaLembreteVencimento : IEmailSender
    {
        public List<string> Enviados { get; } = new();

        // Hook opcional para simular falha de envio pra um destinatario especifico.
        public Func<string, bool>? FalharPara { get; set; }

        public Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default)
        {
            if (FalharPara?.Invoke(destinatario) == true)
            {
                throw new InvalidOperationException($"Falha simulada ao enviar para {destinatario}.");
            }

            Enviados.Add(destinatario);
            return Task.CompletedTask;
        }
    }
}
