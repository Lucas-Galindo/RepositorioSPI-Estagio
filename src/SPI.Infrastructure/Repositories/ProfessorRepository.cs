using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class ProfessorRepository : IProfessorRepository
    {
        private readonly SpiDbContext _dbContext;

        public ProfessorRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Professor?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            _dbContext.Professores.FirstOrDefaultAsync(p => p.Email == email, cancellationToken);

        public Task<Professor?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.Professores.FirstOrDefaultAsync(p => p.Id == id && p.Ativo, cancellationToken);

        public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) =>
            _dbContext.Professores.AnyAsync(cancellationToken);

        public Task<Professor?> ObterUnicaAsync(CancellationToken cancellationToken = default) =>
            _dbContext.Professores.FirstOrDefaultAsync(cancellationToken);

        public Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken = default) =>
            _dbContext.Professores.AnyAsync(p => p.Cpf == cpf, cancellationToken);

        public Task<bool> ExisteEmailAsync(string email, int? ignorarId = null, CancellationToken cancellationToken = default) =>
            _dbContext.Professores.AnyAsync(p => p.Email == email && (ignorarId == null || p.Id != ignorarId), cancellationToken);

        public async Task AdicionarAsync(Professor professor, CancellationToken cancellationToken = default) =>
            await _dbContext.Professores.AddAsync(professor, cancellationToken);

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
            _dbContext.SaveChangesAsync(cancellationToken);
    }
}
