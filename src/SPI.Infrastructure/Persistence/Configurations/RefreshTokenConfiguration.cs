using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_token");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasColumnName("id");
            builder.Property(r => r.UsuarioId).HasColumnName("usuario_id");
            builder.Property(r => r.Perfil).HasColumnName("perfil").HasMaxLength(20).HasConversion<string>().IsRequired();
            builder.Property(r => r.TokenHash).HasColumnName("token_hash").HasMaxLength(255).IsRequired();
            builder.Property(r => r.CriadoEm).HasColumnName("criado_em");
            builder.Property(r => r.ExpiraEm).HasColumnName("expira_em");
            builder.Property(r => r.RevogadoEm).HasColumnName("revogado_em");
            builder.Property(r => r.SubstituidoPorTokenHash).HasColumnName("substituido_por_token_hash").HasMaxLength(255);
            builder.Property(r => r.IpCriacao).HasColumnName("ip_criacao").HasMaxLength(45);
            builder.Property(r => r.UserAgent).HasColumnName("user_agent").HasMaxLength(255);

            builder.Ignore(r => r.EstaAtivo);

            builder.HasIndex(r => r.TokenHash).IsUnique();
            builder.HasIndex(r => new { r.UsuarioId, r.Perfil });
        }
    }
}
