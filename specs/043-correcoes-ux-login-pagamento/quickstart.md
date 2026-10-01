# Quickstart: validar as 3 correções de UX

Guia de validação ponta a ponta. Contratos em [contracts/](./contracts/); modelo em [data-model.md](./data-model.md).

## 1. Login — Enter submete, sem duplo-envio (US3, FR-010/FR-011, SC-005/SC-006)

Com o backend e o frontend rodando, abrir a tela de login.

| # | Passo | Esperado |
|---|---|---|
| 1 | Preencher e-mail/RA e senha; apertar Enter no campo de senha | Login submete, mesmo resultado de clicar em "Entrar" |
| 2 | Repetir apertando Enter no campo de e-mail/RA | Idem |
| 3 | Deixar um campo vazio e apertar Enter | Mensagem "Preencha e-mail/RA e senha para continuar." |
| 4 | Com uma rede lenta (DevTools → Network → Slow 3G) ou backend propositalmente lento, apertar Enter várias vezes seguidas antes da resposta chegar | Só uma requisição de login é disparada (conferir na aba Network) |

## 2. Recuperação de senha — e-mail chega de verdade (US1, FR-001 a FR-004, SC-001/SC-002)

Ver [contracts/email-delivery-verification.md](./contracts/email-delivery-verification.md) para o passo a passo completo de configuração via User Secrets e os 5 passos de verificação (incluindo o teste de não-diferenciação entre e-mail existente/inexistente).

## 3. Botões de status — confirmação antes da ação (US2, FR-005 a FR-009, SC-003/SC-004)

Abrir o detalhe de uma Conta a Pagar (ou a Receber) com status "Pendente".

| # | Passo | Esperado |
|---|---|---|
| 1 | Olhar a seção "Alterar status" sem clicar em nada | O status atual aparece como um selo (`StatusPill`), separado dos botões de ação; os botões têm rótulo de verbo ("Marcar como Pago", etc.), não parecem um filtro |
| 2 | Clicar em "Marcar como Pago" | Abre confirmação explicando que a data de pagamento será preenchida automaticamente |
| 3 | Cancelar a confirmação | Nada muda — status continua "Pendente", nenhuma chamada de rede (conferir na aba Network) |
| 4 | Clicar em "Marcar como Pago" de novo e confirmar | Status muda para "Pago", data de pagamento preenchida — mesmo resultado de hoje |
| 5 | Repetir para "Cancelado" | Confirmação menciona que o registro não é excluído, só encerrado |
| 6 | Repetir em Contas a Receber | Mesmo comportamento, tela irmã |

## 4. Não-regressão

- `git diff --stat -- database` vazio (nenhuma migração nesta feature).
- Nenhuma mudança em `PagamentoService`/`AtualizarStatusPagamentoRequestValidator`/stored procedures — só frontend e configuração.
- Login com credenciais inválidas continua mostrando a mensagem de erro já existente, em qualquer um dos dois gatilhos (clique ou Enter).
