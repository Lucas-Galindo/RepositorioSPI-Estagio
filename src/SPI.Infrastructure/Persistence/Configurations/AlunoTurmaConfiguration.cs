using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class AlunoTurmaConfiguration : IEntityTypeConfiguration<AlunoTurma>
    {
        public void Configure(EntityTypeBuilder<AlunoTurma> builder)
        {
            builder.ToTable("alunos_turma");

            builder.HasKey(at => new { at.AlunoId, at.TurmaId });
            builder.Property(at => at.AlunoId).HasColumnName("aluno_id");
            builder.Property(at => at.TurmaId).HasColumnName("turma_id");

            builder.HasOne(at => at.Aluno)
                .WithMany(a => a.AlunosTurma)
                .HasForeignKey(at => at.AlunoId)
                .HasConstraintName("fk_alunosturma_aluno");

            builder.HasOne(at => at.Turma)
                .WithMany(t => t.AlunosTurma)
                .HasForeignKey(at => at.TurmaId)
                .HasConstraintName("fk_alunosturma_turma");
        }
    }
}
