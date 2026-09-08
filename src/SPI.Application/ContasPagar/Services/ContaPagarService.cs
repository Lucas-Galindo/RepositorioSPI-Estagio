using SPI.Application.ContasPagar.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.ContasPagar.Services
{
    public class ContaPagarService : IContaPagarService
    {
        private readonly IContaPagarRepository _contaPagarRepository;
        private readonly ICategoriaDespesaRepository _categoriaDespesaRepository;
        private readonly IFormaPagamentoRepository _formaPagamentoRepository;

        public ContaPagarService(
            IContaPagarRepository contaPagarRepository,
            ICategoriaDespesaRepository categoriaDespesaRepository,
            IFormaPagamentoRepository formaPagamentoRepository)
        {
            _contaPagarRepository = contaPagarRepository;
            _categoriaDespesaRepository = categoriaDespesaRepository;
            _formaPagamentoRepository = formaPagamentoRepository;
        }

        public async Task<List<ContaPagarResponse>> ListarAsync(
            int? categoriaDespesaId,
            string? status,
            string? favorecido,
            DateOnly? vencimentoInicio,
            DateOnly? vencimentoFim,
            CancellationToken cancellationToken = default)
        {
            // "Atrasado" nao e persistido: e um Pendente cuja DataVencimento ja passou
            // (mesma convencao de Pagamento). Ao filtrar por Atrasado, buscamos os
            // Pendentes e deixamos o Mapear calcular/expor o status efetivo.
            var statusConsulta = status == "Atrasado" ? "Pendente" : status;

            var contas = await _contaPagarRepository.ListarAsync(categoriaDespesaId, statusConsulta, favorecido, vencimentoInicio, vencimentoFim, cancellationToken);
            var mapeadas = contas.Select(Mapear);

            return status == "Atrasado"
                ? mapeadas.Where(c => c.Status == "Atrasado").ToList()
                : mapeadas.ToList();
        }

        public async Task<ContaPagarResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var contaPagar = await _contaPagarRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Conta a pagar nao encontrada.");

            return Mapear(contaPagar);
        }

        public async Task<ContaPagarResponse> RegistrarAsync(RegistrarContaPagarRequest request, CancellationToken cancellationToken = default)
        {
            if (await _categoriaDespesaRepository.ObterPorIdAsync(request.CategoriaDespesaId, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Categoria de despesa nao encontrada.");
            }

            if (request.FormaPagamentoId.HasValue
                && await _formaPagamentoRepository.ObterPorIdAsync(request.FormaPagamentoId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Forma de pagamento nao encontrada.");
            }

            var contaPagar = new ContaPagar
            {
                Descricao = request.Descricao,
                CategoriaDespesaId = request.CategoriaDespesaId,
                Favorecido = request.Favorecido,
                Valor = request.Valor,
                Competencia = request.Competencia,
                DataVencimento = request.DataVencimento,
                FormaPagamentoId = request.FormaPagamentoId,
                Observacoes = request.Observacoes,
                Status = request.Status,
                DataPagamento = request.Status == "Pago" ? DateOnly.FromDateTime(DateTime.UtcNow) : null
            };

            await _contaPagarRepository.AdicionarAsync(contaPagar, cancellationToken);
            await _contaPagarRepository.SalvarAlteracoesAsync(cancellationToken);

            var contaPagarCompleta = await _contaPagarRepository.ObterPorIdAsync(contaPagar.Id, cancellationToken);
            return Mapear(contaPagarCompleta!);
        }

        public async Task<ContaPagarResponse> AtualizarAsync(int id, AtualizarContaPagarRequest request, CancellationToken cancellationToken = default)
        {
            var contaPagar = await _contaPagarRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Conta a pagar nao encontrada.");

            if (request.CategoriaDespesaId.HasValue
                && await _categoriaDespesaRepository.ObterPorIdAsync(request.CategoriaDespesaId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Categoria de despesa nao encontrada.");
            }

            if (request.FormaPagamentoId.HasValue
                && await _formaPagamentoRepository.ObterPorIdAsync(request.FormaPagamentoId.Value, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Forma de pagamento nao encontrada.");
            }

            // Campos nao informados no request mantem o valor atual (mesma
            // convencao usada em PagamentoService.AtualizarAsync).
            contaPagar.Descricao = request.Descricao ?? contaPagar.Descricao;
            contaPagar.CategoriaDespesaId = request.CategoriaDespesaId ?? contaPagar.CategoriaDespesaId;
            contaPagar.Favorecido = request.Favorecido ?? contaPagar.Favorecido;
            contaPagar.Valor = request.Valor ?? contaPagar.Valor;
            contaPagar.Competencia = request.Competencia ?? contaPagar.Competencia;
            contaPagar.DataVencimento = request.DataVencimento ?? contaPagar.DataVencimento;
            contaPagar.FormaPagamentoId = request.FormaPagamentoId ?? contaPagar.FormaPagamentoId;
            contaPagar.Observacoes = request.Observacoes ?? contaPagar.Observacoes;

            await _contaPagarRepository.SalvarAlteracoesAsync(cancellationToken);

            var contaPagarAtualizada = await _contaPagarRepository.ObterPorIdAsync(id, cancellationToken);
            return Mapear(contaPagarAtualizada!);
        }

        public async Task<ContaPagarResponse> AtualizarStatusAsync(int id, AtualizarStatusContaPagarRequest request, CancellationToken cancellationToken = default)
        {
            if (await _contaPagarRepository.ObterPorIdAsync(id, cancellationToken) is null)
            {
                throw new NaoEncontradoException("Conta a pagar nao encontrada.");
            }

            // sp_atualizar_status_conta_pagar ja preenche data_pagamento
            // automaticamente quando o novo status e 'Pago'.
            await _contaPagarRepository.AtualizarStatusViaProcedureAsync(id, request.Status, cancellationToken);

            var contaPagarAtualizada = await _contaPagarRepository.ObterPorIdAsync(id, cancellationToken);
            return Mapear(contaPagarAtualizada!);
        }

        private static ContaPagarResponse Mapear(ContaPagar contaPagar)
        {
            var statusEfetivo = contaPagar.Status == "Pendente" && contaPagar.DataVencimento < DateOnly.FromDateTime(DateTime.UtcNow)
                ? "Atrasado"
                : contaPagar.Status;

            return new ContaPagarResponse
            {
                Id = contaPagar.Id,
                Descricao = contaPagar.Descricao,
                CategoriaDespesaId = contaPagar.CategoriaDespesaId,
                CategoriaDespesaNome = contaPagar.CategoriaDespesa.Nome,
                Favorecido = contaPagar.Favorecido,
                Valor = contaPagar.Valor,
                Competencia = contaPagar.Competencia,
                DataVencimento = contaPagar.DataVencimento,
                DataPagamento = contaPagar.DataPagamento,
                FormaPagamentoId = contaPagar.FormaPagamentoId,
                FormaPagamentoNome = contaPagar.FormaPagamento?.Forma,
                Observacoes = contaPagar.Observacoes,
                Status = statusEfetivo
            };
        }
    }
}
