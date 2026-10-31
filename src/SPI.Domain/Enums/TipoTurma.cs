namespace SPI.Domain.Enums
{
    /// <summary>
    /// Tipo de uma Turma (specs/063). Persistido como texto (mesmo padrao de
    /// ModalidadeCobranca) e trafegado como string no JSON.
    /// Mensalidade: turma comum, os alunos entram por matricula.
    /// Pacote: turma fechada, com data de inicio e de fim; os alunos entram pelo
    /// cadastro do pacote dela e, depois que a turma comeca, ninguem novo entra.
    /// </summary>
    public enum TipoTurma
    {
        Mensalidade = 1,
        Pacote = 2
    }
}
