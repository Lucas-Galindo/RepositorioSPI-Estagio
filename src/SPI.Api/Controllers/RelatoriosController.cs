using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Application.Relatorios.Services;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;

namespace SPI.Api.Controllers
{
    [Route("api/relatorios")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class RelatoriosController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;

        public RelatoriosController(IRelatorioService relatorioService)
        {
            _relatorioService = relatorioService;
        }

        [HttpGet("agenda")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio de Agenda (Estoria 12)
        /// </summary>
        /// <param name="inicio">Inicio do periodo</param>
        /// <param name="fim">Fim do periodo</param>
        /// <param name="status">Filtro por status da aula</param>
        /// <param name="turmaId">Filtro por turma</param>
        /// <param name="alunoId">Filtro por aluno</param>
        /// <returns>Retorna a lista de aulas que atendem aos filtros</returns>
        public async Task<IActionResult> Agenda(
            [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] string? status,
            [FromQuery] int? turmaId, [FromQuery] int? alunoId)
        {
            try
            {
                return Ok(await _relatorioService.ObterAgendaAsync(inicio, fim, status, turmaId, alunoId));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("historico-aluno/{alunoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio de Historico do Aluno (Estoria 13)
        /// </summary>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="inicio">Inicio do periodo</param>
        /// <param name="fim">Fim do periodo</param>
        /// <param name="status">Filtro por status da aula</param>
        /// <param name="turmaId">Filtro por turma</param>
        /// <returns>Retorna o historico de aulas do aluno e o percentual de frequencia</returns>
        public async Task<IActionResult> HistoricoAluno(
            int alunoId, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] string? status, [FromQuery] int? turmaId)
        {
            try
            {
                return Ok(await _relatorioService.ObterHistoricoAlunoAsync(alunoId, inicio, fim, status, turmaId));
            }
            catch (NaoEncontradoException e)
            {
                return NotFound(e.Message);
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("pagamentos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio de Pagamentos (Estoria 14)
        /// </summary>
        /// <param name="alunoId">Filtro por aluno</param>
        /// <param name="inicio">Inicio do periodo (pela Data de Vencimento)</param>
        /// <param name="fim">Fim do periodo (pela Data de Vencimento)</param>
        /// <param name="status">Filtro por status (Pendente/Atrasado/Pago)</param>
        /// <returns>Retorna os pagamentos que atendem aos filtros e o total pendente consolidado</returns>
        public async Task<IActionResult> Pagamentos(
            [FromQuery] int? alunoId, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] string? status)
        {
            try
            {
                return Ok(await _relatorioService.ObterPagamentosAsync(alunoId, inicio, fim, status));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("financeiro")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio Financeiro (Estoria 15)
        /// </summary>
        /// <param name="inicio">Inicio do periodo (pela Data de Pagamento)</param>
        /// <param name="fim">Fim do periodo (pela Data de Pagamento)</param>
        /// <param name="formaPagamentoId">Filtro por forma de pagamento</param>
        /// <param name="alunoId">Filtro por aluno</param>
        /// <returns>Retorna o total recebido e o detalhamento por forma de pagamento e por aluno</returns>
        public async Task<IActionResult> Financeiro(
            [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] int? formaPagamentoId, [FromQuery] int? alunoId)
        {
            try
            {
                return Ok(await _relatorioService.ObterFinanceiroAsync(inicio, fim, formaPagamentoId, alunoId));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("periodo-agenda")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio do Periodo da Agenda (Estoria 16)
        /// </summary>
        /// <param name="inicio">Inicio do periodo (padrao: inicio do mes corrente)</param>
        /// <param name="fim">Fim do periodo (padrao: fim do mes corrente)</param>
        /// <param name="turmaId">Filtro por turma</param>
        /// <param name="materiaId">Filtro por materia</param>
        /// <returns>Retorna indicadores agregados: aulas por dia, comparativo por status e taxa de ocupacao</returns>
        public async Task<IActionResult> PeriodoAgenda(
            [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, [FromQuery] int? turmaId, [FromQuery] int? materiaId)
        {
            try
            {
                var hoje = DateOnly.FromDateTime(DateTime.Now);
                var periodoInicio = inicio ?? new DateOnly(hoje.Year, hoje.Month, 1);
                var periodoFim = fim ?? periodoInicio.AddMonths(1).AddDays(-1);

                return Ok(await _relatorioService.ObterPeriodoAgendaAsync(periodoInicio, periodoFim, turmaId, materiaId));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("alunos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio de Alunos (Estoria 17)
        /// </summary>
        /// <param name="nome">Filtro por nome (busca parcial)</param>
        /// <param name="turmaId">Filtro por turma</param>
        /// <param name="ativo">Filtro por status de atividade</param>
        /// <returns>Retorna os alunos que atendem aos filtros, com dados de contato, turmas, valor e frequencia</returns>
        public async Task<IActionResult> Alunos([FromQuery] string? nome, [FromQuery] int? turmaId, [FromQuery] bool? ativo)
        {
            try
            {
                return Ok(await _relatorioService.ObterAlunosAsync(nome, turmaId, ativo));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("materias")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para o Relatorio de Materias (Estoria 18)
        /// </summary>
        /// <param name="nivel">Filtro por nivel (busca parcial)</param>
        /// <param name="inicio">Inicio do periodo considerado para contagem de aulas/alunos</param>
        /// <param name="fim">Fim do periodo considerado</param>
        /// <returns>Retorna as materias com a quantidade de aulas e de alunos atendidos no periodo</returns>
        public async Task<IActionResult> Materias([FromQuery] string? nivel, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim)
        {
            try
            {
                return Ok(await _relatorioService.ObterMateriasAsync(nivel, inicio, fim));
            }
            catch (Exception e)
            {
                return Problem(title: "Erro inesperado", detail: e.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
