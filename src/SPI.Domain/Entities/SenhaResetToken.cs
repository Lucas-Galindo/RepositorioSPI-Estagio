namespace SPI.Domain.Entities
{
    // Recuperacao de senha e exclusiva da Professora nesta fase (ver prompt
    // mestre, secao "Fluxos de autenticacao"). Token de uso unico, hash
    // armazenado, validade curta controlada em ExpiraEm.
    public class SenhaResetToken
    {
        public long Id { get; set; }
        public int ProfessorId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public bool Usado { get; set; }

        public Professor Professor { get; set; } = null!;
    }
}
