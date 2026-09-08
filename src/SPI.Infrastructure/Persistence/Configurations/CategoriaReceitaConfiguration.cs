using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class CategoriaReceitaConfiguration : IEntityTypeConfiguration<CategoriaReceita>
    {
        public void Configure(EntityTypeBuilder<CategoriaReceita> builder)
        {
            builder.ToTable("categoria_receita");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
            builder.Property(c => c.Ativo).HasColumnName("ativo").HasDefaultValue(true);
        }
    }
}
