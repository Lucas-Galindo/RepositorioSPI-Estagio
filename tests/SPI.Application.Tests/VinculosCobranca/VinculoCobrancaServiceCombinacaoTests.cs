using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/056: regras de combinacao de vinculos ativos do mesmo aluno -- mensalidade
    // exclusiva, um pacote por vez, avulsa por contexto -- no cadastro, na edicao e na
    // reativacao. Toda recusa deixa os vinculos exatamente como estavam.
    public class VinculoCobrancaServiceCombinacaoTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;

        private const string MsgMensalidadeComOutros = "O aluno ja tem outros vinculos ativos. Desative-os antes de cadastrar a mensalidade, que cobre todas as aulas.";
        private const string MsgJaTemMensalidade = "O aluno tem mensalidade ativa, que ja cobre todas as aulas.";
        private const string MsgJaTemPacote = "O aluno ja tem um pacote ativo.";

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeTurmaRepositoryParaVinculo _turmas = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceCombinacaoTests()
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            var turma = new Turma { Id = TurmaId, Nome = "Turma A", Ativo = true };
            turma.AlunosTurma.Add(new AlunoTurma { AlunoId = AlunoId, TurmaId = TurmaId, Turma = turma });
            _turmas.Turmas.Add(turma);
            _vinculos.TurmasConhecidas.Add(turma);
            _servico = new VinculoCobrancaService(_vinculos, alunos, _turmas, new FakeAulaRepositoryParaVinculo());
        }

        private VinculoCobranca Semear(ModalidadeCobranca modalidade, int? turmaId = null, bool ativo = true) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, TurmaId = turmaId, Modalidade = modalidade, Valor = 100m, Ativo = ativo });

        private static VinculoCobrancaRequest Request(ModalidadeCobranca modalidade, int? turmaId = null) =>
            new() { Modalidade = modalidade, TurmaId = turmaId, Valor = 100m };

        private async Task AssertRecusaAsync(Func<Task> acao, string mensagem)
        {
            var antes = _vinculos.Vinculos.Select(v => (v.Id, v.Modalidade, v.TurmaId, v.Ativo)).ToList();

            var excecao = await Assert.ThrowsAsync<ConflitoException>(acao);

            Assert.Equal(mensagem, excecao.Message);
            Assert.Equal(antes, _vinculos.Vinculos.Select(v => (v.Id, v.Modalidade, v.TurmaId, v.Ativo)).ToList());
        }

        // ---------- Mensalidade exclusiva (US1) ----------

        [Theory]
        [InlineData(ModalidadeCobranca.Pacote)]
        [InlineData(ModalidadeCobranca.Avulsa)]
        public async Task Cadastrar_mensalidade_com_outro_vinculo_ativo_e_recusado(ModalidadeCobranca existente)
        {
            Semear(existente);

            await AssertRecusaAsync(() => _servico.CadastrarAsync(AlunoId, Request(ModalidadeCobranca.Mensalidade)), MsgMensalidadeComOutros);
        }

        [Fact]
        public async Task Reativar_mensalidade_com_outro_vinculo_ativo_e_recusado()
        {
            var mensalidade = Semear(ModalidadeCobranca.Mensalidade, ativo: false);
            Semear(ModalidadeCobranca.Avulsa, TurmaId);

            await AssertRecusaAsync(() => _servico.ReativarAsync(AlunoId, mensalidade.Id), MsgMensalidadeComOutros);
        }

        [Theory]
        [InlineData(ModalidadeCobranca.Pacote, null)]
        [InlineData(ModalidadeCobranca.Avulsa, TurmaId)]
        [InlineData(ModalidadeCobranca.Avulsa, null)]
        public async Task Pacote_ou_avulsa_com_mensalidade_ativa_e_recusado(ModalidadeCobranca modalidade, int? turmaId)
        {
            Semear(ModalidadeCobranca.Mensalidade);

            await AssertRecusaAsync(() => _servico.CadastrarAsync(AlunoId, Request(modalidade, turmaId)), MsgJaTemMensalidade);
        }

        [Fact]
        public async Task Reativar_pacote_com_mensalidade_ativa_e_recusado()
        {
            var pacote = Semear(ModalidadeCobranca.Pacote, ativo: false);
            Semear(ModalidadeCobranca.Mensalidade);

            await AssertRecusaAsync(() => _servico.ReativarAsync(AlunoId, pacote.Id), MsgJaTemMensalidade);
        }

        [Fact]
        public async Task Editar_avulsa_para_mensalidade_com_outros_ativos_e_recusado()
        {
            var avulsa = Semear(ModalidadeCobranca.Avulsa, TurmaId);
            Semear(ModalidadeCobranca.Pacote);

            await AssertRecusaAsync(() => _servico.AtualizarAsync(AlunoId, avulsa.Id, Request(ModalidadeCobranca.Mensalidade)), MsgMensalidadeComOutros);
        }

        [Fact]
        public async Task Editar_unica_avulsa_para_mensalidade_e_permitido()
        {
            var avulsa = Semear(ModalidadeCobranca.Avulsa, TurmaId);

            var resposta = await _servico.AtualizarAsync(AlunoId, avulsa.Id, Request(ModalidadeCobranca.Mensalidade));

            Assert.Equal(ModalidadeCobranca.Mensalidade, resposta.Modalidade);
            Assert.Null(resposta.TurmaId);
        }

        // ---------- Um pacote por aluno; pacote convive com avulsa (US2) ----------

        [Fact]
        public async Task Cadastrar_segundo_pacote_ativo_e_recusado()
        {
            Semear(ModalidadeCobranca.Pacote);

            await AssertRecusaAsync(() => _servico.CadastrarAsync(AlunoId, Request(ModalidadeCobranca.Pacote)), MsgJaTemPacote);
        }

        [Fact]
        public async Task Reativar_segundo_pacote_e_recusado()
        {
            var antigo = Semear(ModalidadeCobranca.Pacote, ativo: false);
            Semear(ModalidadeCobranca.Pacote);

            await AssertRecusaAsync(() => _servico.ReativarAsync(AlunoId, antigo.Id), MsgJaTemPacote);
        }

        [Fact]
        public async Task Pacote_convive_com_avulsas_de_turma_e_individual()
        {
            Semear(ModalidadeCobranca.Avulsa, TurmaId);
            Semear(ModalidadeCobranca.Avulsa, null);

            var resposta = await _servico.CadastrarAsync(AlunoId, Request(ModalidadeCobranca.Pacote));

            Assert.True(resposta.Ativo);
            Assert.Equal(3, _vinculos.Vinculos.Count(v => v.Ativo));
        }
    }
}
