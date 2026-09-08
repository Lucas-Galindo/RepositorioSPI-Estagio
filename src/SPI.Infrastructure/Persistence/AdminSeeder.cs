using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SPI.Application.Common;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence
{
    // Nao ha endpoint publico de cadastro de Admin (ver prompt mestre, secao
    // "Fluxos de autenticacao"). O primeiro admin e criado aqui, a partir de
    // AdminSeed:Email/AdminSeed:Senha configurados via User Secrets - nunca
    // uma senha fixa no codigo. Se as chaves nao estiverem configuradas, ou
    // se ja existir um admin com esse e-mail, nao faz nada.
    public static class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var email = configuration["AdminSeed:Email"];
            var senha = configuration["AdminSeed:Senha"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SpiDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Admin>>();

            var jaExiste = await dbContext.Admins.AnyAsync(a => a.Email == email);
            if (jaExiste)
            {
                return;
            }

            dbContext.Admins.Add(new Admin
            {
                Nome = configuration["AdminSeed:Nome"] ?? "Administrador",
                Email = email,
                Senha = passwordHasher.Hash(senha),
                Ativo = true
            });
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Admin inicial criado via seed para {Email}", email);
        }
    }
}
