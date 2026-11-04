using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SPI.Api.Filters;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Api
{
    // specs/065: o filtro substitui o try/catch repetido em cada acao dos controllers;
    // o status depende do que a acao declara em ProducesResponseType.
    // Revisao de 2026-11-04: o 500 nao devolve a mensagem da excecao fora de Development (ela pode
    // trazer texto interno) e registra a excecao completa no log.
    public class ErrosDeNegocioFilterTests
    {
        private const string TextoInterno = "Table 'spi_db.aula' doesn't exist at C:\\inetpub\\spi\\src\\Repo.cs:line 42";

        private sealed class Ambiente : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;
            public string ApplicationName { get; set; } = "SPI.Api";
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private sealed class LoggerCaptura : ILogger<ErrosDeNegocioFilter>
        {
            public List<(LogLevel Nivel, Exception? Excecao, string Mensagem)> Registros { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                Registros.Add((logLevel, exception, formatter(state, exception)));
        }

        private static ErrosDeNegocioFilter NovoFiltro(out LoggerCaptura logger, string ambiente = "Production")
        {
            logger = new LoggerCaptura();
            return new ErrosDeNegocioFilter(logger, new Ambiente { EnvironmentName = ambiente });
        }

        private static ExceptionContext Contexto(Exception excecao, params int[] statusDeclarados)
        {
            var servicos = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
            var http = new DefaultHttpContext { RequestServices = servicos, TraceIdentifier = "trace-1" };
            var descritor = new ActionDescriptor
            {
                EndpointMetadata = statusDeclarados
                    .Select(s => (object)new ProducesResponseTypeAttribute(s))
                    .ToList()
            };
            return new ExceptionContext(new ActionContext(http, new RouteData(), descritor), new List<IFilterMetadata>())
            {
                Exception = excecao
            };
        }

        [Fact]
        public void NaoEncontrado_com_404_declarado_responde_404_com_a_mensagem()
        {
            var ctx = Contexto(new NaoEncontradoException("Turma nao encontrada."), 200, 404, 500);

            NovoFiltro(out _).OnException(ctx);

            var resposta = Assert.IsType<NotFoundObjectResult>(ctx.Result);
            Assert.Equal("Turma nao encontrada.", resposta.Value);
            Assert.True(ctx.ExceptionHandled);
        }

        [Fact]
        public void Conflito_com_409_declarado_responde_409_com_a_mensagem()
        {
            var ctx = Contexto(new ConflitoException("Ja existe."), 200, 409, 500);

            NovoFiltro(out _).OnException(ctx);

            var resposta = Assert.IsType<ConflictObjectResult>(ctx.Result);
            Assert.Equal("Ja existe.", resposta.Value);
            Assert.True(ctx.ExceptionHandled);
        }

        [Fact]
        public void Conflito_sem_409_declarado_responde_500_sem_a_mensagem()
        {
            var ctx = Contexto(new ConflitoException("Ja existe."), 200, 500);

            NovoFiltro(out _).OnException(ctx);

            var resposta = Assert.IsType<ObjectResult>(ctx.Result);
            Assert.Equal(500, resposta.StatusCode);
            Assert.DoesNotContain("Ja existe.", Assert.IsType<ProblemDetails>(resposta.Value).Detail);
        }

        [Fact]
        public void NaoEncontrado_sem_404_declarado_responde_500_sem_a_mensagem()
        {
            var ctx = Contexto(new NaoEncontradoException("Nao existe."), 200, 500);

            NovoFiltro(out _).OnException(ctx);

            var resposta = Assert.IsType<ObjectResult>(ctx.Result);
            Assert.Equal(500, resposta.StatusCode);
            Assert.DoesNotContain("Nao existe.", Assert.IsType<ProblemDetails>(resposta.Value).Detail);
        }

        // --- 500: nada de texto interno para o cliente fora de Development ---

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        public void Em_producao_o_500_nao_vaza_a_mensagem_da_excecao_e_traz_o_codigo_de_rastreio(string ambiente)
        {
            var ctx = Contexto(new InvalidOperationException(TextoInterno), 200, 404, 409, 500);

            NovoFiltro(out _, ambiente).OnException(ctx);

            var obtido = Assert.IsType<ObjectResult>(ctx.Result);
            var problema = Assert.IsType<ProblemDetails>(obtido.Value);
            Assert.Equal(500, obtido.StatusCode);
            Assert.Equal("Erro inesperado", problema.Title);
            Assert.Equal(500, problema.Status);
            Assert.DoesNotContain("spi_db", problema.Detail);
            Assert.DoesNotContain("inetpub", problema.Detail);
            Assert.DoesNotContain(".cs", problema.Detail);
            Assert.Contains("trace-1", problema.Detail);
            Assert.Equal("trace-1", problema.Extensions["traceId"]?.ToString());
            Assert.True(ctx.ExceptionHandled);
        }

        [Fact]
        public void O_500_registra_a_excecao_completa_no_log_com_o_mesmo_codigo_de_rastreio()
        {
            var excecao = new InvalidOperationException(TextoInterno);
            var ctx = Contexto(excecao, 200, 500);

            NovoFiltro(out var logger).OnException(ctx);

            var registro = Assert.Single(logger.Registros);
            Assert.Equal(LogLevel.Error, registro.Nivel);
            Assert.Same(excecao, registro.Excecao);
            Assert.Contains("trace-1", registro.Mensagem);
        }

        [Fact]
        public void Em_development_o_500_mantem_a_mensagem_da_excecao_para_depuracao()
        {
            var ctx = Contexto(new InvalidOperationException("falha qualquer"), 200, 500);

            NovoFiltro(out _, Environments.Development).OnException(ctx);

            var problema = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(ctx.Result).Value);
            Assert.Equal("falha qualquer", problema.Detail);
        }

        [Fact]
        public void O_corpo_do_500_tem_a_mesma_estrutura_do_Problem_do_controller()
        {
            var ctx = Contexto(new InvalidOperationException("falha qualquer"), 200, 404, 409, 500);

            NovoFiltro(out _).OnException(ctx);

            var obtido = Assert.IsType<ObjectResult>(ctx.Result);
            var problema = Assert.IsType<ProblemDetails>(obtido.Value);

            // O que o catch antigo devolvia: ControllerBase.Problem(title, statusCode, ...). So o texto do
            // detalhe muda (agora generico); titulo, status, tipo, instancia e extensoes sao os mesmos.
            var controller = new ControllerDeReferencia
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = ctx.HttpContext,
                    ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
                },
                ProblemDetailsFactory = ctx.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            };
            var esperado = Assert.IsType<ObjectResult>(controller.ProblemaComoAntes("qualquer"));
            var problemaEsperado = Assert.IsType<ProblemDetails>(esperado.Value);
            Assert.Equal(esperado.StatusCode, obtido.StatusCode);
            Assert.Equal(problemaEsperado.Title, problema.Title);
            Assert.Equal(problemaEsperado.Status, problema.Status);
            Assert.Equal(problemaEsperado.Type, problema.Type);
            Assert.Equal(problemaEsperado.Instance, problema.Instance);
            Assert.Equal(problemaEsperado.Extensions.Keys.OrderBy(k => k), problema.Extensions.Keys.OrderBy(k => k));
        }

        private sealed class ControllerDeReferencia : ControllerBase
        {
            public IActionResult ProblemaComoAntes(string mensagem) =>
                Problem(title: "Erro inesperado", detail: mensagem, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
