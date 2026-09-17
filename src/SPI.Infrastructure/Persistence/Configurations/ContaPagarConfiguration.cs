using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class ContaPagarConfiguration : IEntityTypeConfiguration<ContaPagar>
    {
        public void Configure(EntityTypeBuilder<ContaPagar> builder)
        {
            builder.ToTable("conta_pagar");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");
            builder.Property(c => c.Descricao).HasColumnName("descricao").HasMaxLength(255).IsRequired();
            builder.Property(c => c.CategoriaDespesaId).HasColumnName("categoria_despesa_id");
            builder.Property(c => c.Favorecido).HasColumnName("favorecido").HasMaxLength(150);
            builder.Property(c => c.Valor).HasColumnName("valor").HasColumnType("decimal(10,2)");
            builder.Property(c => c.Competencia).HasColumnName("competencia");
            builder.Property(c => c.DataVencimento).HasColumnName("data_vencimento");
            builder.Property(c => c.DataPagamento).HasColumnName("data_pagamento");
            builder.Property(c => c.FormaPagamentoId).HasColumnName("forma_pagamento_id");
            builder.Property(c => c.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pendente");
            builder.Property(c => c.Observacoes).HasColumnName("observacoes");
            builder.Property(c => c.ArquivoConteudo).HasColumnName("arquivo_conteudo");
            builder.Property(c => c.ArquivoNomeOriginal).HasColumnName("arquivo_nome_original").HasMaxLength(255);
            builder.Property(c => c.ArquivoTipoMime).HasColumnName("arquivo_tipo_mime").HasMaxLength(100);
            builder.Property(c => c.ArquivoTamanhoBytes).HasColumnName("arquivo_tamanho_bytes");
            builder.Property(c => c.ArquivoDataUpload).HasColumnName("arquivo_data_upload");

            builder.HasOne(c => c.CategoriaDespesa)
                .WithMany(cd => cd.ContasPagar)
                .HasForeignKey(c => c.CategoriaDespesaId)
                .HasConstraintName("fk_contapagar_categoria");

            builder.HasOne(c => c.FormaPagamento)
                .WithMany(f => f.ContasPagar)
                .HasForeignKey(c => c.FormaPagamentoId)
                .HasConstraintName("fk_contapagar_forma");
        }
    }
}
