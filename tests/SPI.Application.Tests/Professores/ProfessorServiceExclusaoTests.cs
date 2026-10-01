using SPI.Application.Professores.Dtos;
using SPI.Application.Professores.Services;
using SPI.Application.Professores.Validators;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Professores
{
    public class ProfessorServiceExclusaoTests
    {
        private static (ProfessorService Service, FakeProfessorRepositoryParaTeste Professores, FakeExclusaoProfessorTokenRepositoryParaTeste Tokens,
            FakeAdminRepositoryParaTeste Admins, FakeTokenServiceParaTeste TokenService, FakeEmailSenderParaTeste EmailSender) CriarServico()
        {
            var professores = new FakeProfessorRepositoryParaTeste();
            var tokens = new FakeExclusaoProfessorTokenRepositoryParaTeste();
            var admins = new FakeAdminRepositoryParaTeste();
            var tokenService = new FakeTokenServiceParaTeste();
            var emailSender = new FakeEmailSenderParaTeste();

            var service = new ProfessorService(
                professores,
                new FakeRefreshTokenRepositorySemUso(),
                new FakePasswordHasherSemUso(),
                tokens,
                admins,
                tokenService,
                emailSender);

            return (service, professores, tokens, admins, tokenService, emailSender);
        }

        private static Professor CriarProfessoraAtiva() => new()
        {
            Nome = "Professora Teste",
            Cpf = "11111111111",
            Email = "professora@spi.local",
            Senha = "hash",
            Ativo = true
        };

        private static Admin CriarAdmin(int id, string email) => new()
        {
            Id = id,
            Nome = $"Admin {id}",
            Email = email,
            Senha = "hash",
            Ativo = true
        };

        [Fact]
        public async Task SolicitarExclusao_EnviaCodigoApenasParaOEmailDoAdminAutenticado()
        {
            var (service, professores, tokens, admins, _, emailSender) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);

            Assert.Single(emailSender.EnviosRealizados);
            Assert.Equal("admin1@spi.local", emailSender.EnviosRealizados[0].Destinatario);
            Assert.DoesNotContain(emailSender.EnviosRealizados, e => e.Destinatario == professora.Email);
            Assert.Single(tokens.Tokens);
            Assert.Equal(1, tokens.Tokens[0].AdminId);
            Assert.Equal(professora.Id, tokens.Tokens[0].ProfessorId);
            Assert.NotEqual("codigo-em-claro-nao-deveria-ser-salvo", tokens.Tokens[0].TokenHash);
        }

        [Fact]
        public async Task SolicitarExclusao_DoisAdminsDiferentes_CadaUmRecebeSeuProprioCodigoNoProprioEmail()
        {
            var (service, professores, tokens, admins, _, emailSender) = CriarServico();
            professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));
            admins.Admins.Add(CriarAdmin(2, "admin2@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            await service.SolicitarExclusaoAsync(adminId: 2);

            Assert.Equal(2, emailSender.EnviosRealizados.Count);
            Assert.Contains(emailSender.EnviosRealizados, e => e.Destinatario == "admin1@spi.local");
            Assert.Contains(emailSender.EnviosRealizados, e => e.Destinatario == "admin2@spi.local");
            Assert.Equal(2, tokens.Tokens.Count);
            Assert.True(tokens.Tokens.All(t => !t.Usado));
        }

        [Fact]
        public async Task SolicitarExclusao_NovoPedidoDoMesmoAdmin_InvalidaOCodigoAnteriorDesseAdmin_MasNaoODeOutroAdmin()
        {
            var (service, professores, tokens, admins, _, _) = CriarServico();
            professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));
            admins.Admins.Add(CriarAdmin(2, "admin2@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var tokenAntigoDoAdmin1 = tokens.Tokens.Single(t => t.AdminId == 1);

            await service.SolicitarExclusaoAsync(adminId: 2);
            var tokenDoAdmin2 = tokens.Tokens.Single(t => t.AdminId == 2);

            await service.SolicitarExclusaoAsync(adminId: 1);

            Assert.True(tokenAntigoDoAdmin1.Usado);
            Assert.False(tokenDoAdmin2.Usado);
        }

        [Fact]
        public async Task ConfirmarExclusao_ComCodigoCorreto_DesativaAProfessoraEMarcaOTokenComoUsado()
        {
            var (service, professores, tokens, admins, _, _) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var token = tokens.Tokens.Single();
            var codigo = ObterCodigoEmClaro(token);

            await service.ConfirmarExclusaoAsync(adminId: 1, codigo);

            Assert.False(professora.Ativo);
            Assert.True(token.Usado);
        }

        [Fact]
        public async Task ConfirmarExclusao_BemSucedida_NaoTocaEmNenhumOutroRepositorioAlemDoProfessorEDoToken()
        {
            // FR-007: exclusao e so Ativo=false no Professor; nenhum dado
            // vinculado (Turma/Aluno/Aula/Pagamento) e removido ou alterado -
            // os fakes desses repositorios nem existem no construtor do
            // ProfessorService, entao a prova e estrutural: o service so
            // depende de IProfessorRepository/IExclusaoProfessorTokenRepository/
            // IAdminRepository/ITokenService/IEmailSender/IRefreshTokenRepository/
            // IPasswordHasher - nunca de ITurmaRepository, IAulaRepository,
            // IAlunoRepository ou IPagamentoRepository.
            var (service, professores, tokens, admins, _, _) = CriarServico();
            professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var codigo = ObterCodigoEmClaro(tokens.Tokens.Single());

            await service.ConfirmarExclusaoAsync(adminId: 1, codigo);

            Assert.Equal(1, professores.SalvamentosRealizados);
        }

        [Fact]
        public async Task ConfirmarExclusao_ComCodigoErrado_RecusaSemAlterarProfessorNemToken()
        {
            var (service, professores, tokens, admins, _, _) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var token = tokens.Tokens.Single();

            await Assert.ThrowsAsync<ConflitoException>(() => service.ConfirmarExclusaoAsync(adminId: 1, "codigo-errado"));

            Assert.True(professora.Ativo);
            Assert.False(token.Usado);
        }

        [Fact]
        public async Task ConfirmarExclusao_ComCodigoExpirado_RecusaSemAlterarProfessor()
        {
            var (service, professores, tokens, admins, _, _) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var token = tokens.Tokens.Single();
            var codigo = ObterCodigoEmClaro(token);
            token.ExpiraEm = DateTime.UtcNow.AddMinutes(-1);

            await Assert.ThrowsAsync<ConflitoException>(() => service.ConfirmarExclusaoAsync(adminId: 1, codigo));

            Assert.True(professora.Ativo);
        }

        [Fact]
        public async Task ConfirmarExclusao_ComCodigoJaUsado_RecusaSemReativarAExclusao()
        {
            var (service, professores, tokens, admins, _, _) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var codigo = ObterCodigoEmClaro(tokens.Tokens.Single());
            await service.ConfirmarExclusaoAsync(adminId: 1, codigo);

            await Assert.ThrowsAsync<ConflitoException>(() => service.ConfirmarExclusaoAsync(adminId: 1, codigo));

            Assert.False(professora.Ativo);
        }

        [Fact]
        public async Task ConfirmarExclusao_ComCodigoAntigoInvalidadoPorNovoPedido_Recusa()
        {
            var (service, professores, tokens, admins, _, _) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var codigoAntigo = ObterCodigoEmClaro(tokens.Tokens.Single());

            await service.SolicitarExclusaoAsync(adminId: 1);

            await Assert.ThrowsAsync<ConflitoException>(() => service.ConfirmarExclusaoAsync(adminId: 1, codigoAntigo));
            Assert.True(professora.Ativo);
        }

        [Fact]
        public async Task Cancelar_NuncaChamarConfirmar_ENaoDeixarExpirarAindaMantemProfessoraAtiva()
        {
            // FR-010: cancelar (nunca confirmar) nao altera nada; so a
            // expiracao natural torna o codigo inutilizavel - nao ha acao de
            // "cancelar" explicita no backend (data-model.md).
            var (service, professores, tokens, admins, _, _) = CriarServico();
            var professora = professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await service.SolicitarExclusaoAsync(adminId: 1);
            var token = tokens.Tokens.Single();

            // "cancelar" = simplesmente nao chamar ConfirmarExclusaoAsync.
            Assert.True(professora.Ativo);
            Assert.False(token.Usado);

            token.ExpiraEm = DateTime.UtcNow.AddMinutes(-1);
            var codigo = ObterCodigoEmClaro(token);
            await Assert.ThrowsAsync<ConflitoException>(() => service.ConfirmarExclusaoAsync(adminId: 1, codigo));
            Assert.True(professora.Ativo);
        }

        [Fact]
        public async Task SolicitarExclusao_ComFalhaNoEnvioDeEmail_AindaAssimSalvaOTokenEPropagaAFalha()
        {
            // FR-011: o token ja foi salvo antes da tentativa de envio (mesmo
            // padrao de RecuperacaoSenhaService); diferente dela, aqui a
            // falha de envio e propagada como erro, nunca mascarada.
            var professores = new FakeProfessorRepositoryParaTeste();
            var tokens = new FakeExclusaoProfessorTokenRepositoryParaTeste();
            var admins = new FakeAdminRepositoryParaTeste();
            var tokenService = new FakeTokenServiceParaTeste();
            var emailSender = new FakeEmailSenderParaTeste { DeveFalhar = true };

            var service = new ProfessorService(
                professores,
                new FakeRefreshTokenRepositorySemUso(),
                new FakePasswordHasherSemUso(),
                tokens,
                admins,
                tokenService,
                emailSender);

            professores.Semear(CriarProfessoraAtiva());
            admins.Admins.Add(CriarAdmin(1, "admin1@spi.local"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SolicitarExclusaoAsync(adminId: 1));

            Assert.Single(tokens.Tokens);
            Assert.Empty(emailSender.EnviosRealizados);
        }

        [Fact]
        public void Validator_RejeitaCodigoVazio()
        {
            var validator = new ConfirmarExclusaoProfessorRequestValidator();

            var resultado = validator.Validate(new ConfirmarExclusaoProfessorRequest { Codigo = "" });

            Assert.False(resultado.IsValid);
        }

        private static string ObterCodigoEmClaro(ExclusaoProfessorToken token) =>
            token.TokenHash["hash-".Length..];
    }
}
