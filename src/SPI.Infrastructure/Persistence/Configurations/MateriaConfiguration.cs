using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class MateriaConfiguration : IEntityTypeConfiguration<Materia>
    {
        public void Configure(EntityTypeBuilder<Materia> builder)
        {
            builder.ToTable("materias");

            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("id");
            builder.Property(m => m.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
            builder.Property(m => m.Descricao).HasColumnName("descricao").HasMaxLength(255);
            builder.Property(m => m.Nivel).HasColumnName("nivel").HasMaxLength(100);
            builder.Property(m => m.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            builder.HasIndex(m => m.Nome).IsUnique();
        }
    }
}
