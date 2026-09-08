using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class AdminConfiguration : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            builder.ToTable("admin");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            builder.Property(a => a.Senha).HasColumnName("senha").HasMaxLength(255).IsRequired();
            builder.Property(a => a.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            builder.HasIndex(a => a.Email).IsUnique();
        }
    }
}
