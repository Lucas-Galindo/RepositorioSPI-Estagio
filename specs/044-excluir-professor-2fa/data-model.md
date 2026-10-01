# Data Model: Excluir Professor com Confirmação por 2FA via E-mail

## Entidade existente reutilizada: `Professor`

Nenhum campo novo — `Ativo` já existe e já é verificado no login (`AutenticacaoService.ValidarCredenciais`/`ObterPorIdAtivoAsync`). Esta feature só passa a escrever `false`/`true` nele pelas novas ações de exclusão/reativação, mesmo padrão de `Aluno`/`Turma`/`Materia`.

## Entidade nova: `ExclusaoProfessorToken`

| Campo | Tipo | Regra |
|---|---|---|
| `Id` | `long` (PK, auto-incremento) | |
| `AdminId` | `int` (FK → `admin.id`) | Dono do código — o Admin que o solicitou e único que pode usá-lo. |
| `ProfessorId` | `int` (FK → `professor.id`) | Alvo da exclusão (sempre "a" professora única, no sistema single-tenant de hoje). |
| `TokenHash` | `string` (`VARCHAR(255)`, `UNIQUE`) | Hash SHA-256 do código (via `ITokenService.CalcularHash`) — nunca o valor em claro (Princípio IV). |
| `CriadoEm` | `DateTime` | Data/hora de emissão. |
| `ExpiraEm` | `DateTime` | `CriadoEm` + 15 minutos (FR-003, mesmo valor de `SenhaResetToken`). |
| `Usado` | `bool` (padrão `false`) | `true` após confirmação bem-sucedida, **ou** quando um novo código do mesmo Admin é solicitado antes deste expirar (FR-004/R4). |

**Índices/constraints**: `UNIQUE (token_hash)`; índice em `admin_id` (para localizar/invalidar o código pendente de um Admin ao pedir um novo); índice em `professor_id` (uso ocasional, consistência com `senha_reset_token`).

## Fluxo de estado do token

| Evento | Efeito |
|---|---|
| Admin solicita o código | Todo token não-usado do mesmo `AdminId` vira `Usado = true`; um novo token é criado (`Usado = false`, `ExpiraEm` = agora + 15min); e-mail enviado para o endereço do Admin. |
| Admin confirma com o código certo, dentro da validade, ainda não usado | `Usado = true`; `Professor.Ativo = false` (mesma transação). |
| Admin confirma com código errado, expirado, ou já usado | Nada muda — nem no token, nem no professor; erro claro devolvido. |
| Admin cancela a tela (nunca confirma) | O token permanece como estava (não-usado) até expirar sozinho — expirar é suficiente para torná-lo inutilizável; não precisa de uma ação explícita de "cancelar" no backend (FR-010 já é satisfeito pela expiração natural do token). |

## DTOs novos

| DTO | Campos | Uso |
|---|---|---|
| `ConfirmarExclusaoProfessorRequest` | `Codigo: string` (obrigatório, não vazio) | Corpo de `POST /api/professor/admin/excluir/confirmar`. |

`solicitar-codigo` e `reativar` não têm corpo de requisição nem DTO de resposta com dado relevante (`200 OK` vazio, ou `200 OK` com o `ProfessorResponse` atualizado no caso de reativar — mesmo formato de `AlunoController.Reativar`).

## Validações (dono único)

| Regra | Onde |
|---|---|
| Só Admin pode solicitar/confirmar exclusão ou reativar | `[Authorize(Roles = nameof(PerfilUsuario.Admin))]` nos 3 endpoints |
| Código certo = hash bate, não expirado, não usado, pertence ao Admin autenticado | `ProfessorService.ConfirmarExclusaoAsync` |
| Exclusão é só `Ativo = false`, nunca remove a linha nem toca em Turma/Aluno/Aula/Pagamento | `ProfessorService.ConfirmarExclusaoAsync` (mesmo padrão de `AlunoService.ExcluirAsync`) |
| Destinatário do e-mail é sempre o Admin autenticado, nunca um parâmetro do cliente | `ProfessorService.SolicitarExclusaoAsync`, resolvendo e-mail via `IAdminRepository.ObterPorIdAtivoAsync(User.ObterUsuarioId())` |
| Professor inativo não loga | `AutenticacaoService.ValidarCredenciais`/`ObterPorIdAtivoAsync` (já existente, nenhuma mudança) |
