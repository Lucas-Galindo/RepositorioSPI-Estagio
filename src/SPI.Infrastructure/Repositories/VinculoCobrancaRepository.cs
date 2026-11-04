using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using SPI.Domain.Entities;
using SPI.Domain.Enums;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;
using SPI.Infrastructure.Persistence;

namespace SPI.Infrastructure.Repositories
{
    public class VinculoCobrancaRepository : IVinculoCobrancaRepository
    {
        private readonly SpiDbContext _dbContext;

        public VinculoCobrancaRepository(SpiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<VinculoCobranca?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca.Include(v => v.Turma).FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        public async Task<List<VinculoCobranca>> ListarPorAlunoAsync(int alunoId, bool? ativo, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.VinculosCobranca.Include(v => v.Turma).Where(v => v.AlunoId == alunoId);

            if (ativo.HasValue)
            {
                query = query.Where(v => v.Ativo == ativo.Value);
            }

            return await query
                .OrderByDescending(v => v.Ativo)
                .ThenBy(v => v.TurmaId == null)
                .ThenBy(v => v.Turma!.Nome)
                .ThenBy(v => v.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<List<VinculoCobranca>> ListarPlanosAtivosAsync(CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca
                .Include(v => v.Aluno)
                .Where(v => v.Ativo && (v.Modalidade == ModalidadeCobranca.Semestral || v.Modalidade == ModalidadeCobranca.Anual))
                .ToListAsync(cancellationToken);

        public Task<List<VinculoCobranca>> ListarAtivosDosAlunosAsync(IReadOnlyCollection<int> alunoIds, CancellationToken cancellationToken = default) =>
            alunoIds.Count == 0
                ? Task.FromResult(new List<VinculoCobranca>())
                : _dbContext.VinculosCobranca
                    .Include(v => v.Turma)
                    .Where(v => v.Ativo && alunoIds.Contains(v.AlunoId))
                    .OrderBy(v => v.Id)
                    .ToListAsync(cancellationToken);

        public Task<List<VinculoCobranca>> ListarAtivosPorModalidadeAsync(ModalidadeCobranca modalidade, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca.Include(v => v.Aluno).Where(v => v.Ativo && v.Modalidade == modalidade).ToListAsync(cancellationToken);

        public Task<List<VinculoCobranca>> ListarPacotesAsync(int? alunoId, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca
                .Include(v => v.Aluno)
                .Where(v => v.Modalidade == ModalidadeCobranca.Pacote && (!alunoId.HasValue || v.AlunoId == alunoId.Value))
                .ToListAsync(cancellationToken);

        public Task<List<VinculoCobranca>> ListarPacotesAtivosDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca
                .Include(v => v.Aluno)
                .Where(v => v.Ativo && v.Modalidade == ModalidadeCobranca.Pacote && v.TurmaId == turmaId)
                .ToListAsync(cancellationToken);

        public Task<List<VinculoCobranca>> ListarPacotesAtivosDeTurmasEncerradasAsync(DateOnly hoje, CancellationToken cancellationToken = default) =>
            _dbContext.VinculosCobranca
                .Include(v => v.Turma)
                .Where(v => v.Ativo
                    && v.Modalidade == ModalidadeCobranca.Pacote
                    && v.TurmaId != null
                    && (!v.Turma!.Ativo || (v.Turma.DataFim != null && v.Turma.DataFim < hoje)))
                .ToListAsync(cancellationToken);

        public async Task AdicionarAsync(VinculoCobranca vinculo, CancellationToken cancellationToken = default) =>
            await _dbContext.VinculosCobranca.AddAsync(vinculo, cancellationToken);

        public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException e) when (e.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // Corrida entre duas requisicoes: um indice unico barrou a segunda gravacao -- o de Avulsas
                // ativas (uq_vinculocobranca_chave_avulsa_ativa, specs/056) ou o de uma parcela por mes no
                // vinculo (uq_pagamento_vinculo_competencia, specs/062). A mensagem nao aponta qual.
                throw new ConflitoException("Ja existe um vinculo ativo ou parcela duplicada para este contexto.");
            }
        }
    }
}
