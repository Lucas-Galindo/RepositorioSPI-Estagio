using SPI.Domain.Enums;

namespace SPI.Domain.Entities
{
    // Guarda o hash do refresh token, nunca o token em claro (07_auth.sql).
    // UsuarioId + Perfil juntos identificam o dono, ja que professor/aluno/admin
    // sao tabelas separadas e nao existe uma tabela unica de "usuario".
    public class RefreshToken
    {
        public long Id { get; set; }
        public int UsuarioId { get; set; }
        public PerfilUsuario Perfil { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? RevogadoEm { get; set; }
        public string? SubstituidoPorTokenHash { get; set; }
        public string? IpCriacao { get; set; }
        public string? UserAgent { get; set; }

        public bool EstaAtivo => RevogadoEm is null && ExpiraEm > DateTime.UtcNow;
    }
}
