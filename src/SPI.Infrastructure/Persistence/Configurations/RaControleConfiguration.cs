using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class RaControleConfiguration : IEntityTypeConfiguration<RaControle>
    {
        public void Configure(EntityTypeBuilder<RaControle> builder)
        {
            builder.ToTable("ra_controle");

            builder.HasKey(r => new { r.Ano, r.Semestre });
            builder.Property(r => r.Ano).HasColumnName("ano");
            builder.Property(r => r.Semestre).HasColumnName("semestre");
            builder.Property(r => r.UltimoSequencial).HasColumnName("ultimo_sequencial").HasDefaultValue(0);
        }
    }
}
