using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
    {
        public void Configure(EntityTypeBuilder<Pagamento> builder)
        {
            builder.ToTable("pagamento");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.AlunoId).HasColumnName("aluno_id");
            builder.Property(p => p.Descricao).HasColumnName("descricao").HasMaxLength(255);
            builder.Property(p => p.FormaPagamentoId).HasColumnName("forma_pagamento_id");
            builder.Property(p => p.CategoriaReceitaId).HasColumnName("categoria_receita_id");
            builder.Property(p => p.DataVencimento).HasColumnName("data_vencimento");
            builder.Property(p => p.DataPagamento).HasColumnName("data_pagamento");
            builder.Property(p => p.Competencia).HasColumnName("competencia");
            builder.Property(p => p.ValorFinal).HasColumnName("valor_final").HasColumnType("decimal(10,2)");
            builder.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pendente");
            builder.Property(p => p.Observacoes).HasColumnName("observacoes");

            builder.HasOne(p => p.Aluno)
                .WithMany(a => a.Pagamentos)
                .HasForeignKey(p => p.AlunoId)
                .HasConstraintName("fk_pagamento_aluno");

            builder.HasOne(p => p.FormaPagamento)
                .WithMany(f => f.Pagamentos)
                .HasForeignKey(p => p.FormaPagamentoId)
                .HasConstraintName("fk_pagamento_forma");

            builder.HasOne(p => p.CategoriaReceita)
                .WithMany(c => c.Pagamentos)
                .HasForeignKey(p => p.CategoriaReceitaId)
                .HasConstraintName("fk_pagamento_categoria_receita");
        }
    }
}
