using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SPI.Api.Filters;
using SPI.Domain.Exceptions;
using Xunit;

namespace SPI.Application.Tests.Api
{
    // specs/065: o filtro substitui o try/catch repetido em cada acao dos controllers;
    // o status depende do que a acao declara em ProducesResponseType.
    public class ErrosDeNegocioFilterTests
    {
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

            new ErrosDeNegocioFilter().OnException(ctx);

            var resposta = Assert.IsType<NotFoundObjectResult>(ctx.Result);
            Assert.Equal("Turma nao encontrada.", resposta.Value);
            Assert.True(ctx.ExceptionHandled);
        }

        [Fact]
        public void Conflito_com_409_declarado_responde_409_com_a_mensagem()
        {
            var ctx = Contexto(new ConflitoException("Ja existe."), 200, 409, 500);

            new ErrosDeNegocioFilter().OnException(ctx);

            var resposta = Assert.IsType<ConflictObjectResult>(ctx.Result);
            Assert.Equal("Ja existe.", resposta.Value);
            Assert.True(ctx.ExceptionHandled);
        }

        [Fact]
        public void Conflito_sem_409_declarado_responde_500()
        {
            var ctx = Contexto(new ConflitoException("Ja existe."), 200, 500);

            new ErrosDeNegocioFilter().OnException(ctx);

            var resposta = Assert.IsType<ObjectResult>(ctx.Result);
            Assert.Equal(500, resposta.StatusCode);
        }

        [Fact]
        public void NaoEncontrado_sem_404_declarado_responde_500()
        {
            var ctx = Contexto(new NaoEncontradoException("Nao existe."), 200, 500);

            new ErrosDeNegocioFilter().OnException(ctx);

            var resposta = Assert.IsType<ObjectResult>(ctx.Result);
            Assert.Equal(500, resposta.StatusCode);
        }

        [Fact]
        public void Excecao_qualquer_responde_500_igual_ao_Problem_do_controller()
        {
            var excecao = new InvalidOperationException("falha qualquer");
            var ctx = Contexto(excecao, 200, 404, 409, 500);

            new ErrosDeNegocioFilter().OnException(ctx);

            var obtido = Assert.IsType<ObjectResult>(ctx.Result);
            var problema = Assert.IsType<ProblemDetails>(obtido.Value);
            Assert.Equal(500, obtido.StatusCode);
            Assert.Equal("Erro inesperado", problema.Title);
            Assert.Equal("falha qualquer", problema.Detail);
            Assert.Equal(500, problema.Status);
            Assert.True(ctx.ExceptionHandled);

            // O que o catch antigo devolvia: ControllerBase.Problem(title, detail, 500).
            var controller = new ControllerDeReferencia
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = ctx.HttpContext,
                    ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
                },
                ProblemDetailsFactory = ctx.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            };
            var esperado = Assert.IsType<ObjectResult>(controller.ProblemaComoAntes("falha qualquer"));
            var problemaEsperado = Assert.IsType<ProblemDetails>(esperado.Value);
            Assert.Equal(esperado.StatusCode, obtido.StatusCode);
            Assert.Equal(problemaEsperado.Title, problema.Title);
            Assert.Equal(problemaEsperado.Detail, problema.Detail);
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
