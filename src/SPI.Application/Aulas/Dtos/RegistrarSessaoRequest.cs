namespace SPI.Application.Aulas.Dtos
{
    public record RegistrarSessaoRequest
    {
        /// <summary>
        /// Presenca de cada aluno vinculado a aula (chave = AlunoId, valor = presente).
        /// Todo aluno vinculado a aula deve aparecer aqui.
        /// </summary>
        public Dictionary<int, bool> Presencas { get; set; } = new();
    }
}
