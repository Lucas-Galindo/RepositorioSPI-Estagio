namespace SPI.Application.Aulas.Dtos
{
    public record AlunoPresencaResponse
    {
        public int AlunoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Ra { get; set; } = string.Empty;

        /// <summary>
        /// NULL enquanto a aula nao foi registrada (Estoria 8); TRUE/FALSE apos o registro da sessao.
        /// </summary>
        public bool? Presente { get; set; }
    }
}
