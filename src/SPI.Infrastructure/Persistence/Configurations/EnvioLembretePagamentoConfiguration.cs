using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class EnvioLembretePagamentoConfiguration : IEntityTypeConfiguration<EnvioLembretePagamento>
    {
        public void Configure(EntityTypeBuilder<EnvioLembretePagamento> builder)
        {
            builder.ToTable("envio_lembrete_pagamento");

            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AlunoId).HasColumnName("aluno_id");
            builder.Property(e => e.DataEnvio).HasColumnName("data_envio");
            builder.Property(e => e.TentadoEm).HasColumnName("tentado_em");
            builder.Property(e => e.Destinatarios).HasColumnName("destinatarios").HasMaxLength(500);
            builder.Property(e => e.Resultado).HasColumnName("resultado").HasMaxLength(20);
            builder.Property(e => e.Tentativas).HasColumnName("tentativas");
            builder.Property(e => e.Detalhe).HasColumnName("detalhe").HasMaxLength(500);

            builder.HasOne(e => e.Aluno)
                .WithMany()
                .HasForeignKey(e => e.AlunoId)
                .HasConstraintName("fk_enviolembretepagamento_aluno");
        }
    }
}
