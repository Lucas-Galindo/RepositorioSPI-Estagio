using SPI.Application.Common.Dtos;

namespace SPI.Application.Alunos.Dtos
{
    public record AlunoResponse
    {
        public int Id { get; set; }
        public string Ra { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string? Cpf { get; set; }
        public string? TelefoneAluno { get; set; }
        public string? TelefoneResponsavel { get; set; }
        public string? Email { get; set; }
        public string? EmailResponsavel { get; set; }
        public decimal ValorAula { get; set; }
        public int Frequencia { get; set; }
        public bool Ativo { get; set; }
        public List<TurmaResumoResponse> Turmas { get; set; } = new();

        public string? Cep { get; set; }
        public string? Rua { get; set; }
        public string? Numero { get; set; }
        public string? Complemento { get; set; }
        public string? Bairro { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }

        public bool ResponsavelMesmoEndereco { get; set; }
        public string? ResponsavelCep { get; set; }
        public string? ResponsavelRua { get; set; }
        public string? ResponsavelNumero { get; set; }
        public string? ResponsavelComplemento { get; set; }
        public string? ResponsavelBairro { get; set; }
        public string? ResponsavelCidade { get; set; }
        public string? ResponsavelEstado { get; set; }
    }
}
