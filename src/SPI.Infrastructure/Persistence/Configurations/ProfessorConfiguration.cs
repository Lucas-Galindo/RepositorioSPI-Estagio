using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class ProfessorConfiguration : IEntityTypeConfiguration<Professor>
    {
        public void Configure(EntityTypeBuilder<Professor> builder)
        {
            builder.ToTable("professor");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            builder.Property(p => p.Cpf).HasColumnName("cpf").HasMaxLength(14).IsRequired();
            builder.Property(p => p.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
            builder.Property(p => p.Senha).HasColumnName("senha").HasMaxLength(255).IsRequired();
            builder.Property(p => p.Telefone).HasColumnName("telefone").HasMaxLength(20);
            builder.Property(p => p.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            builder.HasIndex(p => p.Cpf).IsUnique();
            builder.HasIndex(p => p.Email).IsUnique();
        }
    }
}
