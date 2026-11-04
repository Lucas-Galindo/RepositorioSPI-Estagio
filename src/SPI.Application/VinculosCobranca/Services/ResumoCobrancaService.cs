using SPI.Application.Aulas.Services;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Application.VinculosCobranca.Services
{
    // specs/068: calcula o selo de cobranca em lote. So le: nenhum dado e alterado e nenhuma regra de
    // cobranca e reescrita -- o "que vale" no grupo vem de RegraCobrancaPresenca.Decidir (a mesma funcao
    // que cobra a presenca) e o progresso do pacote, de ProgressoPacote.Calcular (spec 058).
    public class ResumoCobrancaService : IResumoCobrancaService
    {
        public const string SemVinculo = "SemVinculo";

        // Ordem fixa do vinculo principal na LISTA (um lugar so): os planos que cobrem tudo vem antes do pacote.
        private static readonly ModalidadeCobranca[] OrdemDoPrincipal =
        {
            ModalidadeCobranca.Mensalidade,
            ModalidadeCobranca.Semestral,
            ModalidadeCobranca.Anual,
            ModalidadeCobranca.Pacote,
            ModalidadeCobranca.Avulsa,
        };

        private readonly IVinculoCobrancaRepository _vinculoRepository;
        private readonly IAulaRepository _aulaRepository;

        public ResumoCobrancaService(IVinculoCobrancaRepository vinculoRepository, IAulaRepository aulaRepository)
        {
            _vinculoRepository = vinculoRepository;
            _aulaRepository = aulaRepository;
        }

        public async Task<Dictionary<int, SeloCobrancaResponse>> ResumirParaListaAsync(IReadOnlyCollection<Aluno> alunos, CancellationToken cancellationToken = default)
        {
            var (ativosPorAluno, presencas) = await CarregarAsync(alunos, cancellationToken);
            var resultado = new Dictionary<int, SeloCobrancaResponse>();

            foreach (var aluno in alunos)
            {
                var ativos = ativosPorAluno.GetValueOrDefault(aluno.Id) ?? new List<VinculoCobranca>();
                if (ativos.Count == 0)
                {
                    resultado[aluno.Id] = SeloSemVinculo(aluno);
                    continue;
                }

                // Principal: pela ordem fixa; em empate, o mais recente (maior Id).
                var ordenados = ativos
                    .OrderBy(v => PosicaoNaOrdem(v.Modalidade))
                    .ThenByDescending(v => v.Id)
                    .ToList();

                var selo = SeloDoVinculo(ordenados[0], presencas, incluirTurma: true);
                selo.OutrosVinculos = ordenados.Count - 1;
                selo.Detalhes = ordenados.Select(v => DetalheDoVinculo(v, presencas)).ToList();
                resultado[aluno.Id] = selo;
            }

            return resultado;
        }

        public async Task<Dictionary<int, SeloCobrancaResponse>> ResumirParaGrupoAsync(IReadOnlyCollection<Aluno> alunos, int? turmaId, DateOnly? dataDaAula = null, CancellationToken cancellationToken = default)
        {
            var data = dataDaAula ?? DateOnly.FromDateTime(DateTime.Now);
            var (ativosPorAluno, presencas) = await CarregarAsync(alunos, cancellationToken);
            var resultado = new Dictionary<int, SeloCobrancaResponse>();

            foreach (var aluno in alunos)
            {
                var ativos = ativosPorAluno.GetValueOrDefault(aluno.Id) ?? new List<VinculoCobranca>();
                var decisao = RegraCobrancaPresenca.Decidir(ativos, turmaId, data);

                resultado[aluno.Id] = decisao.Tipo switch
                {
                    TipoCobrancaPresenca.PlanoPeriodico or TipoCobrancaPresenca.Pacote => SeloDoVinculo(decisao.Vinculo!, presencas, incluirTurma: false),
                    TipoCobrancaPresenca.Avulsa => SeloDoVinculo(decisao.Vinculo!, presencas, incluirTurma: false) with { AvisoPacoteEsgotado = decisao.PacoteEsgotado },
                    // A presenca sera barrada: o pacote do grupo esta esgotado e nao ha avulsa para cair.
                    TipoCobrancaPresenca.Bloqueada => PacoteEsgotado(decisao.Vinculo!, presencas),
                    _ => SeloSemVinculo(aluno),
                };
            }

            return resultado;
        }

        // No maximo duas consultas para a lista inteira: vinculos ativos de todos os alunos e, so se houver
        // pacote, as presencas desses pacotes.
        private async Task<(Dictionary<int, List<VinculoCobranca>> AtivosPorAluno, Dictionary<int, int> Presencas)> CarregarAsync(
            IReadOnlyCollection<Aluno> alunos, CancellationToken cancellationToken)
        {
            var alunoIds = alunos.Select(a => a.Id).Distinct().ToList();
            if (alunoIds.Count == 0)
            {
                return (new Dictionary<int, List<VinculoCobranca>>(), new Dictionary<int, int>());
            }

            var vinculos = await _vinculoRepository.ListarAtivosDosAlunosAsync(alunoIds, cancellationToken);
            var ativosPorAluno = vinculos
                .Where(v => v.Ativo)
                .GroupBy(v => v.AlunoId)
                .ToDictionary(g => g.Key, g => g.OrderBy(v => v.Id).ToList());

            var pacoteIds = vinculos.Where(v => v.Ativo && v.Modalidade == ModalidadeCobranca.Pacote).Select(v => v.Id).ToList();
            var presencas = pacoteIds.Count == 0
                ? new Dictionary<int, int>()
                : await _aulaRepository.ContarPresencasPorVinculosAsync(pacoteIds, null, null, cancellationToken);

            return (ativosPorAluno, presencas);
        }

        private static int PosicaoNaOrdem(ModalidadeCobranca modalidade)
        {
            var posicao = Array.IndexOf(OrdemDoPrincipal, modalidade);
            return posicao < 0 ? int.MaxValue : posicao;
        }

        private static SeloCobrancaResponse SeloDoVinculo(VinculoCobranca vinculo, IReadOnlyDictionary<int, int> presencas, bool incluirTurma) => new()
        {
            Categoria = vinculo.Modalidade.ToString(),
            Valor = vinculo.Valor,
            TurmaNome = incluirTurma ? vinculo.Turma?.Nome : null,
            Progresso = ProgressoSePacote(vinculo, presencas),
        };

        private static DetalheCobrancaResponse DetalheDoVinculo(VinculoCobranca vinculo, IReadOnlyDictionary<int, int> presencas) => new()
        {
            Categoria = vinculo.Modalidade.ToString(),
            TurmaNome = vinculo.Turma?.Nome,
            Valor = vinculo.Valor,
            Progresso = ProgressoSePacote(vinculo, presencas),
        };

        private static ProgressoPacoteResponse? ProgressoSePacote(VinculoCobranca vinculo, IReadOnlyDictionary<int, int> presencas) =>
            vinculo.Modalidade == ModalidadeCobranca.Pacote
                ? ProgressoPacote.Calcular(vinculo, presencas.GetValueOrDefault(vinculo.Id))
                : null;

        // Pacote com o saldo zerado: o selo mostra "esgotado" mesmo que o total informado ainda aponte aulas
        // restantes (o que bloqueia a presenca e o saldo, spec 041).
        private static SeloCobrancaResponse PacoteEsgotado(VinculoCobranca pacote, IReadOnlyDictionary<int, int> presencas)
        {
            var selo = SeloDoVinculo(pacote, presencas, incluirTurma: false);
            return selo with { Progresso = selo.Progresso! with { Estado = ProgressoPacote.Esgotado, AulasRestantes = 0 } };
        }

        private static SeloCobrancaResponse SeloSemVinculo(Aluno aluno) => new()
        {
            Categoria = SemVinculo,
            Valor = aluno.ValorAula,
        };
    }
}
