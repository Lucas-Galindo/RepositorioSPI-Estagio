using SPI.Domain.Enums;

namespace SPI.Application.Auth.Dtos
{
    public record LoginResponse
    {   /// <summary>
        /// Perfil identificado automaticamente a partir do Login informado
        /// (Professor, Aluno ou Admin). O front usa isso para decidir para
        /// qual area redirecionar o usuario.
        /// </summary>
        public PerfilUsuario Perfil { get; set; }

        /// <summary>
        /// Token JWT de curta duracao, usado no header Authorization das proximas requisicoes.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Token opaco de longa duracao, usado apenas para obter um novo par via /api/auth/refresh.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Data/hora (UTC) em que o access token expira.
        /// </summary>
        public DateTime AccessTokenExpiraEm { get; set; }
    }
}
