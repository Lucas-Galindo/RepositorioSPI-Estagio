using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
    {
        public void Configure(EntityTypeBuilder<Notificacao> builder)
        {
            builder.ToTable("notificacao");

            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).HasColumnName("id");
            builder.Property(n => n.ProfessorId).HasColumnName("professor_id");
            builder.Property(n => n.Tipo).HasColumnName("tipo").HasMaxLength(40).IsRequired().HasConversion<string>();
            builder.Property(n => n.AulaId).HasColumnName("aula_id");
            builder.Property(n => n.CriadaEm).HasColumnName("criada_em");
            builder.Property(n => n.LidaEm).HasColumnName("lida_em");
            builder.Property(n => n.ResolvidaEm).HasColumnName("resolvida_em");

            builder.HasOne(n => n.Aula)
                .WithMany()
                .HasForeignKey(n => n.AulaId)
                .HasConstraintName("fk_notificacao_aula");
        }
    }
}
