using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class TurmaConfiguration : IEntityTypeConfiguration<Turma>
    {
        public void Configure(EntityTypeBuilder<Turma> builder)
        {
            builder.ToTable("turma");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).HasColumnName("id");
            builder.Property(t => t.ProfessorId).HasColumnName("professor_id");
            builder.Property(t => t.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
            builder.Property(t => t.Ativo).HasColumnName("ativo").HasDefaultValue(true);
            builder.Property(t => t.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired().HasConversion<string>();
            builder.Property(t => t.DataInicio).HasColumnName("data_inicio");
            builder.Property(t => t.DataFim).HasColumnName("data_fim");

            builder.HasOne(t => t.Professor)
                .WithMany(p => p.Turmas)
                .HasForeignKey(t => t.ProfessorId)
                .HasConstraintName("fk_turma_professor");
        }
    }
}
