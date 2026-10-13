# Quickstart: validar a migração para Gmail SMTP

Guia de validação ponta a ponta. Modelo de dados em [data-model.md](./data-model.md); decisões técnicas em [research.md](./research.md).

## Pré-requisitos (passo manual do usuário, fora do código desta feature)

- Criar uma conta Google **dedicada** (não a pessoal de ninguém envolvido no sistema).
- Ativar Verificação em Duas Etapas nessa conta e gerar uma **Senha de app** específica para SMTP.
- Atualizar o User Secrets do projeto: `Smtp:SmtpUsuario` (o e-mail da conta dedicada), `Smtp:SmtpChave` (a Senha de app gerada, não a senha normal), `Smtp:RemetenteEmail` (o mesmo e-mail da conta dedicada, ou um alias dela).
- Aplicar a migração `database/18_lembrete_destinatarios_tentados.sql`.

## 1. Recuperação de senha e 2FA continuam funcionando (US1, FR-001, FR-002, FR-006, SC-001)

| # | Passo | Esperado |
|---|---|---|
| 1 | Solicitar recuperação de senha com um e-mail cadastrado | Código chega no e-mail, enviado pela conta Gmail dedicada (confirmar remetente) |
| 2 | Como Admin, solicitar o código de 2FA de exclusão de professor (spec 044) | Código chega no e-mail do Admin, mesmo conteúdo/formato de antes |
| 3 | Comparar o texto/formato de ambos com o que era antes desta feature | Idêntico — só o remetente/provedor muda |

## 2. Lembretes divididos em lotes entre ciclos (US2, FR-003/FR-004/FR-005, SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Configurar `Lembretes:MaxEmailsPorCiclo` para um valor baixo (ex.: 2, só para teste) e `Lembretes:IntervaloVerificacaoSegundos` para um valor curto (ex.: 30) | — |
| 2 | Criar/forçar um lembrete vencido para uma turma com mais alunos que o limite configurado (ex.: 5 alunos, limite 2) | Lembrete fica `Pendente` |
| 3 | Esperar o primeiro ciclo rodar | Só 2 e-mails são enviados; `Status` continua `Pendente`; `DestinatariosTentados` tem 2 endereços |
| 4 | Esperar o ciclo seguinte | Mais 2 e-mails são enviados (os que faltavam, não os 2 já tentados); continua `Pendente` |
| 5 | Esperar o ciclo seguinte (último destinatário) | Último e-mail enviado; `Status` vira `Enviado` (supondo pelo menos 1 sucesso) |
| 6 | Conferir no MySQL: `SELECT status, destinatarios_tentados, envios_com_sucesso FROM lembrete WHERE id = ...` a cada passo acima | Confirma a retomada parcial descrita |

## 3. Priorização por tempo de espera (US2, FR-004)

| # | Passo | Esperado |
|---|---|---|
| 1 | Ter dois lembretes vencidos ao mesmo tempo, somando mais destinatários que o limite por ciclo, com `HoraProgramada` diferentes | — |
| 2 | Deixar um ciclo rodar | O lembrete com `HoraProgramada` mais antiga consome o orçamento primeiro |

## 4. Não-regressão e verificação (FR-006/FR-007, SC-003/SC-004, regra do `CLAUDE.md`)

- Nenhuma mudança na regra de quais lembretes são elegíveis (spec 001) — só a cadência de envio.
- `grep -rn "Brevo" src/` (fora de `bin`/`obj`) não retorna nenhuma ocorrência — confirma FR-007/SC-004.
- Ler o diff de `LembreteDispatcherService.cs`/`LembreteRepository.cs`: confirmar que `ObterEmailsDestinatarios` e a query de elegibilidade (`Status == "Pendente" && HoraProgramada <= agora`) não foram alterados — só a ordenação e o corte por orçamento (regra do `CLAUDE.md` para FR-006 "MUST NOT alterar regra de elegibilidade").
- `git status --porcelain` não mostra nenhum arquivo não rastreado além dos renomeados/criados intencionalmente (regra do `CLAUDE.md` para "não criar arquivo além do necessário").
- `dotnet test tests/SPI.Application.Tests` 100% verde, incluindo os testes novos do orçamento por ciclo.
