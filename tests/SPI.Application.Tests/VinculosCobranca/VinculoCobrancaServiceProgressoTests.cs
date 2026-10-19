using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/058 (US1/US2): progresso do pacote na ficha do aluno, saldo inicial
    // no cadastro e a lista de aulas que contaram como dadas.
    public class VinculoCobrancaServiceProgressoTests
    {
        private const int AlunoId = 1;
        private const int OutroAlunoId = 2;

        private readonly FakeVinculoCobrancaRepositoryParaTeste _vinculos = new();
        private readonly FakeAulaRepositoryParaVinculo _aulas = new();
        private readonly VinculoCobrancaService _servico;

        public VinculoCobrancaServiceProgressoTests()
        {
            var alunos = new FakeAlunoRepositoryParaVinculo();
            alunos.Alunos.Add(new Aluno { Id = AlunoId, Nome = "Ana", Ativo = true });
            alunos.Alunos.Add(new Aluno { Id = OutroAlunoId, Nome = "Bia", Ativo = true });
            _servico = new VinculoCobrancaService(_vinculos, alunos, new FakeTurmaRepositoryParaVinculo(), _aulas);
        }

        private VinculoCobranca SemearPacote(int total, int alunoId = AlunoId, bool ativo = true) =>
            _vinculos.Semear(new VinculoCobranca { AlunoId = alunoId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, TotalAulasPacote = total, SaldoAulas = total, Ativo = ativo });

        private void Presencas(int vinculoId, int quantidade)
        {
            for (var i = 0; i < quantidade; i++)
            {
                _aulas.Presencas.Add((vinculoId, 2026, 10));
            }
        }

        [Fact]
        public async Task Cadastro_de_pacote_so_com_total_comeca_com_saldo_cheio()
        {
            var resposta = await _servico.CadastrarAsync(AlunoId, new VinculoCobrancaRequest { Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, TotalAulasPacote = 10 });

            Assert.Equal(10, resposta.SaldoAulas);
            Assert.Equal(10, resposta.TotalAulasPacote);
            Assert.Equal(0, resposta.Progresso!.AulasDadas);
            Assert.Equal(10, resposta.Progresso.AulasRestantes);
        }

        [Fact]
        public async Task Cadastro_com_saldo_informado_mantem_o_saldo()
        {
            var resposta = await _servico.CadastrarAsync(AlunoId, new VinculoCobrancaRequest { Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, TotalAulasPacote = 10, SaldoAulas = 6 });

            Assert.Equal(6, resposta.SaldoAulas);
        }

        // A listagem conta cada pacote pelas proprias presencas, inclusive os inativos.
        [Fact]
        public async Task Listagem_traz_o_progresso_de_cada_pacote_ativo_ou_nao()
        {
            var antigo = SemearPacote(4, ativo: false);
            var atual = SemearPacote(10);
            Presencas(antigo.Id, 4);
            Presencas(atual.Id, 3);

            var lista = await _servico.ListarPorAlunoAsync(AlunoId, ativo: null);

            var progressoAntigo = lista.Single(v => v.Id == antigo.Id).Progresso!;
            Assert.Equal(ProgressoPacote.Esgotado, progressoAntigo.Estado);
            var progressoAtual = lista.Single(v => v.Id == atual.Id).Progresso!;
            Assert.Equal(3, progressoAtual.AulasDadas);
            Assert.Equal(7, progressoAtual.AulasRestantes);
        }

        [Fact]
        public async Task Lista_de_aulas_do_pacote_traz_as_presencas_com_contexto()
        {
            var pacote = SemearPacote(10);
            var materia = new Materia { Id = 1, Nome = "Matematica", Ativo = true };
            _aulas.PresencasComAula.Add(new AulaAluno
            {
                AlunoId = AlunoId, VinculoCobrancaId = pacote.Id, Presente = true,
                Aula = new Aula { Id = 5, DataInicio = new DateOnly(2026, 10, 1), HoraInicio = new TimeOnly(9, 0), HoraFim = new TimeOnly(10, 0), Status = "Realizada", Materia = materia, Turma = new Turma { Id = 3, Nome = "Turma A" } },
                AulaId = 5,
            });
            _aulas.PresencasComAula.Add(new AulaAluno
            {
                AlunoId = AlunoId, VinculoCobrancaId = pacote.Id, Presente = true,
                Aula = new Aula { Id = 6, DataInicio = new DateOnly(2026, 10, 8), HoraInicio = new TimeOnly(9, 0), HoraFim = new TimeOnly(10, 0), Status = "Realizada", Materia = materia },
                AulaId = 6,
            });

            var aulas = await _servico.ListarAulasDoPacoteAsync(AlunoId, pacote.Id);

            Assert.Equal(new[] { 6, 5 }, aulas.Select(a => a.AulaId));
            Assert.Equal("Atendimento individual", aulas[0].Contexto);
            Assert.Equal("Turma A", aulas[1].Contexto);
            Assert.Equal("Realizada", aulas[1].Status);
        }

        [Fact]
        public async Task Aulas_de_pacote_de_outro_aluno_respondem_nao_encontrado()
        {
            var pacoteDaBia = SemearPacote(10, alunoId: OutroAlunoId);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => _servico.ListarAulasDoPacoteAsync(AlunoId, pacoteDaBia.Id));
        }
    }
}
