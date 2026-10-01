# Contrato: API de exclusão/reativação do Professor

Três endpoints novos em `ProfessorController` (`[Route("api/professor")]`, já existente), todos `[Authorize(Roles = nameof(PerfilUsuario.Admin))]`. Nenhum usa `{id}` na rota — sistema single-tenant, mesmo padrão de `GET/PUT /api/professor/admin` já existentes.

## `POST /api/professor/admin/excluir/solicitar-codigo`

Gera um código novo e o envia por e-mail ao Admin autenticado.

- **Request**: sem corpo.
- **200 OK**: sem corpo relevante (`Ok()`), mesmo quando já havia um código pendente (ele é invalidado antes do novo ser criado — R4).
- **404 Not Found**: nenhuma professora cadastrada (nada a excluir).
- **500**: falha ao enviar o e-mail (provedor fora do ar, rede bloqueada) — mensagem clara, nunca falha silenciosa (FR-011). O código já foi salvo no banco antes da tentativa de envio (mesmo padrão de `RecuperacaoSenhaService.EsqueciSenhaAsync`: salvar primeiro, depois tentar enviar) — mas, diferente da recuperação de senha, aqui a falha de envio **é** reportada ao cliente (o Admin precisa saber que não vai receber o código, já que esta ação não tem a mesma preocupação de "não revelar se um e-mail existe" que a recuperação de senha tem).

## `POST /api/professor/admin/excluir/confirmar`

Valida o código e, se correto, efetiva a exclusão lógica.

- **Request**: `{ "codigo": "<valor recebido por e-mail>" }`
- **200 OK**: professora desativada (`Ativo = false`).
- **400 Bad Request**: `codigo` vazio/ausente (validação de formato).
- **409 Conflict**: código errado, expirado, ou já usado — mensagem clara, sem revelar o código correto (FR-005).
- **404 Not Found**: nenhuma professora cadastrada.

## `PATCH /api/professor/admin/reativar`

Reativa a professora previamente desativada — **sem** 2FA (FR-009).

- **Request**: sem corpo.
- **200 OK**: `ProfessorResponse` atualizado (`Ativo = true`), mesmo formato de `AlunoController.Reativar`.
- **404 Not Found**: nenhuma professora cadastrada.
- **409 Conflict**: professora já está ativa (nada a reativar) — mensagem informativa, não um erro grave.

## Não-regressão

- `GET/PUT /api/professor/admin`, `GET/PUT /api/professor/me`, `PUT /api/professor/me/senha`, `POST /api/professor/cadastro-inicial`: contratos inalterados.
- `POST /api/auth/login`/`POST /api/auth/refresh`: comportamento com professora inativa inalterado (já existente).
