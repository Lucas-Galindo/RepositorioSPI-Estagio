using SPI.Application.Common;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Repositories;

namespace SPI.Application.Tests.Professores
{
    // Fakes manuais (biblioteca de mock nao instalada neste projeto de testes),
    // mesmo padrao de VinculoCobrancaFakes.cs. Primeiro arquivo de fakes de
    // Professor/ExclusaoProfessorToken: nao existia nenhum teste deste servico
    // antes da spec 044.

    public class FakeProfessorRepositoryParaTeste : IProfessorRepository
    {
        private int _proximoId = 1;

        public List<Professor> Professores { get; } = new();
        public int SalvamentosRealizados { get; private set; }

        public Professor Semear(Professor professor)
        {
            professor.Id = professor.Id == 0 ? _proximoId++ : professor.Id;
            _proximoId = Math.Max(_proximoId, professor.Id + 1);
            Professores.Add(professor);
            return professor;
        }

        public Task<Professor?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Professores.FirstOrDefault(p => p.Email == email));

        public Task<Professor?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Professores.FirstOrDefault(p => p.Id == id && p.Ativo));

        public Task<bool> ExisteAlgumAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Professores.Count > 0);

        public Task<Professor?> ObterUnicaAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Professores.FirstOrDefault());

        public Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken = default) =>
            Task.FromResult(Professores.Any(p => p.Cpf == cpf));

        public Task<bool> ExisteEmailAsync(string email, int? ignorarId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Professores.Any(p => p.Email == email && (ignorarId is null || p.Id != ignorarId)));

        public Task AdicionarAsync(Professor professor, CancellationToken cancellationToken = default)
        {
            Semear(professor);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            SalvamentosRealizados++;
            return Task.CompletedTask;
        }
    }

    public class FakeExclusaoProfessorTokenRepositoryParaTeste : IExclusaoProfessorTokenRepository
    {
        private long _proximoId = 1;

        public List<ExclusaoProfessorToken> Tokens { get; } = new();
        public int SalvamentosRealizados { get; private set; }

        public Task AdicionarAsync(ExclusaoProfessorToken token, CancellationToken cancellationToken = default)
        {
            token.Id = token.Id == 0 ? _proximoId++ : token.Id;
            _proximoId = Math.Max(_proximoId, token.Id + 1);
            Tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task<ExclusaoProfessorToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task<List<ExclusaoProfessorToken>> ObterPendentesPorAdminIdAsync(int adminId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens.Where(t => t.AdminId == adminId && !t.Usado).ToList());

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            SalvamentosRealizados++;
            return Task.CompletedTask;
        }
    }

    public class FakeAdminRepositoryParaTeste : IAdminRepository
    {
        public List<Admin> Admins { get; } = new();

        public Task<Admin?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Admins.FirstOrDefault(a => a.Email == email));

        public Task<Admin?> ObterPorIdAtivoAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Admins.FirstOrDefault(a => a.Id == id && a.Ativo));
    }

    // Gera valores deterministicos (sem aleatoriedade) para as asserções de teste:
    // o "token" em claro e sempre igual ao hash prefixado, facilitando comparar
    // o que foi "enviado por e-mail" com o que foi persistido.
    public class FakeTokenServiceParaTeste : ITokenService
    {
        public string? ProximoToken { get; set; }

        public (string AccessToken, DateTime ExpiraEm) GerarAccessToken(int usuarioId, SPI.Domain.Enums.PerfilUsuario perfil, string nome, string? email) =>
            throw new NotImplementedException();

        public (string Token, string TokenHash, DateTime ExpiraEm) GerarRefreshToken() =>
            throw new NotImplementedException();

        public string HashRefreshToken(string refreshToken) => throw new NotImplementedException();

        public (string Token, string TokenHash) GerarTokenSeguro()
        {
            var token = ProximoToken ?? Guid.NewGuid().ToString("N");
            return (token, CalcularHash(token));
        }

        public string CalcularHash(string valor) => $"hash-{valor}";
    }

    // Captura destinatario/assunto/corpo de todo envio, para as asserções
    // negativas de FR-002 (nunca para o e-mail da professora) e pode ser
    // configurado para falhar (FR-011).
    public class FakeEmailSenderParaTeste : IEmailSender
    {
        public List<(string Destinatario, string Assunto, string CorpoHtml)> EnviosRealizados { get; } = new();
        public bool DeveFalhar { get; set; }

        public Task EnviarAsync(string destinatario, string assunto, string corpoHtml, CancellationToken cancellationToken = default)
        {
            if (DeveFalhar)
            {
                throw new InvalidOperationException("Falha simulada de envio de e-mail (Brevo indisponivel).");
            }

            EnviosRealizados.Add((destinatario, assunto, corpoHtml));
            return Task.CompletedTask;
        }
    }

    // Dependencias de ProfessorService nao exercitadas pelos testes de
    // exclusao/reativacao (AlterarSenhaAsync nao e chamado nesses cenarios).
    public class FakeRefreshTokenRepositorySemUso : IRefreshTokenRepository
    {
        public Task AdicionarAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<RefreshToken?> ObterPorTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RevogarTodosDoUsuarioAsync(int usuarioId, PerfilUsuario perfil, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public class FakePasswordHasherSemUso : IPasswordHasher
    {
        public string Hash(string senha) => throw new NotImplementedException();

        public bool Verificar(string senha, string hash) => throw new NotImplementedException();
    }
}
