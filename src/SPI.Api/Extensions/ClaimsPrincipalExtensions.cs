using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SPI.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Le o id do usuario autenticado a partir da claim "sub" do JWT
        /// (ver JwtTokenService.GerarAccessToken).
        /// </summary>
        public static int ObterUsuarioId(this ClaimsPrincipal usuario)
        {
            var valor = usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException("Token nao contem a claim 'sub'.");

            return int.Parse(valor);
        }
    }
}
