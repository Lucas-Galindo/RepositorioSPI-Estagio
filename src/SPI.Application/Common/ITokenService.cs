using SPI.Domain.Enums;

namespace SPI.Application.Common
{
    public interface ITokenService
    {
        /// <summary>
        /// Gera o access token JWT (curta duracao) com as claims do usuario autenticado.
        /// </summary>
        (string AccessToken, DateTime ExpiraEm) GerarAccessToken(int usuarioId, PerfilUsuario perfil, string nome, string? email);

        /// <summary>
        /// Gera um refresh token opaco (longa duracao) e o hash que deve ser persistido.
        /// O valor em claro so existe neste retorno; nunca e armazenado.
        /// </summary>
        (string Token, string TokenHash, DateTime ExpiraEm) GerarRefreshToken();

        /// <summary>
        /// Calcula o hash de um refresh token recebido do cliente, para busca no banco.
        /// </summary>
        string HashRefreshToken(string refreshToken);

        /// <summary>
        /// Gera um token opaco generico (ex: recuperacao de senha) e o hash a persistir.
        /// </summary>
        (string Token, string TokenHash) GerarTokenSeguro();

        /// <summary>
        /// Calcula o hash de um token opaco recebido do cliente, para busca no banco.
        /// </summary>
        string CalcularHash(string valor);
    }
}
