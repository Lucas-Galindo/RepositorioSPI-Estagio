using System.Globalization;
using System.Net;
using SPI.Application.Common;
using SPI.Application.Lembretes.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Lembretes.Services
{
    // specs/060 (US2/US5, research.md R4): e-mail semanal ao aluno e ao
    // responsavel enquanto houver pagamento vencido sem baixa.
    public class LembretePagamentoService : ILembretePagamentoService
    {
        // Falha de SMTP e tentada de novo nas proximas execucoes do mesmo dia, ate este limite.
        public const int MaxTentativas = 3;

        private readonly ILembretePagamentoRepository _lembreteRepository;
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IEmailSender _emailSender;

        public LembretePagamentoService(ILembretePagamentoRepository lembreteRepository, IPagamentoRepository pagamentoRepository, IEmailSender emailSender)
        {
            _lembreteRepository = lembreteRepository;
            _pagamentoRepository = pagamentoRepository;
            _emailSender = emailSender;
        }

        public async Task<RegraLembretePagamentoDto> ObterRegraAsync(CancellationToken cancellationToken = default)
        {
            var regra = await ObterRegraOuFalharAsync(cancellationToken);
            return new RegraLembretePagamentoDto { Ativo = regra.Ativo, DiaSemana = regra.DiaSemana, Hora = regra.Hora };
        }

        public async Task<RegraLembretePagamentoDto> AtualizarRegraAsync(RegraLembretePagamentoDto dto, CancellationToken cancellationToken = default)
        {
            var regra = await ObterRegraOuFalharAsync(cancellationToken);
            regra.Ativo = dto.Ativo;
            regra.DiaSemana = dto.DiaSemana;
            regra.Hora = dto.Hora;
            await _lembreteRepository.SalvarAlteracoesAsync(cancellationToken);
            return await ObterRegraAsync(cancellationToken);
        }

        public async Task<List<EnvioLembretePagamentoResponse>> ListarEnviosAsync(int limite, CancellationToken cancellationToken = default)
        {
            var envios = await _lembreteRepository.ListarEnviosRecentesAsync(Math.Clamp(limite, 1, 500), cancellationToken);
            return envios.Select(e => new EnvioLembretePagamentoResponse
            {
                Id = e.Id,
                AlunoId = e.AlunoId,
                AlunoNome = e.Aluno.Nome,
                DataEnvio = e.DataEnvio,
                TentadoEm = e.TentadoEm,
                Destinatarios = e.Destinatarios,
                Resultado = e.Resultado,
                Tentativas = e.Tentativas,
                Detalhe = e.Detalhe,
            }).ToList();
        }

        public async Task ProcessarAsync(DateTime agora, CancellationToken cancellationToken = default)
        {
            var regra = await ObterRegraOuFalharAsync(cancellationToken);

            // So no dia da regra, a partir do horario. Se o sistema ficar fora do ar
            // o dia inteiro, aquela semana fica sem e-mail (a notificacao no sistema
            // continua valendo de qualquer forma).
            if (!regra.Ativo || (int)agora.DayOfWeek != regra.DiaSemana || TimeOnly.FromDateTime(agora) < regra.Hora)
            {
                return;
            }

            var diaEnvio = DateOnly.FromDateTime(agora);
            // Vencido ANTES do dia de envio: o que vence no proprio dia ainda nao e
            // pendencia, entao o primeiro e-mail e no dia de envio seguinte.
            var pendenciasPorAluno = (await _pagamentoRepository.ListarPendentesVencidosAsync(diaEnvio, cancellationToken))
                .GroupBy(p => p.AlunoId)
                .ToList();
            var enviosDoDia = (await _lembreteRepository.ListarEnviosDoDiaAsync(diaEnvio, cancellationToken))
                .ToDictionary(e => e.AlunoId);

            foreach (var pendencias in pendenciasPorAluno)
            {
                var envio = enviosDoDia.GetValueOrDefault(pendencias.Key);

                // FR-007: no maximo um e-mail com sucesso por aluno por dia de envio.
                if (envio is not null && (envio.Resultado != EnvioLembretePagamento.Falha || envio.Tentativas >= MaxTentativas))
                {
                    continue;
                }

                if (envio is null)
                {
                    envio = new EnvioLembretePagamento { AlunoId = pendencias.Key, DataEnvio = diaEnvio };
                    await _lembreteRepository.AdicionarEnvioAsync(envio, cancellationToken);
                }

                await TentarEnviarAsync(envio, pendencias.ToList(), agora, diaEnvio, cancellationToken);

                try
                {
                    // Salva aluno a aluno: uma queda no meio nao reenvia quem ja recebeu.
                    await _lembreteRepository.SalvarAlteracoesAsync(cancellationToken);
                }
                catch (ConflitoException)
                {
                    // Outra execucao simultanea registrou este aluno primeiro (indice
                    // unico aluno + dia); o registro dela vale. Encerra o ciclo: o
                    // registro barrado continuaria pendente neste escopo e faria falhar
                    // os proximos salvamentos. A proxima execucao segue com os demais.
                    return;
                }
            }
        }

        // Um mesmo e-mail para aluno e responsavel (sem repetir endereco). A
        // professora nunca recebe: so os e-mails cadastrados no aluno.
        private async Task TentarEnviarAsync(EnvioLembretePagamento envio, List<Pagamento> pendencias, DateTime agora, DateOnly diaEnvio, CancellationToken cancellationToken)
        {
            var aluno = pendencias[0].Aluno;
            var destinatarios = new[] { aluno.Email, aluno.EmailResponsavel }
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            envio.TentadoEm = agora;
            envio.Tentativas += 1;
            envio.Destinatarios = string.Join(", ", destinatarios);

            if (destinatarios.Count == 0)
            {
                envio.Resultado = EnvioLembretePagamento.SemDestinatario;
                envio.Detalhe = "Aluno sem e-mail proprio nem de responsavel cadastrado.";
                return;
            }

            // Atencao: o resultado e um so por aluno/dia. Se um destinatario recebe e outro falha, o
            // envio fica como Falha e a nova tentativa reenvia para TODOS -- quem ja recebeu pode
            // receber o e-mail em duplicidade (no maximo MaxTentativas vezes). O registro de envio
            // e unico por aluno e dia, sem resultado por destinatario.
            var corpo = MontarCorpo(aluno.Nome, pendencias, diaEnvio);
            var falhas = new List<string>();
            foreach (var destinatario in destinatarios)
            {
                try
                {
                    await _emailSender.EnviarAsync(destinatario, "SPI - Pagamento em aberto", corpo, cancellationToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    falhas.Add($"{destinatario}: {e.Message}");
                }
            }

            envio.Resultado = falhas.Count == 0 ? EnvioLembretePagamento.Enviado : EnvioLembretePagamento.Falha;
            envio.Detalhe = falhas.Count == 0 ? null : Truncar(string.Join(" | ", falhas), 500);
        }

        private static string MontarCorpo(string alunoNome, List<Pagamento> pendencias, DateOnly diaEnvio)
        {
            var pt = CultureInfo.GetCultureInfo("pt-BR");
            var linhas = string.Concat(pendencias.Select(p =>
                $"<tr><td>{WebUtility.HtmlEncode(p.Descricao ?? "Pagamento")}</td>"
                + $"<td>{p.ValorFinal.ToString("C", pt)}</td>"
                + $"<td>{p.DataVencimento:dd/MM/yyyy}</td>"
                + $"<td>{diaEnvio.DayNumber - p.DataVencimento.DayNumber} dia(s)</td></tr>"));

            return $"""
                <p>Ol&aacute;!</p>
                <p>Consta no sistema da professora pagamento em aberto de <b>{WebUtility.HtmlEncode(alunoNome)}</b>:</p>
                <table border="1" cellpadding="6" cellspacing="0">
                  <tr><th>Descri&ccedil;&atilde;o</th><th>Valor</th><th>Vencimento</th><th>Atraso</th></tr>
                  {linhas}
                </table>
                <p>Se o pagamento j&aacute; foi feito, avise a professora para que ela atualize o registro. Este aviso se repete toda semana at&eacute; a situa&ccedil;&atilde;o ser atualizada.</p>
                """;
        }

        private static string Truncar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];

        private async Task<RegraLembretePagamento> ObterRegraOuFalharAsync(CancellationToken cancellationToken) =>
            await _lembreteRepository.ObterRegraAsync(cancellationToken)
            ?? throw new NaoEncontradoException("Regra do lembrete de pagamento nao encontrada (aplique database/23_centro_notificacoes.sql).");
    }
}
