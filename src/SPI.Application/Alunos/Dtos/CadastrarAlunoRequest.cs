namespace SPI.Application.Alunos.Dtos
{
    public record CadastrarAlunoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? TelefoneAluno { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public string? Email { get; set; }
        public string? Senha { get; set; }
        public string? EmailResponsavel { get; set; }
        public decimal ValorAula { get; set; }
        public int? TurmaId { get; set; }

        /// <summary>
        /// Informado pela professora no cadastro: quando true, exige Telefone e Email do Responsavel
        /// (o schema atual nao guarda data de nascimento do aluno).
        /// </summary>
        public bool EhMenorDeIdade { get; set; }
    }
}
