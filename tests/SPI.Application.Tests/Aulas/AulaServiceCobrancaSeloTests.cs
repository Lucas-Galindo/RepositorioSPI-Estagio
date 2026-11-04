using SPI.Application.Aulas.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/068 (US3): nos detalhes de UMA aula, cada aluno traz o selo com o vinculo que VALE para a aula
    // (turma da aula; aula individual = aulas individuais), pela mesma regra que cobra a presenca. A lista
    // de aulas nao calcula o selo.
    public class AulaServiceCobrancaSeloTests
    {
        private const int MatriculadoId = 1;
        private const int VisitanteId = 2;
        private const int TurmaDaAula = CenarioCadastroAula.TurmaId;
        private const int OutraTurma = 20;

        private readonly CenarioCadastroAula _cenario;

        public AulaServiceCobrancaSeloTests()
        {
            _cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(MatriculadoId, "Ana"));

            var visitante = CenarioCadastroAula.NovoAluno(VisitanteId, "Bruno");
            _cenario.AlunosRepo.Alunos[VisitanteId] = visitante;
            _cenario.Aulas.AlunosConhecidos[VisitanteId] = visitante;
        }

        private VinculoCobranca Vinculo(int alunoId, ModalidadeCobranca modalidade, int? turmaId = null, int? saldo = null, int? total = null)
        {
            var vinculo = new VinculoCobranca
            {
                Id = _cenario.Vinculos.Vinculos.Count + 1, AlunoId = alunoId, Modalidade = modalidade, TurmaId = turmaId,
                SaldoAulas = saldo, TotalAulasPacote = total, Valor = 300m, Ativo = true,
            };
            _cenario.Vinculos.Vinculos.Add(vinculo);
            return vinculo;
        }

        private static AulaRequest Pedido(bool deTurma = true) => new()
        {
            MateriaId = 1,
            TurmaId = deTurma ? TurmaDaAula : null,
            AlunoId = deTurma ? null : MatriculadoId,
            DataInicio = DateOnly.FromDateTime(DateTime.Now).AddDays(7),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
        };

        private async Task<int> NovaAulaAsync(bool deTurma = true) => (await _cenario.Servico.CadastrarAsync(1, Pedido(deTurma))).Id;

        private static SPI.Application.VinculosCobranca.Dtos.SeloCobrancaResponse? Selo(AulaResponse aula, int alunoId) =>
            aula.Alunos.Single(a => a.AlunoId == alunoId).Cobranca;

        [Fact]
        public async Task Obter_traz_o_selo_do_pacote_da_turma_da_aula_com_as_aulas_restantes()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 8, total: 10);
            var vinculoId = _cenario.Vinculos.Vinculos.Single().Id;
            _cenario.Aulas.PresencasPorVinculo[vinculoId] = 2;
            var aulaId = await NovaAulaAsync();

            var selo = Selo(await _cenario.ServicoComCobranca.ObterPorIdAsync(aulaId), MatriculadoId)!;

            Assert.Equal("Pacote", selo.Categoria);
            Assert.Equal(8, selo.Progresso!.AulasRestantes);
        }

        [Fact]
        public async Task Aula_individual_usa_o_pacote_de_aulas_individuais_e_nao_o_da_turma()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 8, total: 10);
            Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, null, saldo: 3, total: 5);
            var aulaId = await NovaAulaAsync(deTurma: false);

            var selo = Selo(await _cenario.ServicoComCobranca.ObterPorIdAsync(aulaId), MatriculadoId)!;

            Assert.Equal("Pacote", selo.Categoria);
            Assert.Equal(5, selo.Progresso!.Total);
        }

        [Fact]
        public async Task Pacote_esgotado_sem_avulsa_vira_pacote_esgotado_e_com_avulsa_vira_avulsa_com_aviso()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Pacote, TurmaDaAula, saldo: 0, total: 10);
            var aulaId = await NovaAulaAsync();

            var esgotado = Selo(await _cenario.ServicoComCobranca.ObterPorIdAsync(aulaId), MatriculadoId)!;
            Assert.Equal("Pacote", esgotado.Categoria);
            Assert.Equal("Esgotado", esgotado.Progresso!.Estado);

            Vinculo(MatriculadoId, ModalidadeCobranca.Avulsa, TurmaDaAula);
            var comAvulsa = Selo(await _cenario.ServicoComCobranca.ObterPorIdAsync(aulaId), MatriculadoId)!;
            Assert.Equal("Avulsa", comAvulsa.Categoria);
            Assert.True(comAvulsa.AvisoPacoteEsgotado);
        }

        [Fact]
        public async Task Aluno_de_reposicao_com_pacote_de_outra_turma_nao_recebe_o_pacote()
        {
            Vinculo(VisitanteId, ModalidadeCobranca.Pacote, OutraTurma, saldo: 5, total: 10);
            var aulaId = await NovaAulaAsync();

            var resposta = await _cenario.ServicoComCobranca.AdicionarParticipanteAsync(aulaId, VisitanteId);

            var selo = Selo(resposta, VisitanteId)!;
            Assert.Equal("SemVinculo", selo.Categoria);
            Assert.Equal(80m, selo.Valor);
        }

        [Fact]
        public async Task Os_retornos_de_uma_aula_so_tambem_preenchem_o_selo()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Mensalidade);
            var aulaId = await NovaAulaAsync();
            var servico = _cenario.ServicoComCobranca;

            Assert.Equal("Mensalidade", Selo(await servico.AdicionarParticipanteAsync(aulaId, VisitanteId), MatriculadoId)!.Categoria);
            Assert.NotNull(Selo(await servico.AtualizarAsync(aulaId, Pedido()), MatriculadoId));
            Assert.NotNull(Selo(await servico.RegistrarSessaoAsync(aulaId, new RegistrarSessaoRequest { Presencas = { [MatriculadoId] = true, [VisitanteId] = true } }), MatriculadoId));

            // Uma aula ja realizada nao pode ser cancelada: o cancelamento e testado em outra aula.
            var outraAulaId = await NovaAulaAsync();
            Assert.NotNull(Selo(await servico.CancelarAsync(outraAulaId), MatriculadoId));
        }

        [Fact]
        public async Task A_lista_de_aulas_nao_calcula_o_selo()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Mensalidade);
            await NovaAulaAsync();

            var lista = await _cenario.ServicoComCobranca.ListarAsync(null, null, null, null, null);

            Assert.All(lista.SelectMany(a => a.Alunos), a => Assert.Null(a.Cobranca));
        }

        [Fact]
        public async Task Sem_o_servico_de_resumo_o_selo_fica_nulo_e_a_aula_funciona()
        {
            Vinculo(MatriculadoId, ModalidadeCobranca.Mensalidade);
            var aulaId = await NovaAulaAsync();

            var resposta = await _cenario.Servico.ObterPorIdAsync(aulaId);

            Assert.Single(resposta.Alunos);
            Assert.Null(resposta.Alunos[0].Cobranca);
        }
    }
}
