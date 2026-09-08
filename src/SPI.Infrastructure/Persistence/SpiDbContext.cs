using Microsoft.EntityFrameworkCore;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence
{
    public class SpiDbContext : DbContext
    {
        public SpiDbContext(DbContextOptions<SpiDbContext> options) : base(options)
        {
        }

        public DbSet<Professor> Professores => Set<Professor>();
        public DbSet<Materia> Materias => Set<Materia>();
        public DbSet<Aluno> Alunos => Set<Aluno>();
        public DbSet<Turma> Turmas => Set<Turma>();
        public DbSet<AlunoTurma> AlunosTurma => Set<AlunoTurma>();
        public DbSet<Aula> Aulas => Set<Aula>();
        public DbSet<Lembrete> Lembretes => Set<Lembrete>();
        public DbSet<FormaPagamento> FormasPagamento => Set<FormaPagamento>();
        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
        public DbSet<PagamentoAula> PagamentosAula => Set<PagamentoAula>();
        public DbSet<CategoriaReceita> CategoriasReceita => Set<CategoriaReceita>();
        public DbSet<CategoriaDespesa> CategoriasDespesa => Set<CategoriaDespesa>();
        public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();
        public DbSet<AulaAluno> AulaAlunos => Set<AulaAluno>();
        public DbSet<RaControle> RaControles => Set<RaControle>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<SenhaResetToken> SenhaResetTokens => Set<SenhaResetToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpiDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
