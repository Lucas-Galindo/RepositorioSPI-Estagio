using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class AulaAlunoConfiguration : IEntityTypeConfiguration<AulaAluno>
    {
        public void Configure(EntityTypeBuilder<AulaAluno> builder)
        {
            builder.ToTable("aula_aluno");

            builder.HasKey(aa => new { aa.AulaId, aa.AlunoId });
            builder.Property(aa => aa.AulaId).HasColumnName("aula_id");
            builder.Property(aa => aa.AlunoId).HasColumnName("aluno_id");
            builder.Property(aa => aa.Presente).HasColumnName("presente");

            builder.HasOne(aa => aa.Aula)
                .WithMany(a => a.AulaAlunos)
                .HasForeignKey(aa => aa.AulaId)
                .HasConstraintName("fk_aulaaluno_aula");

            builder.HasOne(aa => aa.Aluno)
                .WithMany(a => a.AulaAlunos)
                .HasForeignKey(aa => aa.AlunoId)
                .HasConstraintName("fk_aulaaluno_aluno");
        }
    }
}
