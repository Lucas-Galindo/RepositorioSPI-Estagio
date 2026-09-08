namespace SPI.Domain.Entities
{
    // Tabela auxiliar usada pela procedure sp_cadastrar_aluno (04_procedures.sql)
    // para gerar o RA sequencial de cada ano+semestre. Nao e manipulada pela API.
    public class RaControle
    {
        public int Ano { get; set; }
        public byte Semestre { get; set; }
        public int UltimoSequencial { get; set; }
    }
}
