using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class VinculoCobrancaConfiguration : IEntityTypeConfiguration<VinculoCobranca>
    {
        public void Configure(EntityTypeBuilder<VinculoCobranca> builder)
        {
            builder.ToTable("vinculo_cobranca");

            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id).HasColumnName("id");
            builder.Property(v => v.AlunoId).HasColumnName("aluno_id");
            builder.Property(v => v.TurmaId).HasColumnName("turma_id");
            builder.Property(v => v.Modalidade).HasColumnName("modalidade").HasMaxLength(20).IsRequired().HasConversion<string>();
            builder.Property(v => v.Valor).HasColumnName("valor").HasColumnType("decimal(10,2)");
            builder.Property(v => v.AulasIncluidas).HasColumnName("aulas_incluidas");
            builder.Property(v => v.SaldoAulas).HasColumnName("saldo_aulas");
            builder.Property(v => v.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            // A coluna gerada chave_ativa (unicidade dos vinculos ativos) e gerida
            // somente pelo banco -- de proposito nao e mapeada aqui.

            builder.HasOne(v => v.Aluno)
                .WithMany(a => a.VinculosCobranca)
                .HasForeignKey(v => v.AlunoId)
                .HasConstraintName("fk_vinculocobranca_aluno");

            builder.HasOne(v => v.Turma)
                .WithMany(t => t.VinculosCobranca)
                .HasForeignKey(v => v.TurmaId)
                .HasConstraintName("fk_vinculocobranca_turma");
        }
    }
}
