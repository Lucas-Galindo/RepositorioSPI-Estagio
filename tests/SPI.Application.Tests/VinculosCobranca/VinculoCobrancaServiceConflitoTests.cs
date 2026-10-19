using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/056 (US4): combinacoes herdadas da regra antiga sao so sinalizadas na
    // listagem, nunca resolvidas automaticamente (FR-020/FR-021).
    public class VinculoCobrancaServiceConflitoTests
    {
        private const int AlunoId = 1;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceConflitoTests()
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            _servico = new VinculoCobrancaService(_vinculos, alunos, new FakeTurmaRepositoryParaVinculo(), new FakeAulaRepositoryParaVinculo());
        }

        private VinculoCobranca Semear(ModalidadeCobranca modalidade, bool ativo = true, int? turmaId = null) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = AlunoId, Modalidade = modalidade, TurmaId = turmaId, Valor = 100m, Ativo = ativo });

        [Fact]
        public async Task Mensalidade_com_pacote_ativo_marca_os_dois_sem_alterar_nada()
        {
            var mensalidade = Semear(ModalidadeCobranca.Mensalidade);
            var pacote = Semear(ModalidadeCobranca.Pacote);

            var lista = await _servico.ListarPorAlunoAsync(AlunoId, ativo: true);

            Assert.All(lista, v => Assert.Equal("Mensalidade nao pode coexistir com outros vinculos ativos.", v.Conflito));
            Assert.True(mensalidade.Ativo);
            Assert.True(pacote.Ativo);
        }

        [Fact]
        public async Task Dois_pacotes_ativos_marcam_so_os_pacotes()
        {
            Semear(ModalidadeCobranca.Pacote);
            Semear(ModalidadeCobranca.Pacote);
            var avulsa = Semear(ModalidadeCobranca.Avulsa, turmaId: 10);

            var lista = await _servico.ListarPorAlunoAsync(AlunoId, ativo: true);

            Assert.All(lista.Where(v => v.Modalidade == ModalidadeCobranca.Pacote), v => Assert.Equal("Mais de um pacote ativo.", v.Conflito));
            Assert.Null(lista.Single(v => v.Id == avulsa.Id).Conflito);
        }

        [Fact]
        public async Task Combinacao_permitida_nao_tem_conflito()
        {
            Semear(ModalidadeCobranca.Pacote);
            Semear(ModalidadeCobranca.Avulsa, turmaId: 10);
            Semear(ModalidadeCobranca.Avulsa);

            var lista = await _servico.ListarPorAlunoAsync(AlunoId, ativo: null);

            Assert.All(lista, v => Assert.Null(v.Conflito));
        }

        [Fact]
        public async Task Vinculos_inativos_nunca_sao_marcados()
        {
            Semear(ModalidadeCobranca.Mensalidade);
            var pacoteInativo = Semear(ModalidadeCobranca.Pacote, ativo: false);
            var mensalidadeInativa = Semear(ModalidadeCobranca.Mensalidade, ativo: false);

            var lista = await _servico.ListarPorAlunoAsync(AlunoId, ativo: null);

            Assert.All(lista, v => Assert.Null(v.Conflito));
            Assert.Contains(lista, v => v.Id == pacoteInativo.Id);
            Assert.Contains(lista, v => v.Id == mensalidadeInativa.Id);
        }
    }
}
