using SPI.Application.Aulas.Dtos;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Aulas
{
    // specs/055 (US2): aula "Realizada" aceita mudar so a descricao; os demais
    // campos continuam bloqueados (FR-008/FR-009).
    public class AulaServiceAtualizarRealizadaTests
    {
        private static readonly DateOnly Ontem = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);

        // Cria uma aula de turma retroativa (ja "Realizada") e devolve o request que a
        // reproduz exatamente, para cada teste mudar so um campo.
        private static async Task<(CenarioCadastroAula Cenario, int AulaId, AulaRequest Igual)> AulaRealizadaAsync()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            var request = new AulaRequest
            {
                Descricao = "Plano original",
                MateriaId = 1,
                TurmaId = CenarioCadastroAula.TurmaId,
                DataInicio = Ontem,
                HoraInicio = new TimeOnly(10, 0),
                HoraFim = new TimeOnly(11, 0),
                Presencas = new() { [1] = true },
            };
            var criada = await cenario.Servico.CadastrarAsync(1, request);
            return (cenario, criada.Id, request with { Presencas = null });
        }

        [Fact]
        public async Task Mudar_so_a_descricao_de_aula_realizada_e_aceito()
        {
            var (cenario, aulaId, igual) = await AulaRealizadaAsync();

            var resposta = await cenario.Servico.AtualizarAsync(aulaId, igual with { Descricao = "O que foi dado de verdade" });

            Assert.Equal("O que foi dado de verdade", resposta.Descricao);
            Assert.Equal("Realizada", resposta.Status);
        }

        public static IEnumerable<object[]> CamposBloqueados() => new[]
        {
            new object[] { (Func<AulaRequest, AulaRequest>)(r => r with { DataInicio = r.DataInicio.AddDays(-1) }) },
            new object[] { (Func<AulaRequest, AulaRequest>)(r => r with { HoraInicio = new TimeOnly(9, 0) }) },
            new object[] { (Func<AulaRequest, AulaRequest>)(r => r with { HoraFim = new TimeOnly(12, 0) }) },
            new object[] { (Func<AulaRequest, AulaRequest>)(r => r with { MateriaId = 2 }) },
            new object[] { (Func<AulaRequest, AulaRequest>)(r => r with { TurmaId = null, AlunoId = 1 }) },
        };

        [Theory]
        [MemberData(nameof(CamposBloqueados))]
        public async Task Mudar_outro_campo_de_aula_realizada_e_recusado(Func<AulaRequest, AulaRequest> alterar)
        {
            var (cenario, aulaId, igual) = await AulaRealizadaAsync();
            var salvamentosAntes = cenario.Aulas.Salvamentos;

            await Assert.ThrowsAsync<ConflitoException>(() => cenario.Servico.AtualizarAsync(aulaId, alterar(igual)));

            Assert.Equal(salvamentosAntes, cenario.Aulas.Salvamentos);
            Assert.Equal("Plano original", cenario.Aulas.Aulas.Single().Descricao);
        }

        [Fact]
        public async Task Aula_agendada_continua_aceitando_edicao_completa()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"));
            var amanha = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
            var request = new AulaRequest
            {
                MateriaId = 1, TurmaId = CenarioCadastroAula.TurmaId, DataInicio = amanha,
                HoraInicio = new TimeOnly(10, 0), HoraFim = new TimeOnly(11, 0),
            };
            var criada = await cenario.Servico.CadastrarAsync(1, request);

            var resposta = await cenario.Servico.AtualizarAsync(criada.Id,
                request with { DataInicio = amanha.AddDays(1), HoraInicio = new TimeOnly(14, 0), HoraFim = new TimeOnly(15, 0), Descricao = "Nova" });

            Assert.Equal(amanha.AddDays(1), resposta.DataInicio);
            Assert.Equal(new TimeOnly(14, 0), resposta.HoraInicio);
            Assert.Equal("Agendada", resposta.Status);
        }

        [Fact]
        public async Task Aula_individual_realizada_recusa_troca_de_aluno()
        {
            var cenario = new CenarioCadastroAula(CenarioCadastroAula.NovoAluno(1, "Ana"), CenarioCadastroAula.NovoAluno(2, "Bruno"));
            var request = new AulaRequest
            {
                MateriaId = 1, AlunoId = 1, DataInicio = Ontem,
                HoraInicio = new TimeOnly(10, 0), HoraFim = new TimeOnly(11, 0), Presencas = new() { [1] = true },
            };
            var criada = await cenario.Servico.CadastrarAsync(1, request);

            await Assert.ThrowsAsync<ConflitoException>(() =>
                cenario.Servico.AtualizarAsync(criada.Id, request with { AlunoId = 2, Presencas = null }));
        }
    }
}
