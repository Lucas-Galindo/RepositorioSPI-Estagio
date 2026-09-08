using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class LembreteConfiguration : IEntityTypeConfiguration<Lembrete>
    {
        public void Configure(EntityTypeBuilder<Lembrete> builder)
        {
            builder.ToTable("lembrete");

            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("id");
            builder.Property(l => l.TurmaId).HasColumnName("turma_id");
            builder.Property(l => l.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pendente");
            builder.Property(l => l.HoraProgramada).HasColumnName("hora_programada").HasColumnType("datetime");
            builder.Property(l => l.Destinatarios).HasColumnName("destinatarios").HasMaxLength(255).IsRequired();
            builder.Property(l => l.Canal).HasColumnName("canal").HasMaxLength(20).IsRequired();
            builder.Property(l => l.AntecedenciaHora).HasColumnName("antecedencia_hora");
            builder.Property(l => l.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            builder.HasOne(l => l.Turma)
                .WithMany(t => t.Lembretes)
                .HasForeignKey(l => l.TurmaId)
                .HasConstraintName("fk_lembrete_turma");
        }
    }
}
