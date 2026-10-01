using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class ExclusaoProfessorTokenConfiguration : IEntityTypeConfiguration<ExclusaoProfessorToken>
    {
        public void Configure(EntityTypeBuilder<ExclusaoProfessorToken> builder)
        {
            builder.ToTable("exclusao_professor_token");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AdminId).HasColumnName("admin_id");
            builder.Property(e => e.ProfessorId).HasColumnName("professor_id");
            builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
            builder.Property(e => e.CriadoEm).HasColumnName("criado_em");
            builder.Property(e => e.ExpiraEm).HasColumnName("expira_em");
            builder.Property(e => e.Usado).HasColumnName("usado").HasDefaultValue(false);

            builder.HasOne(e => e.Admin)
                .WithMany()
                .HasForeignKey(e => e.AdminId)
                .HasConstraintName("fk_exclusao_professor_token_admin");

            builder.HasOne(e => e.Professor)
                .WithMany()
                .HasForeignKey(e => e.ProfessorId)
                .HasConstraintName("fk_exclusao_professor_token_professor");

            builder.HasIndex(e => e.TokenHash).IsUnique();
            builder.HasIndex(e => e.AdminId);
            builder.HasIndex(e => e.ProfessorId);
        }
    }
}
