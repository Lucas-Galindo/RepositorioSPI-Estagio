using System.Reflection;
using SPI.Application.Alunos.Services;
using SPI.Application.Aulas.Services;
using SPI.Application.Pagamentos.Services;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.VinculosCobranca
{
    // specs/037, FR-012 (MUST NOT): VinculoCobranca existe apenas como cadastro e
    // NAO pode alterar a geracao automatica de cobranca
    // (AulaService.GerarContasAReceberAsync) nem o uso de Aluno.ValorAula.
    //
    // Estes testes guardam esse limite estruturalmente: se alguem ligar o vinculo
    // a cobranca automatica, eles falham -- o que so deve acontecer de forma
    // consciente, na proxima fatia (EX-001 em specs/037-vinculo-cobranca/plan.md,
    // Complexity Tracking), removendo/ajustando esta guarda no mesmo momento.
    public class VinculoCobrancaNaoAfetaCobrancaAutomaticaTests
    {
        private const BindingFlags Tudo = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static bool MencionaVinculoCobranca(Type tipo) =>
            tipo.FullName!.Contains("VinculoCobranca", StringComparison.Ordinal)
            || (tipo.IsGenericType && tipo.GetGenericArguments().Any(MencionaVinculoCobranca))
            || (tipo.HasElementType && MencionaVinculoCobranca(tipo.GetElementType()!));

        public static IEnumerable<object[]> ServicosDeCobranca() => new[]
        {
            new object[] { typeof(AulaService) },
            new object[] { typeof(AlunoService) },
            new object[] { typeof(PagamentoService) },
        };

        public static IEnumerable<object[]> RepositoriosDeCobranca() => new[]
        {
            new object[] { typeof(IAulaRepository) },
            new object[] { typeof(IPagamentoRepository) },
            new object[] { typeof(IAlunoRepository) },
        };

        [Theory]
        [MemberData(nameof(ServicosDeCobranca))]
        public void Servico_nao_depende_de_VinculoCobranca_no_construtor(Type servico)
        {
            var parametros = servico.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType);

            Assert.DoesNotContain(parametros, MencionaVinculoCobranca);
        }

        [Theory]
        [MemberData(nameof(ServicosDeCobranca))]
        public void Servico_nao_guarda_campo_nem_expoe_membro_que_referencie_VinculoCobranca(Type servico)
        {
            var campos = servico.GetFields(Tudo).Select(f => f.FieldType);
            var metodos = servico.GetMethods(Tudo).SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType));

            Assert.DoesNotContain(campos, MencionaVinculoCobranca);
            Assert.DoesNotContain(metodos, MencionaVinculoCobranca);
        }

        [Theory]
        [MemberData(nameof(RepositoriosDeCobranca))]
        public void Repositorio_nao_expoe_metodo_com_VinculoCobranca(Type repositorio)
        {
            var tipos = repositorio.GetMethods().SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType));

            Assert.DoesNotContain(tipos, MencionaVinculoCobranca);
        }

        [Fact]
        public void GerarContasAReceberAsync_continua_existindo_sem_referenciar_VinculoCobranca()
        {
            var metodo = typeof(AulaService).GetMethod("GerarContasAReceberAsync", Tudo);

            Assert.NotNull(metodo);
            Assert.DoesNotContain(
                metodo!.GetParameters().Select(p => p.ParameterType).Append(metodo.ReturnType),
                MencionaVinculoCobranca);
        }

        [Fact]
        public void Aluno_ValorAula_continua_sendo_decimal_e_sem_relacao_com_o_vinculo()
        {
            var propriedade = typeof(Aluno).GetProperty(nameof(Aluno.ValorAula));

            Assert.NotNull(propriedade);
            Assert.Equal(typeof(decimal), propriedade!.PropertyType);

            // O unico acoplamento permitido de Aluno com o vinculo e a colecao de navegacao (cadastro).
            var propriedadesComVinculo = typeof(Aluno).GetProperties().Where(p => MencionaVinculoCobranca(p.PropertyType)).ToList();
            Assert.Single(propriedadesComVinculo);
            Assert.Equal(nameof(Aluno.VinculosCobranca), propriedadesComVinculo[0].Name);
        }

        // Comportamento: cadastrar/editar/excluir um vinculo nunca toca em Aluno.ValorAula.
        [Fact]
        public async Task Operacoes_do_servico_de_vinculo_nao_alteram_o_ValorAula_do_aluno()
        {
            var vinculos = new FakeVinculoCobrancaRepositoryParaTeste();
            var turmas = new FakeTurmaRepositoryParaVinculo();
            var alunos = new FakeAlunoRepositoryParaVinculo();
            var aluno = new Aluno { Id = 1, Nome = "Ana", Ativo = true, ValorAula = 80m };
            alunos.Alunos.Add(aluno);
            var servico = new SPI.Application.VinculosCobranca.Services.VinculoCobrancaService(vinculos, alunos, turmas);

            var criado = await servico.CadastrarAsync(1, new SPI.Application.VinculosCobranca.Dtos.VinculoCobrancaRequest
            {
                TurmaId = null,
                Modalidade = SPI.Domain.Enums.ModalidadeCobranca.Pacote,
                Valor = 999m,
                SaldoAulas = 10,
            });
            await servico.AtualizarAsync(1, criado.Id, new SPI.Application.VinculosCobranca.Dtos.VinculoCobrancaRequest
            {
                TurmaId = null,
                Modalidade = SPI.Domain.Enums.ModalidadeCobranca.Avulsa,
                Valor = 123m,
            });
            await servico.ExcluirAsync(1, criado.Id);
            await servico.ReativarAsync(1, criado.Id);

            Assert.Equal(80m, aluno.ValorAula);
            Assert.Equal(0, aluno.Frequencia);
            Assert.Empty(aluno.Pagamentos);
        }
    }
}
