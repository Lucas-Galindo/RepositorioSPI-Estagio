# Relatório: Chamada só nos detalhes da aula e aviso de aula sem frequência (spec 064)

**Card Trello**: "Bloquear chamada no agendamento; liberar frequência só nos detalhes após o início da aula" (Backlog → Design → A Fazer → Em andamento → Revisão de Código/Conceito)
**Spec**: `specs/064-chamada-so-nos-detalhes/` (specify, plan, tasks e analyze em 2026-10-31 e 2026-11-03; implement em 2026-11-03)
**Status final**: ✅ Implementada e validada: 507 testes, API autenticada e telas no Chrome. A migração 26 foi aplicada só no banco local; produção fica com o usuário.

## O que foi pedido
O popup "Agendar aula" só agenda (sem presença), toda aula nasce "Agendada", a chamada só se faz nos detalhes depois do horário de início (fuso America/Sao_Paulo, decidido pelo servidor, 409 antes), e toda aula antiga sem frequência gera aviso nas notificações, sem corte de data.

## O que foi feito
**Banco**: `database/26_aula_frequencia_registrada_em.sql` (coluna opcional `aula.frequencia_registrada_em`, sem preenchimento retroativo). Consulta somente de leitura `database/consultas/aulas_realizadas_antes_do_inicio.sql`.

**Backend**
- `RegraChamada` (regra única e pura): liberada = aula ativa, "Agendada" e agora ≥ data + hora de início; `SegundosParaLiberar`; mensagem de bloqueio. `RelogioExtensions.AgoraEmSaoPaulo` sobre o `TimeProvider` (IANA com fallback do Windows, erro explícito se o fuso não resolver).
- `AulaService`: criar sempre "Agendada" (sem frequência, sem cobrança; `Presencas` saiu de `AulaRequest`); `RegistrarSessaoAsync` recusa aula excluída (404) e aula que não começou (409), e grava `FrequenciaRegistradaEm`; as respostas trazem `chamadaLiberada` e `segundosParaChamada`.
- Notificações: removidos o corte de vigência e `Notificacoes:AulasPendentesDesde`; novo texto; o `agora` do sino e da rotina vem do relógio de São Paulo.

**Frontend**: `AulaForm` só agenda (botão sempre "Agendar aula"); `PresencaChecklist` removido (sem uso); detalhes da aula mostram a chamada só se `chamadaLiberada`, senão "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA." e buscam a aula de novo na hora certa (duração informada pelo servidor, com teto de 1 h); sino: "Aula sem frequência". Editar, cancelar, excluir e reposição seguem disponíveis.

**Documentação**: notas nas specs 055 (FR-001 a FR-007 e US1 revogados; US2 vale) e 059 (FR-004).

## Validação
| Prova | Resultado |
|---|---|
| `dotnet test` (com build) | **507 aprovados**, 0 falhas (eram 469; −7 casos "retroativos" revogados, +45 novos) |
| API autenticada (hora real de SP) | aula de ontem nasce Agendada e liberada; `presencas` no corpo ignorado; aula 1 h à frente bloqueada (`segundosParaChamada≈3550`) e 409 com a mensagem; aula em andamento registra (200) e a segunda chamada dá 409; aula excluída 404; data futura 409 com editar/cancelar/excluir 200; avisos sem duplicar |
| Banco | `frequencia_registrada_em` gravada em hora de SP (21:29:47); as 2 aulas de antes de 19/10 passaram a ter aviso; 0 aulas realizadas antes do início; contagens de `aula`, `aula_aluno` e `pagamento` iguais antes e depois da consulta de auditoria |
| Chrome | formulário sem Presença; detalhes bloqueada (com mensagem), liberada (Presente + Registrar), realizada, sino e modo escuro conferidos |
| `tsc`, `eslint`, contraste, `next build` | limpos (48 combinações, 0 pares abaixo de 4.5:1) |
| "MUST NOT" (FR-001, FR-002, FR-004, FR-007, FR-009 a FR-011, FR-015, FR-016) | `grep` e leitura do diff: sem textos de retroativo, sem relógio do navegador na tela, `registrarSessao` com um único uso, migração sem `UPDATE`, consulta sem escrita, nenhum `try/catch` novo em controller; arquivos novos conforme o esperado |

## Observações
- **`dotnet build SPI.slnx` não compila o projeto de testes** (não está na solução). Validar sempre com `dotnet test tests/SPI.Application.Tests`, sem `--no-build`. Isso também recompilou e validou o código da spec 065.
- **Efeito esperado do aviso sem corte**: as 4 aulas antigas ativas e sem frequência do banco local ganharam aviso (o selo do sino sobe), inclusive 2 anteriores a 19/10/2026. Por isso a comparação por hash de todas as telas deixou de ser um teste confiável.
- O MySQL local caiu uma vez durante a validação (memória: builds do Next e Chrome ao mesmo tempo) e foi religado pelo usuário; os dados estavam íntegros.
- Testes antigos que registram chamada com data futura passaram a usar um relógio fixo em 2100-01-01 nos fakes (só o arrange mudou, nenhum `Assert`); os expectativas de dois testes mudaram por decisão da spec (texto do aviso; corte de 19/10), com comentário.

## O que NÃO foi feito
Trocar os demais ~18 `DateTime.Now` do projeto pelo relógio de São Paulo (financeiro, relatórios, lembretes, turma de pacote): não decidem a chamada; viram card sugerido. Aplicar a migração 26 em produção: com o usuário, com backup.

## Dados e segurança
Só o banco local, com backup antes (`spi_db_20261103_211401.sql` e `…214005.sql`). Dados sintéticos "TESTE-064" (7 aulas, 2 alunos, 2 turmas, 1 pagamento e avisos) apagados; dados do usuário intactos (turma 15, vínculos 20, 21, 22 e 34, 1 aluno). Nenhum e-mail saiu (servidor de e-mail apontado para um endereço inexistente). Nada em produção.
