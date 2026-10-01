namespace SPI.Domain.Entities
{
    // 2FA por e-mail para o Admin confirmar a exclusao logica da Professora
    // (ver spec 044). Dono do token e o Admin que solicitou a exclusao
    // (AdminId); alvo e a Professora a ser desativada (ProfessorId) - donos
    // diferentes do fluxo de SenhaResetToken, onde dono e alvo sao sempre a
    // mesma professora.
    public class ExclusaoProfessorToken
    {
        public long Id { get; set; }
        public int AdminId { get; set; }
        public int ProfessorId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public bool Usado { get; set; }

        public Admin Admin { get; set; } = null!;
        public Professor Professor { get; set; } = null!;
    }
}
