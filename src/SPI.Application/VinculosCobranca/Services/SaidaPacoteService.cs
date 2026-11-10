using System.Globalization;
using SPI.Application.Common;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.VinculosCobranca.Services
{
    // specs/078: saida do aluno de uma turma de pacote com acerto proporcional ao consumo. A previa e a gravacao partem
    // da mesma leitura e do mesmo calculo (AcertoSaidaPacote); a tela so exibe (Principio II).
    public class SaidaPacoteService : ISaidaPacoteService
    {
        public const string CategoriaDoAcerto = "Acerto de pacote";

        private const int TamanhoMaximoDaDescricao = 255;

        private readonly ITurmaRepository _turmaRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly IVinculoCobrancaRepository _vinculoRepository;
        private readonly IAulaRepository _aulaRepository;
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly ICategoriaReceitaRepository _categoriaRepository;
        private readonly TimeProvider _relogio;

        public SaidaPacoteService(
            ITurmaRepository turmaRepository,
            IAlunoRepository alunoRepository,
            IVinculoCobrancaRepository vinculoRepository,
            IAulaRepository aulaRepository,
            IPagamentoRepository pagamentoRepository,
            ICategoriaReceitaRepository categoriaRepository,
            TimeProvider relogio)
        {
            _turmaRepository = turmaRepository;
            _alunoRepository = alunoRepository;
            _vinculoRepository = vinculoRepository;
            _aulaRepository = aulaRepository;
            _pagamentoRepository = pagamentoRepository;
            _categoriaRepository = categoriaRepository;
            _relogio = relogio;
        }

        // Mostra o acerto sem gravar nada (nem remove nada).
        public async Task<AcertoSaidaPacoteResponse> PreVisualizarAsync(int turmaId, int alunoId, SaidaPacoteRequest request, CancellationToken cancellationToken = default)
        {
            var situacao = await CarregarAsync(turmaId, alunoId, cancellationToken);
            return MontarAcerto(situacao, request.ValorPago);
        }

        // Grava tudo ou nada: ate o "SalvarAlteracoesAsync" so ha leitura e entidades rastreadas; o EF grava a
        // desvinculacao, a retirada das aulas, o encerramento do pacote e a conta da diferenca numa transacao so.
        public async Task<SaidaPacoteRegistradaResponse> RegistrarAsync(int turmaId, int alunoId, ConfirmarSaidaPacoteRequest request, CancellationToken cancellationToken = default)
        {
            var situacao = await CarregarAsync(turmaId, alunoId, cancellationToken);
            var acerto = MontarAcerto(situacao, request.ValorPago);

            // A professora confirmou o resumo que viu: se o consumo mudou desde entao, nada e gravado.
            if (acerto.AulasConsumidas != request.ConsumidasEsperadas || acerto.ValorDevido != request.ValorDevidoEsperado)
            {
                throw new ConflitoException("O consumo do aluno mudou desde que o resumo foi aberto. Confira o novo resumo e confirme de novo.");
            }

            Pagamento? conta = null;
            if (acerto.Diferenca > 0)
            {
                conta = await MontarContaDaDiferencaAsync(situacao, acerto, request.Vencimento, cancellationToken);
                await _pagamentoRepository.AdicionarAsync(conta, cancellationToken);
            }

            await _turmaRepository.DesvincularAlunoAsync(turmaId, alunoId, cancellationToken);
            _aulaRepository.RemoverParticipacoes(situacao.ParticipacoesFuturas);

            situacao.Pacote.Ativo = false;
            situacao.Pacote.SaidaRegistradaEm = situacao.Agora;

            await _vinculoRepository.SalvarAlteracoesAsync(cancellationToken);

            return new SaidaPacoteRegistradaResponse { Acerto = acerto, PagamentoId = conta?.Id };
        }

        // Regras de elegibilidade (as mesmas da previa e da gravacao): turma de pacote em andamento, aluno matriculado,
        // pacote ativo dele nessa turma e total de aulas definido.
        private async Task<SituacaoDaSaida> CarregarAsync(int turmaId, int alunoId, CancellationToken cancellationToken)
        {
            var turma = await _turmaRepository.ObterPorIdAsync(turmaId, cancellationToken)
                ?? throw new NaoEncontradoException("Turma nao encontrada.");

            var aluno = await _alunoRepository.ObterPorIdAsync(alunoId, cancellationToken)
                ?? throw new NaoEncontradoException("Aluno nao encontrado.");

            var agora = _relogio.AgoraLocal();
            var hoje = DateOnly.FromDateTime(agora);

            if (turma.Tipo != TipoTurma.Pacote)
            {
                throw new ConflitoException("A saida com acerto existe so na turma de pacote.");
            }

            if (!turma.EstaEmAndamento(hoje))
            {
                throw new ConflitoException("A turma de pacote ja terminou: o pacote dela foi encerrado e nao ha saida a registrar.");
            }

            if (!turma.AlunosTurma.Any(at => at.AlunoId == alunoId))
            {
                throw new ConflitoException("O aluno nao esta matriculado nesta turma.");
            }

            var pacote = (await _vinculoRepository.ListarPacotesAtivosDaTurmaAsync(turmaId, cancellationToken))
                .Where(p => p.AlunoId == alunoId)
                .OrderBy(p => p.Id)
                .FirstOrDefault()
                ?? throw new ConflitoException("O aluno nao tem pacote ativo nesta turma (ou a saida dele ja foi registrada).");

            // Mesma fonte da ficha do aluno e da Home (spec 058): o acerto nao pode divergir do que a professora ja ve.
            var presencas = await _aulaRepository.ContarPresencasPorVinculosAsync(new[] { pacote.Id }, null, null, cancellationToken);
            var consumidas = presencas.GetValueOrDefault(pacote.Id);
            var progresso = ProgressoPacote.Calcular(pacote, consumidas);

            if (progresso.Total is not > 0)
            {
                throw new ConflitoException("O pacote do aluno nao tem o total de aulas definido. Informe o total de aulas do pacote antes de registrar a saida.");
            }

            var futuras = await _aulaRepository.ListarParticipacoesAgendadasQueNaoComecaramAsync(turmaId, alunoId, agora, cancellationToken);

            return new SituacaoDaSaida(turma, aluno, pacote, consumidas, progresso.Total.Value, progresso.TotalEstimado, futuras, hoje, agora);
        }

        private static AcertoSaidaPacoteResponse MontarAcerto(SituacaoDaSaida s, decimal valorPago)
        {
            var calculo = AcertoSaidaPacote.Calcular(s.Pacote.Valor, s.Total, s.Consumidas, valorPago);

            return new AcertoSaidaPacoteResponse
            {
                AlunoId = s.Aluno.Id,
                AlunoNome = s.Aluno.Nome,
                TurmaId = s.Turma.Id,
                TurmaNome = s.Turma.Nome,
                ValorPacote = s.Pacote.Valor,
                TotalAulas = s.Total,
                TotalEstimado = s.TotalEstimado,
                AulasConsumidas = s.Consumidas,
                ValorPorAula = calculo.ValorPorAula,
                ValorDevido = calculo.ValorDevido,
                ValorPago = valorPago,
                Diferenca = calculo.Diferenca,
                Excedente = calculo.Excedente,
                Consequencia = calculo.Consequencia,
                PrecisaVencimento = calculo.Diferenca > 0,
                AulasAgendadasQueSairao = s.ParticipacoesFuturas.Count,
            };
        }

        // A conta da diferenca e uma conta a receber comum (Pendente), ligada ao pacote, no formato das parcelas da spec 062.
        private async Task<Pagamento> MontarContaDaDiferencaAsync(SituacaoDaSaida s, AcertoSaidaPacoteResponse acerto, DateOnly? vencimento, CancellationToken cancellationToken)
        {
            if (vencimento is null)
            {
                throw new ConflitoException("Informe o vencimento da conta da diferenca.");
            }

            if (vencimento.Value < s.Hoje)
            {
                throw new ConflitoException("O vencimento da conta da diferenca nao pode ser anterior a hoje.");
            }

            var categoria = await _categoriaRepository.ObterPorNomeAsync(CategoriaDoAcerto, cancellationToken)
                ?? throw new ConflitoException($"A categoria de receita \"{CategoriaDoAcerto}\" nao existe. Aplique a migracao 28 do banco.");

            var descricao =
                $"{CategoriaDoAcerto} - {s.Aluno.Nome} - {s.Turma.Nome}: {s.Consumidas} de {s.Total} aulas consumidas, " +
                $"devido {Moeda(acerto.ValorDevido)}, pago {Moeda(acerto.ValorPago)}";

            return new Pagamento
            {
                AlunoId = s.Aluno.Id,
                Descricao = descricao.Length > TamanhoMaximoDaDescricao ? descricao[..TamanhoMaximoDaDescricao] : descricao,
                CategoriaReceitaId = categoria.Id,
                DataVencimento = vencimento.Value,
                Competencia = new DateOnly(vencimento.Value.Year, vencimento.Value.Month, 1),
                ValorFinal = acerto.Diferenca,
                Status = "Pendente",
                VinculoCobranca = s.Pacote,
            };
        }

        // "R$ 70,00" sem depender da cultura do servidor.
        private static string Moeda(decimal valor) => "R$ " + valor.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

        private sealed record SituacaoDaSaida(
            Turma Turma,
            Aluno Aluno,
            VinculoCobranca Pacote,
            int Consumidas,
            int Total,
            bool TotalEstimado,
            List<AulaAluno> ParticipacoesFuturas,
            DateOnly Hoje,
            DateTime Agora);
    }
}
