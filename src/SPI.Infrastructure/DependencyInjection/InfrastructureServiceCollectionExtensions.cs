using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPI.Application.Common;
using SPI.Domain.Repositories;
using SPI.Infrastructure.BackgroundServices;
using SPI.Infrastructure.Email;
using SPI.Infrastructure.Persistence;
using SPI.Infrastructure.Repositories;
using SPI.Infrastructure.Security;

namespace SPI.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Registra o DbContext apontando para o MySQL existente (spi_db),
        /// repositorios e servicos de autenticacao (hash de senha, JWT).
        /// Banco e "database first": nenhuma migration recria as tabelas
        /// definidas em database/01-07_*.sql.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SpiDb")
                ?? throw new InvalidOperationException("Connection string 'SpiDb' nao configurada. Defina-a via User Secrets ou variavel de ambiente.");

            // Versao fixa (nao AutoDetect): AutoDetect exigiria conexao com o banco
            // durante o boot da aplicacao so para descobrir a versao do servidor.
            // Stack fixa em MySQL 8 (ver prompt mestre), entao fixar aqui evita que
            // a API deixe de subir so porque o MySQL esta indisponivel no momento.
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 39));

            services.AddDbContext<SpiDbContext>(options =>
                options.UseMySql(connectionString, serverVersion));

            services.AddScoped<IProfessorRepository, ProfessorRepository>();
            services.AddScoped<IAlunoRepository, AlunoRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ISenhaResetTokenRepository, SenhaResetTokenRepository>();
            services.AddScoped<IMateriaRepository, MateriaRepository>();
            services.AddScoped<ITurmaRepository, TurmaRepository>();
            services.AddScoped<IAulaRepository, AulaRepository>();
            services.AddScoped<IPagamentoRepository, PagamentoRepository>();
            services.AddScoped<IFormaPagamentoRepository, FormaPagamentoRepository>();
            services.AddScoped<ICategoriaReceitaRepository, CategoriaReceitaRepository>();
            services.AddScoped<ICategoriaDespesaRepository, CategoriaDespesaRepository>();
            services.AddScoped<IContaPagarRepository, ContaPagarRepository>();
            services.AddScoped<ILembreteRepository, LembreteRepository>();
            services.AddScoped<IRelatorioRepository, RelatorioRepository>();

            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.AddSingleton<ITokenService, JwtTokenService>();

            services.Configure<BrevoOptions>(configuration.GetSection(BrevoOptions.SectionName));
            services.AddScoped<IEmailSender, BrevoEmailSender>();

            services.AddHostedService<LembreteDispatcherService>();

            return services;
        }
    }
}
