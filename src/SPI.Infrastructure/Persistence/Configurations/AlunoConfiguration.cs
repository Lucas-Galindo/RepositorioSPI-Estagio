using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SPI.Domain.Entities;

namespace SPI.Infrastructure.Persistence.Configurations
{
    public class AlunoConfiguration : IEntityTypeConfiguration<Aluno>
    {
        public void Configure(EntityTypeBuilder<Aluno> builder)
        {
            builder.ToTable("alunos");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).HasColumnName("id");
            builder.Property(a => a.Ra).HasColumnName("ra").HasMaxLength(20).IsRequired();
            builder.Property(a => a.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
            builder.Property(a => a.Cpf).HasColumnName("cpf").HasMaxLength(14);
            builder.Property(a => a.TelefoneAluno).HasColumnName("telefone_aluno").HasMaxLength(20);
            builder.Property(a => a.TelefoneResponsavel).HasColumnName("telefone_responsavel").HasMaxLength(20);
            builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(150);
            builder.Property(a => a.Senha).HasColumnName("senha").HasMaxLength(255);
            builder.Property(a => a.EmailResponsavel).HasColumnName("email_responsavel").HasMaxLength(150);
            builder.Property(a => a.ValorAula).HasColumnName("valor_aula").HasColumnType("decimal(10,2)");
            builder.Property(a => a.Frequencia).HasColumnName("frequencia").HasDefaultValue(0);
            builder.Property(a => a.Ativo).HasColumnName("ativo").HasDefaultValue(true);

            builder.HasIndex(a => a.Ra).IsUnique();
            builder.HasIndex(a => a.Cpf).IsUnique();
        }
    }
}
