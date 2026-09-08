using SPI.Application.Pagamentos.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Pagamentos.Services
{
    public class PagamentoService : IPagamentoService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IAlunoRepository _alunoRepository;
        private readonly IFormaPagamentoRepository _formaPagamentoRepository;
        private readonly ICategoriaReceitaRepository _categoriaReceitaRepository;
        private readonly IAulaRepository _aulaRepository;

        public PagamentoService(
            IPagamentoRepository pagamentoRepository,
            IAlunoRepository alunoRepository,
            IFormaPagamentoRepository formaPagamentoRepository,
            ICategoriaReceitaRepository categoriaReceitaRepository,
            IAulaRepository aulaRepository)
        {
            _pagamentoRepository = pagamentoRepository;
            _alunoRepository = alunoRepository;
            _formaPagamentoRepository = formaPagamentoRepository;
            _categoriaReceitaRepository = categoriaReceitaRepository;
            _aulaRepository = aulaRepository;
        }

        public async Task<List<PagamentoResponse>> ListarAsync(
            int? alunoId,
            string? status,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default)
        {
            // "Atrasado" nao e persistido: e um Pendente cuja DataVencimento ja passou
            // (ver 02_criacao_tabelas.sql). Ao filtrar por Atrasado, buscamos os
            // Pendentes e deixamos o Mapear calcular/expor o status efetivo.
            var statusConsulta = status == "Atrasado" ? "Pendente" : status;

            var pagamentos = await _pagamentoRepository.ListarAsync(alunoId, statusConsulta, vencimentoInicio, vencimentoFim, cancellationToken);
            var mapeados = pagamentos.Select(Mapear);

            return status == "Atrasado"
                ? mapeados.Where(p => p.Status == "Atrasado").ToList()
                : mapeados.ToList();
        }

        public async Task<PagamentoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var pagamento = await _pagamentoRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Pagamento nao encontrado.");

            return Mapear(pagamento);
        }

        public async Task<PagamentoResponse> RegistrarAsync(RegistrarPagamentoRequest request, CancellationToken cancellationToken = default)
        {
            if (await _alunoRepository.ObterPorIdAsync(request.AlunoId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Aluno nao encontrado.");
            }

            if (await _formaPagamentoRepository.ObterPorIdAsync(request.FormaPagamentoId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Forma de pagamento nao encontrada.");
            }

            if (request.CategoriaReceitaId.HasValue
                && await _categoriaReceitaRepository.ObterPorIdAsync(request.CategoriaReceitaId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Categoria de receita nao encontrada.");
            }

            foreach (var aulaId in request.AulaIds)
            {
                if (await _aulaRepository.ObterPorIdAsync(aulaId, cancellationToken) is null)
                {
                    throw new NaoEncontradoException($"Aula {aulaId} nao encontrada.");
                }
            }

            var pagamento = new Pagamento
            {
                AlunoId = request.AlunoId,
                Descricao = request.Descricao,
                CategoriaReceitaId = request.CategoriaReceitaId,
                FormaPagamentoId = request.FormaPagamentoId,
                DataVencimento = request.DataVencimento,
                Competencia = request.Competencia,
                ValorFinal = request.ValorFinal,
                Status = request.Status,
                Observacoes = request.Observacoes,
                DataPagamento = request.Status == "Pago" ? DateOnly.FromDateTime(DateTime.UtcNow) : null
            };

            await _pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

            foreach (var aulaId in request.AulaIds)
            {
                await _pagamentoRepository.VincularAulaAsync(pagamento.Id, aulaId, cancellationToken);
            }
            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

            var pagamentoCompleto = await _pagamentoRepository.ObterPorIdAsync(pagamento.Id, cancellationToken);
            return Mapear(pagamentoCompleto!);
        }

        public async Task<PagamentoResponse> AtualizarAsync(int id, AtualizarPagamentoRequest request, CancellationToken cancellationToken = default)
        {
            var pagamento = await _pagamentoRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Pagamento nao encontrado.");

            if (request.FormaPagamentoId.HasValue
                && await _formaPagamentoRepository.ObterPorIdAsync(request.FormaPagamentoId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Forma de pagamento nao encontrada.");
            }

            if (request.CategoriaReceitaId.HasValue
                && await _categoriaReceitaRepository.ObterPorIdAsync(request.CategoriaReceitaId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Categoria de receita nao encontrada.");
            }

            // Campos nao informados no request mantem o valor atual (mesma
            // convencao usada em AlunoService/MateriaService/ProfessorService,
            // para evitar apagar dados existentes em uma atualizacao parcial).
            pagamento.Descricao = request.Descricao ?? pagamento.Descricao;
            pagamento.CategoriaReceitaId = request.CategoriaReceitaId ?? pagamento.CategoriaReceitaId;
            pagamento.FormaPagamentoId = request.FormaPagamentoId ?? pagamento.FormaPagamentoId;
            pagamento.DataVencimento = request.DataVencimento ?? pagamento.DataVencimento;
            pagamento.ValorFinal = request.ValorFinal ?? pagamento.ValorFinal;
            pagamento.Competencia = request.Competencia ?? pagamento.Competencia;
            pagamento.Observacoes = request.Observacoes ?? pagamento.Observacoes;

            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

            var pagamentoAtualizado = await _pagamentoRepository.ObterPorIdAsync(id, cancellationToken);
            return Mapear(pagamentoAtualizado!);
        }

        public async Task<PagamentoResponse> AtualizarStatusAsync(int id, AtualizarStatusPagamentoRequest request, CancellationToken cancellationToken = default)
        {
            if (await _pagamentoRepository.ObterPorIdAsync(id, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Pagamento nao encontrado.");
            }

            // sp_atualizar_status_pagamento ja preenche data_pagamento
            // automaticamente quando o novo status e 'Pago'.
            await _pagamentoRepository.AtualizarStatusViaProcedureAsync(id, request.Status, cancellationToken);

            var pagamentoAtualizado = await _pagamentoRepository.ObterPorIdAsync(id, cancellationToken);
            return Mapear(pagamentoAtualizado!);
        }

        private static PagamentoResponse Mapear(Pagamento pagamento)
        {
            var statusEfetivo = pagamento.Status == "Pendente" && pagamento.DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow)
                ? "Atrasado"
                : pagamento.Status;

            return new PagamentoResponse
            {
                Id = pagamento.Id,
                AlunoId = pagamento.AlunoId,
                AlunoNome = pagamento.Aluno.Nome,
                Descricao = pagamento.Descricao,
                CategoriaReceitaId = pagamento.CategoriaReceitaId,
                CategoriaReceitaNome = pagamento.CategoriaReceita?.Nome,
                FormaPagamentoId = pagamento.FormaPagamentoId,
                FormaPagamentoNome = pagamento.FormaPagamento?.Forma,
                DataVencimento = pagamento.DataVencimento,
                DataPagamento = pagamento.DataPagamento,
                Competencia = pagamento.Competencia,
                ValorFinal = pagamento.ValorFinal,
                Observacoes = pagamento.Observacoes,
                Status = statusEfetivo,
                AulaIds = pagamento.PagamentosAula.Select(pa => pa.AulaId).ToList()
            };
        }
    }
}
