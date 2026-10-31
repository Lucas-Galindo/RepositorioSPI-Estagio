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
    /// Nao registra log: o tratamento que existia antes tambem nao registrava.
    /// </summary>
    public class ErrosDeNegocioFilter : IExceptionFilter
    {
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

        // Mesmo corpo que ControllerBase.Problem(title, detail, 500) monta.
        private static ObjectResult ErroInesperado(ExceptionContext context)
        {
            var fabrica = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problema = fabrica.CreateProblemDetails(
                context.HttpContext,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Erro inesperado",
                detail: context.Exception.Message);
            return new ObjectResult(problema) { StatusCode = problema.Status };
        }
    }
}
