using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class SenhaResetTokenConfiguration : IEntityTypeConfiguration<SenhaResetToken>
    {
        public void Configure(EntityTypeBuilder<SenhaResetToken> builder)
        {
            builder.ToTable("senha_reset_token");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasColumnName("id");
            builder.Property(s => s.ProfessorId).HasColumnName("professor_id");
            builder.Property(s => s.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
            builder.Property(s => s.CriadoEm).HasColumnName("criado_em");
            builder.Property(s => s.ExpiraEm).HasColumnName("expira_em");
            builder.Property(s => s.Usado).HasColumnName("usado").HasDefaultValue(false);

            builder.HasOne(s => s.Professor)
                .WithMany()
                .HasForeignKey(s => s.ProfessorId)
                .HasConstraintName("fk_senha_reset_token_professor");

            builder.HasIndex(s => s.TokenHash).IsUnique();
            builder.HasIndex(s => s.ProfessorId);
        }
    }
}
