using SPI.Application.Aulas.Dtos;
using SPI.Application.Aulas.Services;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/052 (US2): toda presenca confirmada com Vinculo de Cobranca
    // correspondente ao contexto fica associada ao Id desse vinculo em
    // AulaAluno.VinculoCobrancaId -- para Avulsa, Mensalidade e Pacote, nao
    // so Pacote. Reaproveita os mesmos fakes de specs/038.
    public class AulaServiceAuditoriaTests
    {
        private const int AlunoId = 1;
        private const int TurmaId = 10;
        private const int OutraTurmaId = 20;

        private static Aluno NovoAluno(decimal valorAula = 80m) =>
            new() { Id = AlunoId, Nome = "Ana", Ativo = true, ValorAula = valorAula };

        private static Materia NovaMateria() => new() { Id = 1, Nome = "Matematica", Ativo = true };

        private static Aula NovaAula(int id, Aluno aluno, Materia materia, int? turmaId = TurmaId) => new()
        {
            Id = id,
            MateriaId = materia.Id,
            Materia = materia,
            ProfessorId = 1,
            TurmaId = turmaId,
            Status = "Agendada",
            DataInicio = new DateOnly(2026, 9, 15).AddDays(id),
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(11, 0),
            Ativo = true,
            AulaAlunos = new List<AulaAluno>
            {
                new() { AulaId = id, AlunoId = aluno.Id, Aluno = aluno, Presente = null }
            }
        };

        private static AulaService CriarServico(
            FakeAulaRepositoryParaAula aulas, FakePagamentoRepositoryParaAula pagamentos, FakeVinculoCobrancaRepositoryParaAula vinculos) =>
            new(
                aulas,
                new FakeAlunoRepositoryVazio(),
                new FakeTurmaRepositoryVazia(),
                new FakeMateriaRepositoryVazia(),
                new FakeLembreteServiceVazio(),
                pagamentos,
                new FakeCategoriaReceitaRepositoryParaAula
                {
                    AulaEmTurma = new CategoriaReceita { Id = 1, Nome = "Aula em turma", Ativo = true },
                    AulaParticular = new CategoriaReceita { Id = 2, Nome = "Aula particular", Ativo = true },
                },
                vinculos);

        [Theory]
        [InlineData(ModalidadeCobranca.Avulsa)]
        [InlineData(ModalidadeCobranca.Mensalidade)]
        [InlineData(ModalidadeCobranca.Pacote)]
        public async Task Presenca_confirmada_associa_o_vinculo_ativo_do_contexto(ModalidadeCobranca modalidade)
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca
            {
                Id = 42, AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = modalidade, Valor = 100m,
                SaldoAulas = modalidade == ModalidadeCobranca.Pacote ? 5 : null,
                AulasIncluidas = modalidade == ModalidadeCobranca.Mensalidade ? 10 : null,
                Ativo = true,
            };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Equal(42, aula.AulaAlunos.Single().VinculoCobrancaId);
        }

        [Fact]
        public async Task Presenca_confirmada_sem_vinculo_correspondente_nao_associa_nada()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Null(aula.AulaAlunos.Single().VinculoCobrancaId);
        }

        [Fact]
        public async Task Presenca_confirmada_com_vinculo_de_outro_contexto_nao_associa_nada()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia, turmaId: TurmaId);
            var vinculoDeOutraTurma = new VinculoCobranca { Id = 7, AlunoId = AlunoId, TurmaId = OutraTurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 100m, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculoDeOutraTurma);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Null(aula.AulaAlunos.Single().VinculoCobrancaId);
        }

        [Fact]
        public async Task Falta_nunca_associa_vinculo_mesmo_havendo_um_ativo()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca { Id = 9, AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Avulsa, Valor = 100m, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = false } });

            Assert.Null(aula.AulaAlunos.Single().VinculoCobrancaId);
        }

        [Fact]
        public async Task Troca_de_pacote_mantem_presencas_antigas_ligadas_ao_vinculo_antigo()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var vinculoA = new VinculoCobranca { Id = 1, AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 1, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculoA);
            var pagamentos = new FakePagamentoRepositoryParaAula();

            // Primeira presenca: consome o ultimo saldo do vinculo A.
            var aula1 = NovaAula(1, aluno, materia);
            await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula1 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula1.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });
            Assert.Equal(1, aula1.AulaAlunos.Single().VinculoCobrancaId);
            Assert.Equal(0, vinculoA.SaldoAulas);

            // Vinculo A esgotado e desativado; vinculo B (novo pacote) ativado.
            vinculoA.Ativo = false;
            var vinculoB = new VinculoCobranca { Id = 2, AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 10, Ativo = true };
            vinculos.Vinculos.Add(vinculoB);

            var aula2 = NovaAula(2, aluno, materia);
            await CriarServico(new FakeAulaRepositoryParaAula { AulaSemeada = aula2 }, pagamentos, vinculos)
                .RegistrarSessaoAsync(aula2.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Equal(1, aula1.AulaAlunos.Single().VinculoCobrancaId); // presenca antiga nunca muda
            Assert.Equal(2, aula2.AulaAlunos.Single().VinculoCobrancaId); // presenca nova vai pro vinculo novo
        }

        // specs/052 (FR-002, cobertura explicita achado E1 do /speckit-analyze):
        // a presenca nunca decrementa TotalAulasPacote, so SaldoAulas.
        [Fact]
        public async Task Presenca_em_pacote_nunca_decrementa_total_aulas_pacote()
        {
            var aluno = NovoAluno();
            var materia = NovaMateria();
            var aula = NovaAula(1, aluno, materia);
            var vinculo = new VinculoCobranca { Id = 1, AlunoId = AlunoId, TurmaId = TurmaId, Modalidade = ModalidadeCobranca.Pacote, Valor = 500m, SaldoAulas = 10, TotalAulasPacote = 10, Ativo = true };
            var vinculos = new FakeVinculoCobrancaRepositoryParaAula();
            vinculos.Vinculos.Add(vinculo);
            var aulas = new FakeAulaRepositoryParaAula { AulaSemeada = aula };
            var pagamentos = new FakePagamentoRepositoryParaAula();
            var servico = CriarServico(aulas, pagamentos, vinculos);

            await servico.RegistrarSessaoAsync(aula.Id, new RegistrarSessaoRequest { Presencas = { [AlunoId] = true } });

            Assert.Equal(10, vinculo.TotalAulasPacote);
            Assert.Equal(9, vinculo.SaldoAulas);
        }
    }
}
