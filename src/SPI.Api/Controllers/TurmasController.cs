using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPI.Api.Extensions;
using SPI.Application.Turmas.Dtos;
using SPI.Application.Turmas.Validators;
using SPI.Application.Turmas.Services;
using SPI.Application.VinculosCobranca.Dtos;
using SPI.Application.VinculosCobranca.Services;
using SPI.Domain.Enums;

namespace SPI.Api.Controllers
{
    [Route("api/turmas")]
    [ApiController]
    [Authorize(Roles = nameof(PerfilUsuario.Professor))]
    public class TurmasController : ControllerBase
    {
        private readonly ITurmaService _turmaService;
        private readonly IValidator<TurmaRequest> _validator;
        private readonly ISaidaPacoteService _saidaPacoteService;
        private readonly IValidator<SaidaPacoteRequest> _saidaValidator;
        private readonly IValidator<ConfirmarSaidaPacoteRequest> _confirmarSaidaValidator;
        private readonly ILogger<TurmasController> _logger;

        public TurmasController(
            ITurmaService turmaService,
            IValidator<TurmaRequest> validator,
            ISaidaPacoteService saidaPacoteService,
            IValidator<SaidaPacoteRequest> saidaValidator,
            IValidator<ConfirmarSaidaPacoteRequest> confirmarSaidaValidator,
            ILogger<TurmasController> logger)
        {
            _turmaService = turmaService;
            _validator = validator;
            _saidaPacoteService = saidaPacoteService;
            _saidaValidator = saidaValidator;
            _confirmarSaidaValidator = confirmarSaidaValidator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para listar e buscar turmas
        /// </summary>
        /// <param name="nome">Filtro por nome (busca parcial)</param>
        /// <param name="ativo">Filtro por status</param>
        /// <param name="materiaId">Filtro por materia da turma (specs/076)</param>
        /// <param name="semMateria">true = so turmas sem materia (specs/076); nao combina com materiaId</param>
        /// <returns>Retorna a lista de turmas que atendem aos filtros</returns>
        public async Task<IActionResult> Listar([FromQuery] string? nome, [FromQuery] bool? ativo, [FromQuery] int? materiaId, [FromQuery] bool? semMateria)
        {
            if (materiaId.HasValue && semMateria == true)
            {
                return BadRequest(new[] { "Informe materiaId ou semMateria, nao os dois." });
            }

            return Ok(await _turmaService.ListarAsync(nome, ativo, materiaId: materiaId, semMateria: semMateria));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para obter uma turma por id, com os alunos vinculados
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <returns>Retorna a turma encontrada</returns>
        public async Task<IActionResult> ObterPorId(int id)
        {
            return Ok(await _turmaService.ObterPorIdAsync(id));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para cadastrar uma nova turma (professora associada automaticamente)
        /// </summary>
        /// <param name="request">Nome da turma</param>
        /// <returns>Retorna a turma cadastrada</returns>
        public async Task<IActionResult> Cadastrar([FromBody] TurmaRequest request)
        {
            // specs/063: no cadastro vale tambem o conjunto de regras que exige as datas da
            // turma de pacote (na edicao, uma turma migrada pode ficar sem datas).
            var validacao = await _validator.ValidateAsync(request, opcoes =>
                opcoes.IncludeRuleSets(TurmaRequestValidator.RegrasDeCadastro).IncludeRulesNotInRuleSet());
            if (!validacao.IsValid)
            {
                return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
            }

            var response = await _turmaService.CadastrarAsync(User.ObterUsuarioId(), request);
            _logger.LogInformation("Turma {Nome} cadastrada", request.Nome);
            return CreatedAtAction(nameof(ObterPorId), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para atualizar o nome de uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="request">Novo nome da turma</param>
        /// <returns>Retorna a turma atualizada</returns>
        public async Task<IActionResult> Atualizar(int id, [FromBody] TurmaRequest request)
        {
            var validacao = await _validator.ValidateAsync(request);
            if (!validacao.IsValid)
            {
                return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
            }

            var response = await _turmaService.AtualizarAsync(id, request);
            _logger.LogInformation("Turma {Id} atualizada", id);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para excluir (logicamente) uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <returns>Sem conteudo em caso de sucesso</returns>
        public async Task<IActionResult> Excluir(int id)
        {
            await _turmaService.ExcluirAsync(id);
            _logger.LogInformation("Turma {Id} excluida (logicamente)", id);
            return Ok();
        }

        [HttpPatch("{id}/reativar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para reativar uma turma desativada
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <returns>A turma reativada</returns>
        public async Task<IActionResult> Reativar(int id)
        {
            var response = await _turmaService.ReativarAsync(id);
            _logger.LogInformation("Turma {Id} reativada", id);
            return Ok(response);
        }

        [HttpPost("{id}/alunos/{alunoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para vincular um aluno a uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="alunoId">Id do aluno</param>
        /// <returns>Retorna a turma atualizada com o novo aluno</returns>
        public async Task<IActionResult> VincularAluno(int id, int alunoId)
        {
            var response = await _turmaService.VincularAlunoAsync(id, alunoId);
            _logger.LogInformation("Aluno {AlunoId} vinculado a turma {TurmaId}", alunoId, id);
            return Ok(response);
        }

        [HttpPost("{id}/alunos/{alunoId}/saida/previa")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Resumo do acerto da saida de um aluno de turma de pacote (specs/078): consumido, devido, pago informado e a
        /// consequencia. Nao grava nada.
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="request">Quanto o aluno ja pagou e, se quiser, o vencimento da conta da diferenca</param>
        /// <returns>Retorna o resumo do acerto</returns>
        public async Task<IActionResult> PreVisualizarSaida(int id, int alunoId, [FromBody] SaidaPacoteRequest request)
        {
            var validacao = await _saidaValidator.ValidateAsync(request);
            if (!validacao.IsValid)
            {
                return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
            }

            return Ok(await _saidaPacoteService.PreVisualizarAsync(id, alunoId, request));
        }

        [HttpPost("{id}/alunos/{alunoId}/saida")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Registra a saida do aluno da turma de pacote (specs/078), de forma atomica: desvincula, tira das aulas que ainda
        /// nao comecaram, encerra o pacote e gera a conta a receber da diferenca (quando ele pagou menos do que consumiu)
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="alunoId">Id do aluno</param>
        /// <param name="request">O que foi informado e os numeros do resumo confirmado pela professora</param>
        /// <returns>Retorna o acerto e o id da conta gerada (se houve)</returns>
        public async Task<IActionResult> RegistrarSaida(int id, int alunoId, [FromBody] ConfirmarSaidaPacoteRequest request)
        {
            var validacao = await _confirmarSaidaValidator.ValidateAsync(request);
            if (!validacao.IsValid)
            {
                return BadRequest(validacao.Errors.Select(e => e.ErrorMessage));
            }

            var response = await _saidaPacoteService.RegistrarAsync(id, alunoId, request);
            _logger.LogInformation("Saida do aluno {AlunoId} da turma {TurmaId} registrada", alunoId, id);
            return Ok(response);
        }

        [HttpDelete("{id}/alunos/{alunoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        /// <summary>
        /// Endpoint para desvincular um aluno de uma turma
        /// </summary>
        /// <param name="id">Id da turma</param>
        /// <param name="alunoId">Id do aluno</param>
        /// <returns>Retorna a turma atualizada</returns>
        public async Task<IActionResult> DesvincularAluno(int id, int alunoId)
        {
            var response = await _turmaService.DesvincularAlunoAsync(id, alunoId);
            _logger.LogInformation("Aluno {AlunoId} desvinculado da turma {TurmaId}", alunoId, id);
            return Ok(response);
        }
    }
}
