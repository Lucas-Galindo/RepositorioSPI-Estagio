using SPI.Application.Materias.Dtos;
using SPI.Domain.Entities;
using SPI.Domain.Exceptions;
using SPI.Domain.Repositories;

namespace SPI.Application.Materias.Services
{
    public class MateriaService : IMateriaService
    {
        private readonly IMateriaRepository _materiaRepository;

        public MateriaService(IMateriaRepository materiaRepository)
        {
            _materiaRepository = materiaRepository;
        }

        public async Task<List<MateriaResponse>> ListarAsync(string? nome, string? nivel, bool? ativo, CancellationToken cancellationToken = default)
        {
            var materias = await _materiaRepository.ListarAsync(nome, nivel, ativo, cancellationToken);
            return materias.Select(Mapear).ToList();
        }

        public async Task<MateriaResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var materia = await _materiaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Materia nao encontrada.");

            return Mapear(materia);
        }

        public async Task<MateriaResponse> CadastrarAsync(MateriaRequest request, CancellationToken cancellationToken = default)
        {
            if (await _materiaRepository.ExisteNomeAsync(request.Nome, cancellationToken: cancellationToken))
            {
                throw new ConflitoException("Ja existe uma materia com este nome.");
            }

            var materia = new Materia
            {
                Nome = request.Nome,
                Nivel = request.Nivel,
                Descricao = request.Descricao,
                Ativo = true
            };

            await _materiaRepository.AdicionarAsync(materia, cancellationToken);
            await _materiaRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(materia);
        }

        public async Task<MateriaResponse> AtualizarAsync(int id, MateriaRequest request, CancellationToken cancellationToken = default)
        {
            var materia = await _materiaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Materia nao encontrada.");

            if (await _materiaRepository.ExisteNomeAsync(request.Nome, id, cancellationToken))
            {
                throw new ConflitoException("Ja existe uma materia com este nome.");
            }

            // null = campo nao informado nesta chamada (mantem o valor
            // atual); para limpar o campo de fato, o cliente manda "".
            materia.Nome = request.Nome;
            materia.Nivel = request.Nivel ?? materia.Nivel;
            materia.Descricao = request.Descricao ?? materia.Descricao;

            await _materiaRepository.SalvarAlteracoesAsync(cancellationToken);

            return Mapear(materia);
        }

        public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
        {
            var materia = await _materiaRepository.ObterPorIdAsync(id, cancellationToken)
                ?? throw new NaoEncontradoException("Materia nao encontrada.");

            materia.Ativo = false;
            await _materiaRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        private static MateriaResponse Mapear(Materia materia) => new()
        {
            Id = materia.Id,
            Nome = materia.Nome,
            Nivel = materia.Nivel,
            Descricao = materia.Descricao,
            Ativo = materia.Ativo
        };
    }
}
