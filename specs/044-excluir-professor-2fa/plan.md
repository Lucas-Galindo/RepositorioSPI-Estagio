# Implementation Plan: Excluir Professor com Confirmação por 2FA via E-mail

**Branch**: `044-excluir-professor-2fa` | **Date**: 2026-10-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/044-excluir-professor-2fa/spec.md`

## Summary

Adiciona exclusão lógica (e reativação) da professora, com uma etapa de 2FA por e-mail antes de efetivar a exclusão. Reaproveita três padrões já existentes no código, sem inventar nenhum mecanismo novo:

1. **Token de verificação**: mesmo gerador já usado pela recuperação de senha (`ITokenService.GerarTokenSeguro()`/`CalcularHash()` — string opaca, hash armazenado, nunca texto plano), guardado numa entidade nova e dedicada (`ExclusaoProfessorToken`), porque o dono (Admin) e o alvo (Professor) são diferentes do fluxo de recuperação de senha (onde dono e alvo são a mesma professora).
2. **Exclusão lógica + reativação**: mesmo padrão exato de `AlunoService.ExcluirAsync`/`ReativarAsync` (`Ativo = false`/`true`), replicado em `ProfessorService`/`ProfessorController`.
3. **Envio de e-mail**: mesmo `IEmailSender`/`BrevoEmailSender` da spec 043, sem nenhuma mudança na infraestrutura de envio — só um novo texto de e-mail, endereçado ao Admin autenticado (nunca à professora).

Novidade real desta feature: o destinatário do código é o **Admin autenticado que pede a exclusão** (via claims do próprio token de acesso), não a pessoa cujo cadastro está sendo alterado — diferente de todo 2FA/recuperação já existente no sistema, onde dono do token e alvo da ação sempre coincidem.

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript / Next.js App Router (frontend) — stack já vigente.

**Primary Dependencies**: Nenhuma nova — reaproveita `ITokenService`, `IEmailSender`/`BrevoEmailSender` (spec 043), `IPasswordHasher` não é usado aqui (não há senha envolvida nesta ação).

**Storage**: MySQL — **uma tabela nova**, `exclusao_professor_token`, espelhando `senha_reset_token` (hash, `criado_em`, `expira_em`, `usado`) mais uma FK para `admin` (dono do código) além da FK para `professor` (alvo). Nenhuma mudança em `professor` (o campo `Ativo` já existe).

**Testing**: xUnit com fakes manuais em `tests/SPI.Application.Tests/` (padrão do projeto). Frontend sem suíte automatizada (mesma decisão já aceita nas specs 040-043) — validação via `quickstart.md`.

**Target Platform**: Web (backend ASP.NET Core + frontend Next.js), mesma stack já em produção/dev local.

**Project Type**: Web application (backend + frontend).

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-002: o destinatário do e-mail MUST ser resolvido a partir do Admin autenticado (claims do token de acesso + `IAdminRepository.ObterPorIdAtivoAsync`), nunca um parâmetro vindo do cliente (evita que um Admin peça para mandar o código para outro e-mail).
- FR-003/FR-004: um único código pendente e válido por Admin por vez — pedir um novo invalida o anterior desse mesmo Admin imediatamente (mesma lógica de uso único de `SenhaResetToken`, com o escopo adicional "por Admin").
- FR-007: a exclusão em si (depois do código confirmado) MUST ser idêntica, linha por linha, ao padrão já usado em `AlunoService.ExcluirAsync` — só `Ativo = false`, nada de cascata, nada de exclusão física.
- FR-012: nenhum provedor de e-mail novo — mesma injeção de `IEmailSender` já configurada.
- Princípio II (validação única no backend): "quem pode excluir" (`[Authorize(Roles = Admin)]`) e "o código está certo/válido" vivem só no backend; o frontend só exibe o resultado.

**Scale/Scope**: 1 entidade nova (`ExclusaoProfessorToken`) + 1 repositório + 1 script de banco; 2 métodos novos em `ProfessorService` (`ExcluirAsync` reaproveitando fluxo de código + `ReativarAsync`) + os métodos de solicitar/confirmar código; 3 endpoints novos em `ProfessorController` (todos `Admin`-only, sem `{id}` — sistema é single-tenant, mesmo padrão de `GET/PUT /api/professor/admin`); 1 seção nova no formulário já existente (`CadastrarProfessoraForm.tsx`), seguindo o mesmo padrão de etapas de `EsqueciSenhaForm.tsx`. Nenhuma tela nova.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Sim — central | A própria feature é sobre exclusão lógica (`Ativo = false`), preservando Turmas/Alunos/Aulas/Pagamentos vinculados — mesmo padrão já validado em Aluno/Turma/Matéria. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | "Quem pode clicar" (`[Authorize(Roles=Admin)]`), "o código é válido" (hash/expiração/uso único) e "a exclusão é lógica" vivem só no backend; o frontend só mostra os passos e o resultado. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | Nenhuma opção nova exposta sem implementação — 2FA, exclusão e reativação são implementados de ponta a ponta antes de o botão aparecer. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim | Token de verificação: hash armazenado, nunca texto plano (mesmo padrão de `SenhaResetToken`/`RefreshToken`). Nenhuma credencial nova — reaproveita `IEmailSender` já configurado via User Secrets (spec 043). |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; não supera nenhum comportamento documentado — só estende o padrão de exclusão lógica (spec 026) a mais uma entidade. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/044-excluir-professor-2fa/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── excluir-professor-api.md
│   └── email-codigo-verificacao.md
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── SPI.Domain/
│   ├── Entities/ExclusaoProfessorToken.cs           # NOVO
│   └── Repositories/IExclusaoProfessorTokenRepository.cs  # NOVO
├── SPI.Application/
│   └── Professores/
│       ├── Dtos/SolicitarExclusaoResponse.cs        # NOVO (sem corpo relevante, 202/200 vazio)
│       ├── Dtos/ConfirmarExclusaoProfessorRequest.cs # NOVO { Codigo }
│       ├── Validators/ConfirmarExclusaoProfessorRequestValidator.cs  # NOVO
│       └── Services/
│           ├── IProfessorService.cs                 # + SolicitarExclusaoAsync/ConfirmarExclusaoAsync/ReativarAsync
│           └── ProfessorService.cs                  # idem (implementação)
├── SPI.Infrastructure/
│   ├── Persistence/Configurations/ExclusaoProfessorTokenConfiguration.cs  # NOVO
│   └── Repositories/ExclusaoProfessorTokenRepository.cs                   # NOVO
└── SPI.Api/
    └── Controllers/ProfessorController.cs           # + 3 endpoints (solicitar-codigo/confirmar/reativar)

database/
└── 17_exclusao_professor_token.sql                  # NOVO: tabela + FKs

frontend/
├── lib/api/professor.ts                             # + solicitarExclusaoProfessor/confirmarExclusaoProfessor/reativarProfessor
└── components/admin/CadastrarProfessoraForm.tsx      # + secao "Excluir Professor" (aviso + 2FA, padrao de EsqueciSenhaForm)

tests/SPI.Application.Tests/
└── Professores/
    ├── ProfessorServiceFakes.cs                      # + fake do repositorio de token
    ├── ProfessorServiceExclusaoTests.cs               # NOVO
    └── ProfessorServiceReativacaoTests.cs              # NOVO
```

**Structure Decision**: Estrutura já existente (camadas Domain/Application/Infrastructure/Api + `frontend/`), mesma organização de `SenhaResetToken`/`RecuperacaoSenhaService` espelhada para a nova entidade. Nenhum diretório novo além de `Professores/Dtos`/`Professores/Validators`, que já existem.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, contracts/, quickstart.md).*

- Princípio I: `data-model.md` confirma que a exclusão é só `Ativo = false`; nenhuma tabela vinculada (Turma, Aluno, Aula, Pagamento) é tocada.
- Princípio II: `contracts/excluir-professor-api.md` confirma que a validação do código (hash/expiração/uso único) e a autorização (`Admin`) vivem só no backend.
- Princípio IV: `contracts/email-codigo-verificacao.md` confirma reaproveitamento do `IEmailSender` já configurado, sem segredo novo.
- Nenhuma violação nova introduzida pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
