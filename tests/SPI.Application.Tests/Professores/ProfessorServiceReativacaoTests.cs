using SPI.Application.Professores.Services;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Professores
{
    public class ProfessorServiceReativacaoTests
    {
        private static ProfessorService CriarServico(FakeProfessorRepositoryParaTeste professores) => new(
            professores,
            new FakeRefreshTokenRepositorySemUso(),
            new FakePasswordHasherSemUso(),
            new FakeExclusaoProfessorTokenRepositoryParaTeste(),
            new FakeAdminRepositoryParaTeste(),
            new FakeTokenServiceParaTeste(),
            new FakeEmailSenderParaTeste());

        [Fact]
        public async Task Reativar_ProfessoraInativa_DefineAtivoTrueSemExigirNenhumCodigo()
        {
            var professores = new FakeProfessorRepositoryParaTeste();
            var professora = professores.Semear(new Professor
            {
                Nome = "Professora Teste",
                Cpf = "11111111111",
                Email = "professora@spi.local",
                Senha = "hash",
                Ativo = false
            });
            var service = CriarServico(professores);

            var resultado = await service.ReativarAsync();

            Assert.True(professora.Ativo);
            Assert.True(resultado.Ativo);
        }

        [Fact]
        public async Task Reativar_ProfessoraJaAtiva_RetornaConflitoSemAlterarNada()
        {
            var professores = new FakeProfessorRepositoryParaTeste();
            var professora = professores.Semear(new Professor
            {
                Nome = "Professora Teste",
                Cpf = "11111111111",
                Email = "professora@spi.local",
                Senha = "hash",
                Ativo = true
            });
            var service = CriarServico(professores);

            await Assert.ThrowsAsync<ConflitoException>(() => service.ReativarAsync());

            Assert.True(professora.Ativo);
        }

        [Fact]
        public async Task Reativar_SemNenhumaProfessoraCadastrada_RetornaNaoEncontrado()
        {
            var professores = new FakeProfessorRepositoryParaTeste();
            var service = CriarServico(professores);

            await Assert.ThrowsAsync<NaoEncontradoException>(() => service.ReativarAsync());
        }
    }
}
