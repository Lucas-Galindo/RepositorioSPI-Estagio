# Quickstart: validar o código de exclusão indo para o e-mail da professora

Guia de validação ponta a ponta. Contrato em [contracts/email-codigo-verificacao-delta.md](./contracts/email-codigo-verificacao-delta.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos

- Backend rodando, Gmail SMTP configurado (specs/049), login como Admin.
- Professora cadastrada com um e-mail real (não `.local`).

## 1. Código vai para o e-mail da professora, não do Admin (US1, FR-001, SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Logado como Admin, abrir a tela de gerenciamento da professora e clicar em "Excluir Professor" | Código de verificação gerado |
| 2 | Conferir a caixa de entrada do **e-mail do Admin** | Nenhum e-mail novo chega aí |
| 3 | Conferir a caixa de entrada do **e-mail cadastrado da professora** | E-mail "SPI - Código de confirmação para excluir professora" chega, endereçado a ela (saudação com o nome dela), mencionando qual Admin solicitou |

## 2. Admin ainda confirma a exclusão com o código (US1, FR-002, Acceptance Scenario 2)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com o código recebido no e-mail da professora | — |
| 2 | Admin digita esse código na tela de confirmação | A professora é desativada (`Ativo = false`); histórico (Turmas, Alunos, Aulas, Pagamentos) continua intacto — mesmo comportamento de specs/044 |

## 3. Professora sem e-mail cadastrado é bloqueado (Edge Case, FR-004, SC-003)

| # | Passo | Esperado |
|---|---|---|
| 1 | Professora com o campo de e-mail vazio no banco (cenário defensivo/dado legado) | — |
| 2 | Admin clica em "Excluir Professor" | Pedido recusado com mensagem clara ("A professora nao tem e-mail cadastrado..."); nenhum token criado, nenhuma tentativa de envio |

## 4. Não-regressão das regras já validadas por specs/044 (FR-005, SC-002)

- Código expira em 15 minutos e só pode ser usado uma vez — inalterado.
- Pedir um novo código invalida o anterior do mesmo Admin — inalterado.
- `AdminId` continua gravado no token (rastreabilidade de quem solicitou) — confirmável por `SELECT admin_id FROM exclusao_professor_token`.
- Reativar a professora depois de excluída não exige novo 2FA — inalterado.
- `dotnet test tests/SPI.Application.Tests --filter FullyQualifiedName~ProfessorServiceExclusaoTests` 100% verde.
