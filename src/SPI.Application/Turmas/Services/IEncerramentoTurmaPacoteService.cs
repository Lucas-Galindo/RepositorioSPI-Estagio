namespace SPI.Application.Turmas.Services
{
    // specs/063 (FR-019): fim da turma de pacote inativa os pacotes dela, com o saldo preservado.
    public interface IEncerramentoTurmaPacoteService
    {
        // A professora inativou a turma: inativa os pacotes ativos dela. NAO salva -- quem chama
        // salva no mesmo salvamento da inativacao da turma (repositorios compartilham o contexto).
        // Devolve quantos pacotes foram inativados.
        Task<int> InativarPacotesDaTurmaAsync(int turmaId, CancellationToken cancellationToken = default);

        // Rotina de fim de turma: inativa todo pacote ativo cuja turma esta inativa ou com data de
        // fim anterior a `hoje`, e salva. Idempotente. Devolve quantos pacotes foram inativados.
        Task<int> EncerrarPacotesDeTurmasEncerradasAsync(DateOnly hoje, CancellationToken cancellationToken = default);
    }
}
