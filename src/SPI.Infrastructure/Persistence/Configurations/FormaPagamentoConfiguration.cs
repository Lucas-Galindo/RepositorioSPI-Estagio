using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class FormaPagamentoConfiguration : IEntityTypeConfiguration<FormaPagamento>
    {
        public void Configure(EntityTypeBuilder<FormaPagamento> builder)
        {
            builder.ToTable("forma_pagamento");

            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).HasColumnName("id");
            builder.Property(f => f.Forma).HasColumnName("forma").HasMaxLength(50).IsRequired();
            builder.Property(f => f.Descricao).HasColumnName("descricao").HasMaxLength(255);
            builder.Property(f => f.Ativo).HasColumnName("ativo").HasDefaultValue(true);
        }
    }
}
