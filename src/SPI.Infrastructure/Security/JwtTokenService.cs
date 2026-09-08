using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SPI.Application.Common;
using SPI.Domain.Enums;

namespace SPI.Infrastructure.Security
{
    public class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public (string AccessToken, DateTime ExpiraEm) GerarAccessToken(int usuarioId, PerfilUsuario perfil, string nome, string? email)
        {
            var expiraEm = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, nome),
                new(ClaimTypes.Role, perfil.ToString())
            };

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
            }

            var credenciais = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expiraEm,
                signingCredentials: credenciais);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
        }

        public (string Token, string TokenHash, DateTime ExpiraEm) GerarRefreshToken()
        {
            var (token, hash) = GerarTokenSeguro();
            var expiraEm = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

            return (token, hash, expiraEm);
        }

        public string HashRefreshToken(string refreshToken) => CalcularHash(refreshToken);

        public (string Token, string TokenHash) GerarTokenSeguro()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(bytes);

            return (token, CalcularHash(token));
        }

        public string CalcularHash(string valor)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
            return Convert.ToHexString(bytes);
        }
    }
}
