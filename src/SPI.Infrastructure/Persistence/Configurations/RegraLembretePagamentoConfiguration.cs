using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class RegraLembretePagamentoConfiguration : IEntityTypeConfiguration<RegraLembretePagamento>
    {
        public void Configure(EntityTypeBuilder<RegraLembretePagamento> builder)
        {
            builder.ToTable("regra_lembrete_pagamento");

            builder.HasKey(r => r.Id);
            // Linha unica criada pela migracao (23_centro_notificacoes.sql).
            builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(r => r.Ativo).HasColumnName("ativo");
            builder.Property(r => r.DiaSemana).HasColumnName("dia_semana");
            builder.Property(r => r.Hora).HasColumnName("hora");
        }
    }
}
