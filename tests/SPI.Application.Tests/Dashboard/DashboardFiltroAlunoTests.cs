using System.Reflection;
using SPI.Application.Dashboard.Services;
using SPI.Domain.Repositories;
using Xunit;

namespace SPI.Application.Tests.Dashboard
{
    // specs/071 (US2/FR-003/FR-006): o alunoId recebido pelo dashboard chega aos dois calculos dos cards
    // ("Alunos atendidos" e "Recebidos no periodo"), e sem aluno chega nulo (comportamento de antes).
    // Em vez de um fake de dezenas de metodos, um proxy dinamico implementa as interfaces do repositorio,
    // devolve valores vazios e registra os argumentos de cada chamada.
    public class DashboardFiltroAlunoTests
    {
        public class GravadorProxy : DispatchProxy
        {
            public List<(string Metodo, object?[] Args)> Chamadas { get; } = new();

            protected override object? Invoke(MethodInfo? metodo, object?[]? args)
            {
                Chamadas.Add((metodo!.Name, args ?? Array.Empty<object?>()));
                return ValorVazio(metodo.ReturnType);
            }

            private static object? ValorVazio(Type tipo)
            {
                if (tipo == typeof(Task)) return Task.CompletedTask;
                if (tipo.IsGenericType && tipo.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var interno = tipo.GetGenericArguments()[0];
                    return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(interno).Invoke(null, new[] { ValorVazio(interno) });
                }
                if (tipo.IsValueType) return Activator.CreateInstance(tipo);
                if (tipo == typeof(string)) return string.Empty;
                if (tipo.IsGenericType)
                {
                    var definicao = tipo.GetGenericTypeDefinition();
                    var argumentos = tipo.GetGenericArguments();
                    if (definicao == typeof(List<>) || definicao == typeof(IList<>) || definicao == typeof(IReadOnlyList<>)
                        || definicao == typeof(ICollection<>) || definicao == typeof(IReadOnlyCollection<>) || definicao == typeof(IEnumerable<>))
                        return Activator.CreateInstance(typeof(List<>).MakeGenericType(argumentos));
                    if (definicao == typeof(Dictionary<,>) || definicao == typeof(IDictionary<,>) || definicao == typeof(IReadOnlyDictionary<,>))
                        return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(argumentos));
                }
                return null;
            }
        }

        private static T Novo<T>() where T : class => DispatchProxy.Create<T, GravadorProxy>();

        private static List<(string Metodo, object?[] Args)> Chamadas(object proxy) => ((GravadorProxy)proxy).Chamadas;

        private static async Task<List<(string Metodo, object?[] Args)>> ExecutarAsync(DateOnly? inicio, DateOnly? fim, int? alunoId)
        {
            var relatorio = Novo<IRelatorioRepository>();
            var servico = new DashboardService(relatorio, Novo<IVinculoCobrancaRepository>(), Novo<IAulaRepository>());

            await servico.ObterAsync(inicio, fim, alunoId);

            return Chamadas(relatorio);
        }

        private static object?[] ArgsDe(List<(string Metodo, object?[] Args)> chamadas, string metodo) =>
            chamadas.Single(c => c.Metodo == metodo).Args;

        [Fact]
        public async Task Com_aluno_o_alunoId_chega_a_contagem_de_atendidos_e_ao_valor_recebido()
        {
            var chamadas = await ExecutarAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), alunoId: 7);

            // ContarAlunosAtendidosNoPeriodoAsync(inicio, fim, cancellationToken, alunoId)
            Assert.Equal(7, (int?)ArgsDe(chamadas, "ContarAlunosAtendidosNoPeriodoAsync")[3]);
            // ObterValorFaturadoNoPeriodoAsync(inicio, fim, cancellationToken, turmaId, materiaId, alunoId, ...)
            var valor = ArgsDe(chamadas, "ObterValorFaturadoNoPeriodoAsync");
            Assert.Equal(7, (int?)valor[5]);
            Assert.Null((int?)valor[3]);
            Assert.Null((int?)valor[4]);
        }

        [Fact]
        public async Task Sem_aluno_os_dois_calculos_recebem_nulo()
        {
            var chamadas = await ExecutarAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), alunoId: null);

            Assert.Null((int?)ArgsDe(chamadas, "ContarAlunosAtendidosNoPeriodoAsync")[3]);
            Assert.Null((int?)ArgsDe(chamadas, "ObterValorFaturadoNoPeriodoAsync")[5]);
        }

        [Fact]
        public async Task Os_dois_calculos_usam_o_mesmo_periodo_resolvido()
        {
            // So o "ate": o periodo comeca no 1o dia do mes do "ate".
            var chamadas = await ExecutarAsync(null, new DateOnly(2026, 9, 15), alunoId: null);

            var atendidos = ArgsDe(chamadas, "ContarAlunosAtendidosNoPeriodoAsync");
            var valor = ArgsDe(chamadas, "ObterValorFaturadoNoPeriodoAsync");
            Assert.Equal(new DateOnly(2026, 9, 1), (DateOnly)atendidos[0]!);
            Assert.Equal(new DateOnly(2026, 9, 15), (DateOnly)atendidos[1]!);
            Assert.Equal(new DateOnly(2026, 9, 1), (DateOnly)valor[0]!);
            Assert.Equal(new DateOnly(2026, 9, 15), (DateOnly)valor[1]!);
        }
    }
}
