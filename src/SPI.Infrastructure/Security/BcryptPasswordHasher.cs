using SPI.Application.Common;

namespace SPI.Infrastructure.Security
{
    public class BcryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string senha) => BCrypt.Net.BCrypt.EnhancedHashPassword(senha, workFactor: 12);

        public bool Verificar(string senha, string hash) => BCrypt.Net.BCrypt.EnhancedVerify(senha, hash);
    }
}
