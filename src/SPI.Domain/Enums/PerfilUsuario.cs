namespace SPI.Domain.Enums
{
    /// <summary>
    /// Perfis de acesso ao sistema. Usado como claim de role no JWT e como
    /// discriminador de dono em refresh_token/senha_reset_token, ja que o
    /// schema nao possui uma tabela unica de "usuario" (login e inferido
    /// pela tabela em que o e-mail foi encontrado).
    /// </summary>
    public enum PerfilUsuario
    {
        Professor = 1,
        Aluno = 2,
        Admin = 3
    }
}
