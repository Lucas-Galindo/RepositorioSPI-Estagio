using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class PagamentoAulaConfiguration : IEntityTypeConfiguration<PagamentoAula>
    {
        public void Configure(EntityTypeBuilder<PagamentoAula> builder)
        {
            builder.ToTable("pagamento_aula");

            builder.HasKey(pa => new { pa.PagamentoId, pa.AulaId });
            builder.Property(pa => pa.PagamentoId).HasColumnName("pagamento_id");
            builder.Property(pa => pa.AulaId).HasColumnName("aula_id");

            builder.HasOne(pa => pa.Pagamento)
                .WithMany(p => p.PagamentosAula)
                .HasForeignKey(pa => pa.PagamentoId)
                .HasConstraintName("fk_pagamentoaula_pagamento");

            builder.HasOne(pa => pa.Aula)
                .WithMany(a => a.PagamentosAula)
                .HasForeignKey(pa => pa.AulaId)
                .HasConstraintName("fk_pagamentoaula_aula");
        }
    }
}
