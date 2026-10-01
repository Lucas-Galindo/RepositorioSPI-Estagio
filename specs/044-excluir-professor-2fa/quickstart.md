# Quickstart: validar a exclusão de Professor com 2FA

Guia de validação ponta a ponta. Contratos em [contracts/](./contracts/); modelo em [data-model.md](./data-model.md).

## Pré-requisitos

- Script `database/17_exclusao_professor_token.sql` aplicado.
- Backend e frontend rodando; login como Admin (uma das contas já cadastradas) na tela `admin/cadastrar-professora`, com uma professora já cadastrada e ativa.
- Credenciais da Brevo já configuradas (spec 043) — ou um ambiente sem o bloqueio de rede documentado naquela spec, para confirmar o e-mail de verdade.

## 1. Exclusão completa com 2FA (US1, FR-001 a FR-005, FR-007, SC-001/SC-002/SC-005)

| # | Passo | Esperado |
|---|---|---|
| 1 | Na tela de gerenciamento da professora, localizar "Excluir Professor" | Visível (logado como Admin) |
| 2 | Clicar em "Excluir Professor" | Aviso single-tenant aparece (ver §2) antes/junto do pedido de código |
| 3 | Confirmar o pedido do código | Um e-mail chega na caixa do **Admin logado** (não da professora) com um código |
| 4 | Colar o código na tela e confirmar | Professora passa a inativa; Turmas/Alunos/Aulas/Pagamentos dela continuam intactos e consultáveis |
| 5 | Tentar logar com a conta da professora excluída | Login recusado (comportamento já existente, sem mudança) |
| 6 | Repetir o fluxo com um código **errado** | Recusado, mensagem clara, professora continua ativa |
| 7 | Pedir um novo código antes de usar o anterior, depois tentar o código **antigo** | Recusado (invalidado pelo pedido mais recente) |
| 8 | Esperar o código expirar (ou simular expiração) e tentá-lo | Recusado, com opção de pedir um novo |

## 2. Aviso single-tenant (US2, FR-006, SC-004)

Ao clicar em "Excluir Professor", antes do código ser pedido (ou junto):

| Esperado |
|---|
| Um aviso específico e destacado — distinto de um aviso genérico de "ação irreversível" — explica que o sistema fica inacessível para uso normal (login da professora, alunos, aulas) até uma nova professora ser cadastrada |

## 3. Reativação (US3, FR-009, SC-006)

| # | Passo | Esperado |
|---|---|---|
| 1 | Com a professora excluída (passo 4 de §1), acionar "Reativar" | Sem pedir nenhum código |
| 2 | Tentar logar com a conta reativada | Login funciona normalmente, dados intactos |

## 4. Não-regressão e requisitos negativos

- O e-mail do código **nunca** vai para o endereço da professora (conferir o destinatário real do e-mail recebido).
- Cancelar a tela de código (fechar sem confirmar) não altera `Ativo` nem consome o código — ele só deixa de funcionar quando expira ou quando um novo é pedido.
- `git diff --stat -- frontend/app frontend/components` fora da seção de exclusão, e `database/` só com o script novo — nenhuma mudança em `AlunosController`/`TurmasController`/validação de transição de status (fora de escopo desta feature).
- Suíte de backend (`dotnet test tests/SPI.Application.Tests`) 100% verde, incluindo os testes novos.
