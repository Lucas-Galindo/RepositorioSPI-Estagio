using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class AulaConfiguration : IEntityTypeConfiguration<Aula>
    {
        public void Configure(EntityTypeBuilder<Aula> builder)
        {
            builder.ToTable("aula");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.Descricao).HasColumnName("descricao").HasMaxLength(255);
            builder.Property(a => a.MateriaId).HasColumnName("materia_id");
            builder.Property(a => a.ProfessorId).HasColumnName("professor_id");
            builder.Property(a => a.TurmaId).HasColumnName("turma_id");
            builder.Property(a => a.DataInicio).HasColumnName("data_inicio");
            builder.Property(a => a.HoraInicio).HasColumnName("hora_inicio");
            builder.Property(a => a.HoraFim).HasColumnName("hora_fim");
            builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Agendada");
            builder.Property(a => a.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            builder.HasOne(a => a.Materia)
                .WithMany(m => m.Aulas)
                .HasForeignKey(a => a.MateriaId)
                .HasConstraintName("fk_aula_materia");

            builder.HasOne(a => a.Professor)
                .WithMany(p => p.Aulas)
                .HasForeignKey(a => a.ProfessorId)
                .HasConstraintName("fk_aula_professor");

            builder.HasOne(a => a.Turma)
                .WithMany(t => t.Aulas)
                .HasForeignKey(a => a.TurmaId)
                .HasConstraintName("fk_aula_turma");
        }
    }
}
