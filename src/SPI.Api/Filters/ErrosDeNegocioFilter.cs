using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SPI.Domain.Exceptions;

namespace SPI.Api.Filters
{
    /// <summary>
    /// Traduz as excecoes de negocio em respostas HTTP, no lugar do try/catch que cada acao repetia.
    /// O status e decidido pelo que a propria acao declara em <c>[ProducesResponseType]</c>:
    /// NaoEncontradoException vira 404 e ConflitoException vira 409 so quando a acao declara esse status;
    /// qualquer outra excecao (ou uma dessas duas numa acao que nao declara) vira 500 "Erro inesperado".
    /// Acoes com resposta propria para uma excecao (ex.: login) continuam com o catch no controller.
    /// </summary>
    public class ErrosDeNegocioFilter : IExceptionFilter
    {
        private readonly ILogger<ErrosDeNegocioFilter> _logger;
        private readonly IHostEnvironment _ambiente;

        public ErrosDeNegocioFilter(ILogger<ErrosDeNegocioFilter> logger, IHostEnvironment ambiente)
        {
            _logger = logger;
            _ambiente = ambiente;
        }

        public void OnException(ExceptionContext context)
        {
            context.Result = context.Exception switch
            {
                NaoEncontradoException e when Declara(context, StatusCodes.Status404NotFound) => new NotFoundObjectResult(e.Message),
                ConflitoException e when Declara(context, StatusCodes.Status409Conflict) => new ConflictObjectResult(e.Message),
                _ => ErroInesperado(context)
            };
            context.ExceptionHandled = true;
        }

        private static bool Declara(ExceptionContext context, int statusCode) =>
            context.ActionDescriptor.EndpointMetadata
                .OfType<ProducesResponseTypeAttribute>()
                .Any(a => a.StatusCode == statusCode);

        // 500: a excecao completa vai para o LOG do servidor; o cliente nunca recebe a mensagem dela
        // fora do ambiente de desenvolvimento, porque ela pode trazer texto interno (consulta SQL,
        // caminho de arquivo, nome de servidor). Ele recebe so o codigo de rastreio (traceId), que a
        // professora informa e permite achar a excecao no log. O restante do corpo e o mesmo que
        // ControllerBase.Problem(title, statusCode) monta.
        private ObjectResult ErroInesperado(ExceptionContext context)
        {
            var fabrica = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problema = fabrica.CreateProblemDetails(
                context.HttpContext,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Erro inesperado");

            var traceId = problema.Extensions.TryGetValue("traceId", out var valor) ? valor?.ToString() : context.HttpContext.TraceIdentifier;
            _logger.LogError(context.Exception, "Erro inesperado em {Acao}. TraceId: {TraceId}", context.ActionDescriptor.DisplayName, traceId);

            problema.Detail = _ambiente.IsDevelopment()
                ? context.Exception.Message
                : $"Ocorreu um erro inesperado. Se o problema continuar, informe o codigo {traceId}.";

            return new ObjectResult(problema) { StatusCode = problema.Status };
        }
    }
}
