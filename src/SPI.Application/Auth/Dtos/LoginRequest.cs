namespace SPI.Application.Auth.Dtos
{
    public record LoginRequest
    {
        /// <summary>
        /// E-mail (Professor/Admin/Aluno) ou RA (Aluno) de acesso. O sistema
        /// identifica sozinho o perfil dono da credencial: se contem "@" e
        /// tratado como e-mail (busca em Professor, depois Aluno, depois
        /// Admin, nessa ordem); caso contrario e tratado como RA (busca so
        /// em Aluno).
        /// </summary>
        public string Login { get; set; } = string.Empty;

        /// <summary>
        /// Senha em texto plano, validada contra o hash bcrypt armazenado.
        /// </summary>
        public string Senha { get; set; } = string.Empty;
    }
}
