using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SPI.Application.Alunos.Services;
using SPI.Application.Aulas.Services;
using SPI.Application.Auth.Services;
using SPI.Application.ContasPagar.Services;
using SPI.Application.Dashboard.Services;
using SPI.Application.Financeiro.Services;
using SPI.Application.Lembretes.Services;
using SPI.Application.Materias.Services;
using SPI.Application.Pagamentos.Services;
using SPI.Application.Professores.Services;
using SPI.Application.Relatorios.Services;
using SPI.Application.Turmas.Services;

namespace SPI.Application.DependencyInjection
{
    public static class ApplicationServiceCollectionExtensions
    {
        /// <summary>
        /// Registra os validators FluentValidation e os casos de uso da Application.
        /// </summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

            services.AddScoped<IAutenticacaoService, AutenticacaoService>();
            services.AddScoped<IRecuperacaoSenhaService, RecuperacaoSenhaService>();
            services.AddScoped<IProfessorService, ProfessorService>();
            services.AddScoped<IMateriaService, MateriaService>();
            services.AddScoped<ITurmaService, TurmaService>();
            services.AddScoped<IAlunoService, AlunoService>();
            services.AddScoped<IAulaService, AulaService>();
            services.AddScoped<IPagamentoService, PagamentoService>();
            services.AddScoped<IContaPagarService, ContaPagarService>();
            services.AddScoped<ILembreteService, LembreteService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IFinanceiroService, FinanceiroService>();
            services.AddScoped<IRelatorioService, RelatorioService>();

            return services;
        }
    }
}
