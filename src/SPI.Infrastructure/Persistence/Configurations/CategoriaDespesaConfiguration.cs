using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class CategoriaDespesaConfiguration : IEntityTypeConfiguration<CategoriaDespesa>
    {
        public void Configure(EntityTypeBuilder<CategoriaDespesa> builder)
        {
            builder.ToTable("categoria_despesa");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(80).IsRequired();
            builder.Property(c => c.Ativo).HasColumnName("ativo").HasDefaultValue(true);
        }
    }
}
